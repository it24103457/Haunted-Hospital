using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    /// <summary>A stationary apparition with all three poses, voice and room-local darkness.</summary>
    [DisallowMultipleComponent]
    public sealed class DirectorApparitionExecutor : MonoBehaviour
    {
        // Scene binding for the reusable Director prefab.
        public void BindLighting(Transform director, Transform essential) { directorLightsRoot = director; essentialLightsRoot = essential; }

        [SerializeField] private DirectorEventEligibility eligibility;
        [SerializeField] private DirectorTargetReservations reservations;
        [SerializeField] private DirectorEventMemory memory;
        [SerializeField] private DirectorObservation observation;
        [SerializeField] private DirectorSpatialRegistry registry;
        [SerializeField] private Transform directorLightsRoot, essentialLightsRoot;
        [SerializeField] private GameObject apparitionPrefab;
        [SerializeField] private AnimationClip idleClip, joltClip, poseClip;
        [SerializeField, Range(0f, 2f)] private float revealIntensity = .7f;
        [SerializeField] private string status = "Idle";
        [SerializeField] private bool running;
        [SerializeField] private string currentAnimation;
        [SerializeField] private string targetRoom;
        [SerializeField] private int affectedLights;
        private const float FadeOut = .25f, DarkBeat = .15f, FadeIn = .75f, Blend = .28f;
        private const float RevealAt = FadeOut + DarkBeat;
        private readonly List<Light> lights = new List<Light>();
        private readonly List<float> intensities = new List<float>();
        private DirectorPlanningAction action;
        private string token;
        private bool ownsMemory, revealed, removed, monsterPlayed;
        private float startedAt;
        private GameObject appearance;
        private Animator animator;
        private DirectorApparitionVoice voice;
        private int animationStage;

        private float AnimationDuration => idleClip.length + joltClip.length + poseClip.length + Blend * 2;
        private AudioClip VoiceClip => apparitionPrefab == null ? null : apparitionPrefab.GetComponentInChildren<DirectorApparitionVoice>(true)?.Clip;
        private float ArrivalDuration => apparitionPrefab == null ? 0f : apparitionPrefab.GetComponentInChildren<DirectorApparitionVoice>(true).ArrivalDuration;
        private float MonsterAt => Mathf.Max(idleClip.length, ArrivalDuration);
        public float NextDuration => AssetsReady() ? RevealAt + Mathf.Max(AnimationDuration, MonsterAt + VoiceClip.length) + FadeIn : 0f;

        private bool AssetsReady() => apparitionPrefab != null && VoiceClip != null && VoiceClip.length > 0 &&
            idleClip != null && idleClip.length > 0 && joltClip != null && joltClip.length > 0 && poseClip != null && poseClip.length > 0;

        public bool Supports(DirectorEventDefinition definition) => isActiveAndEnabled && definition != null &&
            definition.EventType == DirectorEventType.DistantApparition && AssetsReady() && registry != null &&
            registry.isActiveAndEnabled && directorLightsRoot != null && essentialLightsRoot != null;

        public bool TryStart(DirectorPlanningAction candidate)
        {
            if (!Application.isPlaying || running || !Supports(candidate.definition) || eligibility == null ||
                reservations == null || memory == null || !memory.IsReady || observation == null) return false;
            if (VoiceClip.loadState != AudioDataLoadState.Loaded)
            { VoiceClip.LoadAudioData(); status = "Waiting for apparition voice"; return false; }
            if (!eligibility.TryReserveCandidate(candidate.definition, candidate.anchor, out token)) return false;
            var check = eligibility.Evaluate(candidate.definition, candidate.anchor, token);
            if (!check.eligible) { status = check.reason; Release(); return false; }
            // Authored light-group membership is authoritative; do not select lights by distance.
            lights.Clear(); intensities.Clear();
            foreach (var anchor in registry.Anchors)
            {
                if (anchor == null || anchor.Room != candidate.anchor.Room || anchor.TargetType != DirectorTargetType.DirectorLightGroup) continue;
                foreach (var light in anchor.DirectorLights)
                    if (light != null && light.isActiveAndEnabled && light.intensity > 0 &&
                        light.transform.IsChildOf(directorLightsRoot) && !light.transform.IsChildOf(essentialLightsRoot) && !lights.Contains(light))
                    { lights.Add(light); intensities.Add(light.intensity); }
            }
            if (lights.Count == 0) { status = "Apparition room has no illuminated Director lights"; Release(); return false; }
            if (!reservations.Renew(token, NextDuration + 2f)) { Release(); return false; }
            action = candidate;
            try
            {
                PrepareAppearance();
                if (!memory.BeginEvent(candidate.definition.EventId, candidate.definition.EventType, candidate.anchorId, candidate.roomId))
                { Cleanup(); Release(); return false; }
                ownsMemory = running = true;
                revealed = removed = monsterPlayed = false; animationStage = -1; startedAt = Time.time;
                targetRoom = candidate.roomId; affectedLights = lights.Count;
                status = "Fading out " + targetRoom;
                return true;
            }
            catch (Exception exception)
            { Finish(DirectorEventOutcome.Cancelled); status = "Apparition could not start: " + exception.Message; return false; }
        }

        private void PrepareAppearance()
        {
            appearance = new GameObject("Director_Apparition_" + action.anchorId);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(appearance, gameObject.scene);
            appearance.SetActive(false);
            appearance.transform.position = action.anchor.Position;
            Vector3 facing = observation.Current.PlayerPosition - action.anchor.Position;
            facing.y = 0;
            appearance.transform.rotation = facing.sqrMagnitude > .001f ? Quaternion.LookRotation(facing) : Quaternion.identity;
            var model = Instantiate(apparitionPrefab, appearance.transform, false);
            // Preserve the prefab's import-axis correction and authored scale.
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = apparitionPrefab.transform.localRotation;
            model.transform.localScale = apparitionPrefab.transform.localScale;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var obstacle in model.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>(true)) obstacle.enabled = false;
            foreach (var body in model.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.detectCollisions = false; }
            animator = model.GetComponentInChildren<Animator>(true);
            voice = model.GetComponentInChildren<DirectorApparitionVoice>(true);
            if (animator == null || voice == null || voice.Clip == null) throw new InvalidOperationException("Prefab needs Animator and voice.");
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // A small shadowless fill reveals the figure without relighting the room.
            var fill = new GameObject("ApparitionRevealLight");
            fill.transform.SetParent(appearance.transform, false);
            fill.transform.localPosition = new Vector3(0f, 1.2f, 1.2f);
            fill.transform.localRotation = Quaternion.LookRotation(new Vector3(0f, -.1f, -1f));
            var light = fill.AddComponent<Light>();
            light.type = LightType.Spot; light.range = 3f; light.spotAngle = 65f;
            light.intensity = revealIntensity; light.color = new Color(.65f, .75f, 1f); light.shadows = LightShadows.None;
        }

        private void Update()
        {
            if (!running) return;
            // Gameplay restrictions apply only to admission. Pause preserves this event.
            // The existing pause menu freezes both scaled time and the audio DSP clock.
            if (Time.timeScale <= 0f || AudioListener.pause) return;
            if (memory == null || directorLightsRoot == null || essentialLightsRoot == null || (!removed && (appearance == null || animator == null || voice == null)))
            { Finish(DirectorEventOutcome.Cancelled); return; }
            float elapsed = Time.time - startedAt;
            float recoveryAt = NextDuration - FadeIn;
            if (elapsed < FadeOut) ApplyBrightness(1f - Mathf.SmoothStep(0, 1, elapsed / FadeOut));
            else if (elapsed < recoveryAt) ApplyBrightness(0);
            else ApplyBrightness(Mathf.SmoothStep(0, 1, (elapsed - recoveryAt) / FadeIn));
            if (!revealed && elapsed >= RevealAt)
            {
                // Placement was checked at admission; committed effects finish even if the player moves.
                startedAt = Time.time - RevealAt;
                elapsed = RevealAt;
                appearance.SetActive(true);
                // Prevent the standalone jolt-to-idle transition from interrupting this sequence.
                animator.SetBool("DirectorSequence", true);
                animator.SetBool("PulsePose", false); animator.ResetTrigger("Jolt");
                voice.PlayArrival();
                revealed = true;
                memory.MarkEffectDelivered(action.definition.PredictedTensionIncrease);
            }
            if (revealed && !removed)
            {
                if (eligibility != null && eligibility.IsVisible(action.anchor))
                {
                    memory.RecordApparitionSighting();
                    memory.TryApplyActiveSanityLoss();
                }
                float age = elapsed - RevealAt;
                if (!monsterPlayed && age >= MonsterAt)
                { voice.PlayVoice(false); monsterPlayed = true; }
                int stage = age < idleClip.length ? 0 : age < idleClip.length + Blend + joltClip.length ? 1 : 2;
                float pointing = stage == 1 ? 0f : Mathf.SmoothStep(0f, 1f, age / .8f);
                voice.PointAt(observation.Current.PlayerPosition + Vector3.up * 1.2f,
                    observation.Current.IsValid ? pointing : 0f);
                if (stage != animationStage)
                {
                    animationStage = stage;
                    currentAnimation = stage == 0 ? "UnsettlingIdle" : stage == 1 ? "BodyJolt" : "PulsePose";
                    animator.SetBool("PulsePose", stage == 2);
                    animator.ResetTrigger("Jolt");
                    if (stage == 0) animator.Play("Base Layer." + currentAnimation, 0, 0);
                    else animator.CrossFadeInFixedTime("Base Layer." + currentAnimation, Blend, 0, 0);
                }
                status = "Showing " + currentAnimation + " in " + targetRoom;
            }
            if (!removed && elapsed >= recoveryAt)
            { voice.PlayDisappearance(); Cleanup(); removed = true; status = "Restoring " + targetRoom; }
            if (elapsed >= NextDuration) Finish(DirectorEventOutcome.Completed);
        }

        private void ApplyBrightness(float multiplier)
        {
            for (int i = 0; i < lights.Count; i++)
                if (lights[i] != null) lights[i].intensity = intensities[i] * multiplier;
        }
        private void Cleanup()
        {
            if (voice != null) voice.StopVoice();
            if (appearance != null) { appearance.SetActive(false); Destroy(appearance); }
            appearance = null; animator = null; voice = null;
        }
        private void Finish(DirectorEventOutcome outcome)
        {
            ApplyBrightness(1f); Cleanup(); running = false;
            if (ownsMemory && memory != null) memory.FinishEvent(outcome);
            ownsMemory = false; Release(); status = outcome.ToString();
        }
        private void Release()
        {
            if (reservations != null && token != null) reservations.Release(token);
            token = null; lights.Clear(); intensities.Clear();
        }
        private void OnDisable() { if (running || ownsMemory) Finish(DirectorEventOutcome.Cancelled); }
    }
}
