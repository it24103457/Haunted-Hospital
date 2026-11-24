using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [DisallowMultipleComponent]
    public sealed class DirectorBlackoutExecutor : MonoBehaviour
    {
        // Scene binding for the reusable Director prefab.
        public void BindLighting(Transform director, Transform essential) { directorLightsRoot = director; essentialLightsRoot = essential; }

        [SerializeField] private DirectorEventEligibility eligibility;
        [SerializeField] private DirectorTargetReservations reservations;
        [SerializeField] private DirectorEventMemory memory;
        [SerializeField] private DirectorObservation observation;
        [SerializeField] private Transform directorLightsRoot;
        [SerializeField] private Transform essentialLightsRoot;
        [Tooltip("Remaining emergency light strength during blackout; normal strength is restored afterward.")]
        [SerializeField, Min(.01f)] private float essentialBlackoutMultiplier = 1f;
        [Tooltip("Extra brightness for selected hallway emergency lights during blackout only.")]
        [SerializeField, Min(1f)] private float hallwayBlackoutMultiplier = 1f;
        [Tooltip("Minimum intensity for selected hallway emergency lights during blackout; normal intensity is restored afterward.")]
        [SerializeField, Min(0f)] private float hallwayBlackoutMinimumIntensity;
        [SerializeField] private Light[] hallwayEmergencyLights = new Light[0];
        [Tooltip("Background ambient light strength during blackout; restored when the event ends.")]
        [SerializeField, Range(.05f, 1f)] private float ambientBlackoutMultiplier = 1f;
        [SerializeField] private AudioSource sound;
        [SerializeField] private AudioClip blackoutSound;
        [SerializeField, Min(0f)] private float blackoutSoundStartSeconds;
        [SerializeField, Range(0,1)] private float volume = .35f;
        [SerializeField] private string status = "Idle";
        [SerializeField] private bool running;
        private readonly List<Light> lights = new List<Light>();
        private readonly List<float> intensities = new List<float>();
        private readonly List<Light> essentialLights = new List<Light>();
        private readonly List<float> essentialIntensities = new List<float>();
        private DirectorPlanningAction action;
        private string token;
        private float startedAt;
        private bool ownsMemory;
        private bool controlsAmbient;
        private Color originalAmbientLight;
        private UnityEngine.Rendering.SphericalHarmonicsL2 originalAmbientProbe;

        public bool Supports(DirectorEventDefinition definition) =>
            isActiveAndEnabled && definition != null && definition.EventType == DirectorEventType.LocalBlackout &&
            sound != null && sound.isActiveAndEnabled && blackoutSound != null;

        public bool TryStart(DirectorPlanningAction candidate)
        {
            if (!Application.isPlaying || running || !Supports(candidate.definition) ||
                eligibility == null || reservations == null || memory == null || observation == null ||
                !memory.IsReady || directorLightsRoot == null || essentialLightsRoot == null) return false;
            if (blackoutSound.loadState != AudioDataLoadState.Loaded)
            { blackoutSound.LoadAudioData(); status = "Waiting for blackout audio"; return false; }
            if (!eligibility.TryReserveCandidate(candidate.definition,candidate.anchor,out token)) return false;
            var check = eligibility.Evaluate(candidate.definition,candidate.anchor,token);
            if (!check.eligible) { reservations.Release(token); token = null; status = check.reason; return false; }
            lights.Clear(); intensities.Clear();
            essentialLights.Clear(); essentialIntensities.Clear();
            foreach (var light in directorLightsRoot.GetComponentsInChildren<Light>(true))
            {
                if (light == null || !light.transform.IsChildOf(directorLightsRoot) ||
                    light.transform.IsChildOf(essentialLightsRoot)) continue;
                if (light.isActiveAndEnabled && light.intensity > 0)
                { lights.Add(light); intensities.Add(light.intensity); }
            }
            if (lights.Count == 0) { status = "No illuminated Director lights"; Release(); return false; }
            foreach (var light in essentialLightsRoot.GetComponentsInChildren<Light>(true))
            {
                if (light != null && light.isActiveAndEnabled && light.intensity > 0)
                { essentialLights.Add(light); essentialIntensities.Add(light.intensity); }
            }
            if (!memory.BeginEvent(candidate.definition.EventId,candidate.definition.EventType,
                candidate.anchorId,candidate.roomId)) { Release(); return false; }
            ownsMemory = true; action = candidate; running = true; startedAt = Time.time;
            controlsAmbient = ambientBlackoutMultiplier < 1f;
            originalAmbientLight = RenderSettings.ambientLight;
            originalAmbientProbe = RenderSettings.ambientProbe;
            reservations.Renew(token,candidate.definition.Duration+1);
            sound.transform.position = lights[0].transform.position;
            sound.Stop(); sound.clip = blackoutSound; sound.volume = volume; sound.loop = false; sound.pitch = 1f;
            sound.time = Mathf.Clamp(blackoutSoundStartSeconds, 0f, Mathf.Max(0f, blackoutSound.length - .05f));
            sound.ignoreListenerPause = false; sound.Play();
            ApplyBrightness(1f);
            memory.MarkEffectDelivered(candidate.definition.PredictedTensionIncrease);
            status = "Fading out hospital Director lights";
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
            // Duration includes both fades, keeping execution aligned with prediction.
            float fadeOut = Mathf.Min(.35f, duration * .25f);
            float fadeIn = Mathf.Min(.75f, duration * .25f);
            float recoveryStarts = duration - fadeIn;
            float multiplier;
            if (elapsed < fadeOut)
            {
                multiplier = 1f - Mathf.SmoothStep(0f, 1f, elapsed / fadeOut);
                status = "Fading out hospital Director lights";
            }
            else if (elapsed < recoveryStarts)
            {
                multiplier = 0f;
                status = "Hospital blackout";
            }
            else
            {
                multiplier = Mathf.SmoothStep(0f, 1f, (elapsed - recoveryStarts) / fadeIn);
                status = "Restoring hospital Director lights";
            }
            ApplyBrightness(multiplier);
            float recovery = Mathf.InverseLerp(recoveryStarts, duration, elapsed);
            sound.volume = volume * (1f - recovery);
        }
        private void ApplyBrightness(float multiplier)
        {
            for(int i=0;i<lights.Count;i++) if(lights[i]!=null) lights[i].intensity=intensities[i]*multiplier;
            float emergencyMultiplier = Mathf.Lerp(essentialBlackoutMultiplier, 1f, Mathf.Clamp01(multiplier));
            for (int i = 0; i < essentialLights.Count; i++)
                if (essentialLights[i] != null)
                {
                    bool hallway = hallwayEmergencyLights != null &&
                        System.Array.IndexOf(hallwayEmergencyLights, essentialLights[i]) >= 0;
                    if (hallway)
                    {
                        float blackoutIntensity = Mathf.Max(hallwayBlackoutMinimumIntensity,
                            essentialIntensities[i] * essentialBlackoutMultiplier * hallwayBlackoutMultiplier);
                        essentialLights[i].intensity = Mathf.Lerp(blackoutIntensity,
                            essentialIntensities[i], Mathf.Clamp01(multiplier));
                    }
                    else essentialLights[i].intensity = essentialIntensities[i] * emergencyMultiplier;
                }
            if (controlsAmbient)
            {
                float ambientMultiplier = Mathf.Lerp(ambientBlackoutMultiplier, 1f, Mathf.Clamp01(multiplier));
                Color ambient = originalAmbientLight.linear * ambientMultiplier;
                ambient.a = originalAmbientLight.a;
                RenderSettings.ambientLight = multiplier >= 1f ? originalAmbientLight : ambient.gamma;
                RenderSettings.ambientProbe = originalAmbientProbe * ambientMultiplier;
            }
        }
        private void Finish(DirectorEventOutcome outcome)
        {
            ApplyBrightness(1);
            controlsAmbient = false;
            if(sound!=null) {sound.Stop();sound.volume=volume;}
            running=false;
            if(ownsMemory && memory!=null) memory.FinishEvent(outcome);
            ownsMemory=false; Release();
            status=outcome.ToString();
        }
        private void Release()
        {
            if(reservations!=null && token!=null)reservations.Release(token);
            token=null;lights.Clear();intensities.Clear();
            essentialLights.Clear(); essentialIntensities.Clear();
        }
        private void OnDisable() { if(running || ownsMemory)Finish(DirectorEventOutcome.Cancelled); }
    }
}
