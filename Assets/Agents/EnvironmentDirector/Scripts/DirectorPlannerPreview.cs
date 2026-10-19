using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [DisallowMultipleComponent]
    public sealed class DirectorPlannerPreview : MonoBehaviour
    {
        [SerializeField] private DirectorSettings directorSettings;
        [SerializeField] private DirectorTensionSettings tensionSettings;
        [SerializeField] private DirectorScoringSettings scoringSettings;
        [SerializeField] private DirectorPredictionPreview prediction;
        [SerializeField] private DirectorDesiredTension desiredTension;
        [SerializeField] private DirectorEventEligibility eligibility;
        [SerializeField] private DirectorEventMemory eventMemory;
        [Header("Manual sandbox only - never used by live planning")]
        [SerializeField, Range(0f, 1f)] private float manualDesiredTension = 0.55f;
        [SerializeField, Range(0f, 1f)] private float manualVulnerability = 0.2f;
        [SerializeField, Range(0f, 1f)] private float manualEventPressure = 0.4f;
        [SerializeField, Min(0f)] private float manualDamageRecoveryRemaining;
        [SerializeField, Min(0f)] private float manualQuietSeconds = 20f;
        [SerializeField, Min(0f)] private float manualGlobalCooldown;
        [Tooltip("Empty means Rest only. Each entry uses a synthetic anchor for mathematical search testing.")]
        [SerializeField] private DirectorEventDefinition[] manualEvents = Array.Empty<DirectorEventDefinition>();
        [SerializeField] private DirectorPlanningCooldown[] manualCooldowns = Array.Empty<DirectorPlanningCooldown>();
        [Header("Output - component menu refresh only")]
        [SerializeField] private string status = "No plan requested";
        [SerializeField] private string recommendedFirstAction;
        [SerializeField] private DirectorSearchPlan plan;
        [SerializeField] private List<DirectorEligibilityResult> liveCandidates = new List<DirectorEligibilityResult>();

        [ContextMenu("Preview Manual Search (synthetic targets, no effects)")]
        private void PreviewManual()
        {
            plan = null;
            recommendedFirstAction = "";
            liveCandidates.Clear();
            if (!DirectorStatePredictor.TryCreate(manualVulnerability, manualEventPressure,
                manualDamageRecoveryRemaining, manualQuietSeconds, manualGlobalCooldown, manualCooldowns,
                directorSettings, tensionSettings, out var source, out status)) return;
            var actions = new List<DirectorPlanningAction>();
            if (manualEvents != null)
                foreach (var definition in manualEvents)
                    if (definition != null) actions.Add(new DirectorPlanningAction { definition = definition,
                        anchorId = "manual_" + definition.EventId, roomId = "manual_sandbox" });
            plan = DirectorGreedyPlanner.Search(source, actions, Array.Empty<DirectorEventRecord>(),
                manualDesiredTension, 0f, true, directorSettings, tensionSettings, scoringSettings);
            Display();
        }

        // Captures live context without writing preview fields; future scheduling can call this.
        public DirectorSearchPlan BuildLivePlan(DirectorFlickerExecutor executor = null, DirectorBlackoutExecutor blackoutExecutor = null)
        {
            var failed = new DirectorSearchPlan { stopReason = "Requires Play mode and valid references" };
            if (!Application.isPlaying || !isActiveAndEnabled || prediction == null || desiredTension == null ||
                eligibility == null || !eligibility.isActiveAndEnabled || eventMemory == null || !eventMemory.IsReady ||
                tensionSettings == null || !tensionSettings.IsValid()) return failed;
            if (!prediction.TryCaptureLive(out var source, out var reason)) { failed.stopReason = reason; return failed; }
            float now = Time.time;
            float age = now - desiredTension.SampledAt;
            if (!desiredTension.IsValid || age < 0f || age > tensionSettings.MaximumObservationAge)
            { failed.stopReason = "Desired tension unavailable or stale"; return failed; }
            var options = new List<DirectorEligibilityResult>();
            eligibility.CollectPlanningCandidates(options);
            var actions = new List<DirectorPlanningAction>();
            foreach (var option in options)
                if (option.eligible && !option.IsRest && ((executor == null && blackoutExecutor == null) || (executor != null && executor.Supports(option.definition)) || (blackoutExecutor != null && blackoutExecutor.Supports(option.definition)))) actions.Add(new DirectorPlanningAction
                { definition = option.definition, anchor = option.anchor,
                    anchorId = option.anchor.AnchorId, roomId = option.anchor.Room.RoomId });
            return DirectorGreedyPlanner.Search(source, actions, new List<DirectorEventRecord>(eventMemory.History),
                desiredTension.DesiredTension, now, false, directorSettings, tensionSettings, scoringSettings,
                scoreWholeTrajectory: executor != null || blackoutExecutor != null);
        }

        [ContextMenu("Preview Live Search (Play mode, no effects)")]
        private void PreviewLive()
        {
            liveCandidates.Clear();
            plan = BuildLivePlan();
            if (Application.isPlaying && eligibility != null && eligibility.isActiveAndEnabled)
                eligibility.CollectPlanningCandidates(liveCandidates, true);
            Display();
        }

        private void Display()
        {
            recommendedFirstAction = plan != null && plan.valid && plan.steps.Count > 0
                ? plan.steps[0].action.Label : "None";
            status = plan == null ? "No plan" : (plan.manualOnly ? "MANUAL ONLY: " : "Live snapshot: ") + plan.stopReason;
        }
    }
}
