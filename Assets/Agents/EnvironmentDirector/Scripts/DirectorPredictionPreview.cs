using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [Serializable]
    public struct DirectorPredictionRow
    {
        public string action;
        public bool valid;
        public string reason;
        public DirectorPlanningState predictedState;
    }

    [DisallowMultipleComponent]
    public sealed class DirectorPredictionPreview : MonoBehaviour
    {
        [SerializeField] private DirectorSettings directorSettings;
        [SerializeField] private DirectorTensionSettings tensionSettings;
        [SerializeField] private DirectorObservation observation;
        [SerializeField] private DirectorEventMemory eventMemory;
        [Header("Manual inputs - separate from live gameplay")]
        [SerializeField, Range(0f, 1f)] private float manualVulnerability = 0.2f;
        [SerializeField, Range(0f, 1f)] private float manualEventPressure = 0.4f;
        [SerializeField, Min(0f)] private float manualDamageRecoveryRemaining;
        [SerializeField, Min(0f)] private float manualQuietSeconds = 20f;
        [SerializeField, Min(0f)] private float manualGlobalCooldown;
        [SerializeField] private DirectorPlanningCooldown[] manualCooldowns = Array.Empty<DirectorPlanningCooldown>();
        [Header("Preview output - refreshed by component menu only")]
        [SerializeField] private string status = "Choose a preview from the component menu";
        [SerializeField] private DirectorPlanningState startingState;
        [SerializeField] private List<DirectorPredictionRow> predictions = new List<DirectorPredictionRow>();

        [ContextMenu("Preview Predictions From Manual Inputs")]
        private void PreviewManual()
        {
            predictions.Clear();
            if (DirectorStatePredictor.TryCreate(manualVulnerability, manualEventPressure,
                manualDamageRecoveryRemaining, manualQuietSeconds, manualGlobalCooldown, manualCooldowns,
                directorSettings, tensionSettings, out startingState, out status)) BuildRows();
        }

        // Usable by the later planner: reading this snapshot never starts/finishes a real event.
        public bool TryCaptureLive(out DirectorPlanningState state, out string reason)
        {
            state = null;
            reason = "Live capture requires Play mode and ready references";
            if (!Application.isPlaying || !isActiveAndEnabled || observation == null || eventMemory == null ||
                !eventMemory.IsReady || directorSettings == null || tensionSettings == null || !tensionSettings.IsValid()) return false;
            if (eventMemory.EventActive) { reason = "Wait for the active event to finish before planning"; return false; }
            var snapshot = observation.Current;
            float age = Time.time - snapshot.SampledAt;
            if (!snapshot.IsValid || !snapshot.IsAlive || age < 0f || age > tensionSettings.MaximumObservationAge)
            { reason = "Player observation unavailable, dead or stale"; return false; }
            float vulnerability = ((1f - snapshot.HealthFraction) * tensionSettings.HealthWeight +
                (1f - snapshot.SanityFraction) * tensionSettings.SanityWeight) /
                (tensionSettings.HealthWeight + tensionSettings.SanityWeight);
            float damage = snapshot.HasReceivedDamage
                ? Mathf.Max(0f, directorSettings.DamageRecoverySeconds - snapshot.SecondsSinceDamage) : 0f;
            float sinceEnd = eventMemory.SecondsSinceEventEnded;
            float global = sinceEnd < 0f ? 0f : Mathf.Max(0f, directorSettings.MinimumTimeBetweenEvents - sinceEnd);
            var cooldowns = new List<DirectorPlanningCooldown>();
            foreach (var definition in directorSettings.Events)
                if (definition != null) cooldowns.Add(new DirectorPlanningCooldown
                    { eventId = definition.EventId, remaining = eventMemory.CooldownRemaining(definition.EventId) });
            return DirectorStatePredictor.TryCreate(Mathf.Clamp01(vulnerability), eventMemory.PlanningEventPressure,
                damage, eventMemory.QuietSeconds, global, cooldowns.ToArray(), directorSettings,
                tensionSettings, out state, out reason, snapshot.SanityFraction, snapshot.MaxSanity);
        }

        [ContextMenu("Preview Predictions From Live State (Play mode)")]
        private void PreviewLive()
        {
            predictions.Clear();
            if (TryCaptureLive(out startingState, out status)) BuildRows();
        }

        private void BuildRows()
        {
            bool valid = DirectorStatePredictor.TryPredictRest(startingState, directorSettings, tensionSettings,
                out var rest, out var reason);
            predictions.Add(new DirectorPredictionRow { action = "Rest", valid = valid, reason = reason, predictedState = rest });
            foreach (var definition in directorSettings.Events)
            {
                if (definition == null) continue;
                valid = DirectorStatePredictor.TryPredictEvent(startingState, definition, directorSettings,
                    tensionSettings, out var next, out reason);
                predictions.Add(new DirectorPredictionRow { action = definition.EventId, valid = valid,
                    reason = reason, predictedState = next });
            }
            status = "Predictions generated. These are mathematical outcomes, not live eligibility or execution.";
        }
    }
}
