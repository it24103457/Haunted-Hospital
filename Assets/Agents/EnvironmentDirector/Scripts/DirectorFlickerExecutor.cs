using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [DisallowMultipleComponent]
    public sealed class DirectorFlickerExecutor : MonoBehaviour
    {
        [SerializeField] private DirectorEventEligibility eligibility;
        [SerializeField] private DirectorTargetReservations reservations;
        [SerializeField] private DirectorEventMemory memory;
        [SerializeField] private DirectorObservation observation;
        [SerializeField] private Transform directorLightsRoot;
        [SerializeField] private Transform essentialLightsRoot;
        [SerializeField] private AudioSource sound;
        [SerializeField] private AudioClip flickerSound;
        [SerializeField, Range(0,1)] private float volume = .35f;
        [SerializeField] private string status = "Idle";
        [SerializeField] private bool running;
        private readonly List<Light> lights = new List<Light>();
        private readonly List<float> intensities = new List<float>();
        private DirectorPlanningAction action;
        private string token;
        private float startedAt;
        private bool ownsMemory;

        public bool Supports(DirectorEventDefinition definition) =>
            isActiveAndEnabled && definition != null && definition.EventType == DirectorEventType.LightFlicker &&
            sound != null && flickerSound != null;

        public bool TryStart(DirectorPlanningAction candidate)
        {
            if (!Application.isPlaying || running || !Supports(candidate.definition) ||
                eligibility == null || reservations == null || memory == null || observation == null ||
                !memory.IsReady || directorLightsRoot == null || essentialLightsRoot == null) return false;
            if (!eligibility.TryReserveCandidate(candidate.definition,candidate.anchor,out token)) return false;
            var check = eligibility.Evaluate(candidate.definition,candidate.anchor,token);
            if (!check.eligible) { reservations.Release(token); token = null; status = check.reason; return false; }
            lights.Clear(); intensities.Clear();
            foreach (var light in candidate.anchor.DirectorLights)
            {
                if (light == null || !light.transform.IsChildOf(directorLightsRoot) ||
                    light.transform.IsChildOf(essentialLightsRoot)) { Release(); return false; }
                if (light.isActiveAndEnabled && light.intensity > 0)
                { lights.Add(light); intensities.Add(light.intensity); }
            }
            if (lights.Count == 0) { status = "No illuminated Director lights"; Release(); return false; }
            if (!memory.BeginEvent(candidate.definition.EventId,candidate.definition.EventType,
                candidate.anchorId,candidate.roomId)) { Release(); return false; }
            ownsMemory = true; action = candidate; running = true; startedAt = Time.time;
            reservations.Renew(token,candidate.definition.Duration+1);
            sound.transform.position = lights[0].transform.position;
            sound.clip = flickerSound; sound.volume = volume; sound.pitch = 1f; sound.loop = true; sound.ignoreListenerPause = false; sound.Play();
            ApplyBrightness(.35f);
            memory.MarkEffectDelivered(candidate.definition.PredictedTensionIncrease);
            status = "Flickering " + candidate.roomId;
            return true;
        }

        private void Update()
        {
            if (!running) return;
            // Gameplay restrictions apply only to admission. Pause preserves this event.
            // The existing pause menu freezes both scaled time and the audio DSP clock.
            if (Time.timeScale <= 0f || AudioListener.pause) return;
            if (memory == null || sound == null || directorLightsRoot == null || essentialLightsRoot == null)
            { Finish(DirectorEventOutcome.Cancelled); return; }
            float elapsed = Time.time-startedAt;
            float duration = action.definition.Duration;
            if (elapsed >= duration) { Finish(DirectorEventOutcome.Completed); return; }
            // An irregular electrical flutter. Essential illumination is never changed.
            float flutter = Mathf.PerlinNoise(elapsed*19f, 4.17f);
            float multiplier = flutter < .46f ? .18f : Mathf.Lerp(.55f,1f,flutter);
            float recovery = Mathf.InverseLerp(Mathf.Max(0,duration-.4f),duration,elapsed);
            ApplyBrightness(Mathf.Lerp(multiplier,1,recovery));
            sound.volume = volume*(1-recovery);
        }
        private void ApplyBrightness(float multiplier)
        {
            for(int i=0;i<lights.Count;i++) if(lights[i]!=null) lights[i].intensity=intensities[i]*multiplier;
        }
        private void Finish(DirectorEventOutcome outcome)
        {
            ApplyBrightness(1);
            if(sound!=null) {sound.Stop();sound.loop=false;sound.volume=volume;}
            running=false;
            if(ownsMemory && memory!=null) memory.FinishEvent(outcome);
            ownsMemory=false; Release();
            status=outcome.ToString();
        }
        private void Release()
        {
            if(reservations!=null && token!=null)reservations.Release(token);
            token=null;lights.Clear();intensities.Clear();
        }
        private void OnDisable() { if(running || ownsMemory)Finish(DirectorEventOutcome.Cancelled); }
    }
}
