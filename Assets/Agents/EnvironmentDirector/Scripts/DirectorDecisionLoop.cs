using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    /// <summary>Plans repeatedly and dispatches supported effects to their executors.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class DirectorDecisionLoop : MonoBehaviour
    {
        [SerializeField] private DirectorPlannerPreview planner;
        [SerializeField] private DirectorObservation observation;
        [SerializeField] private DirectorEventEligibility eligibility;
        [SerializeField] private DirectorEventMemory eventMemory;
        [SerializeField] private DirectorFlickerExecutor executor;
        [SerializeField] private DirectorBlackoutExecutor blackoutExecutor;
        [SerializeField] private DirectorPhoneExecutor phoneExecutor;
        [SerializeField] private DirectorWhisperExecutor whisperExecutor;
        [SerializeField] private DirectorAnnouncementExecutor announcementExecutor;
        [SerializeField] private DirectorKnockExecutor knockExecutor;
        [SerializeField] private DirectorApparitionExecutor apparitionExecutor;
        [SerializeField, Min(0.1f)] private float decisionInterval = .25f;
        [SerializeField, Min(0.02f)] private float maximumObservationAge = 0.5f;
        [Header("Runtime decisions and execution")]
        [SerializeField] private string status = "Waiting for Play mode";
        [SerializeField] private int planningAttempts;
        [SerializeField] private float decidedAt;
        [SerializeField] private string decisionRoom;
        [SerializeField] private string recommendedAction;
        [SerializeField] private bool decisionAvailable;
        [SerializeField] private DirectorPlanningAction decision;
        [SerializeField] private DirectorSearchPlan latestPlan;
        private float nextDecisionAt;
        private string lastPlanningRoom;

        private void OnEnable() { Clear("Waiting for observations"); nextDecisionAt = 0; }
        private void OnDisable() => Clear("Decision loop disabled");

        private void LateUpdate()
        {
            if (Time.timeScale <= 0f || AudioListener.pause) { Clear("Paused"); return; }
            if (!Ready(out var reason)) { Clear(reason); return; }
            if (lastPlanningRoom != observation.Current.RoomId)
                nextDecisionAt = 0f;
            if (decisionAvailable && decisionRoom != observation.Current.RoomId)
                Clear("Player changed room; waiting for next planning tick");
            if (Time.time < nextDecisionAt) return;
            nextDecisionAt = Time.time + decisionInterval;
            lastPlanningRoom = observation.Current.RoomId;
            planningAttempts++;
            latestPlan = planner.BuildLivePlan(executor, blackoutExecutor, phoneExecutor, whisperExecutor, announcementExecutor, knockExecutor, apparitionExecutor);
            decisionAvailable = false;
            recommendedAction = "None";
            if (latestPlan == null || !latestPlan.valid || latestPlan.manualOnly || latestPlan.steps.Count == 0)
            { status = latestPlan == null ? "Planner returned no plan" : latestPlan.stopReason; return; }
            decision = latestPlan.steps[0].action;
            decidedAt = Time.time;
            decisionRoom = observation.Current.RoomId;
            recommendedAction = decision.Label;
            if (!decision.IsRest)
            {
                var check = eligibility.Evaluate(decision.definition, decision.anchor);
                if (!check.eligible) { status = "First action currently blocked: " + check.reason; return; }
            }
            decisionAvailable = true;
            status = decision.IsRest ? "Rest recommended; inspect eligibility for blocked event reasons" : "Event recommended";
            if (!decision.IsRest && ((executor != null && executor.Supports(decision.definition) && executor.TryStart(decision)) ||
                (blackoutExecutor != null && blackoutExecutor.Supports(decision.definition) && blackoutExecutor.TryStart(decision)) ||
                (phoneExecutor != null && phoneExecutor.Supports(decision.definition) && phoneExecutor.TryStart(decision)) ||
                (whisperExecutor != null && whisperExecutor.Supports(decision.definition) && whisperExecutor.TryStart(decision)) ||
                (announcementExecutor != null && announcementExecutor.Supports(decision.definition) && announcementExecutor.TryStart(decision)) ||
                (knockExecutor != null && knockExecutor.Supports(decision.definition) && knockExecutor.TryStart(decision)) ||
                (apparitionExecutor != null && apparitionExecutor.Supports(decision.definition) && apparitionExecutor.TryStart(decision))))
            { decisionAvailable = false; status = "Executing " + decision.Label; }
        }

        // Future executors must still reserve and validate immediately before starting an effect.
        public bool TryGetCurrentDecision(out DirectorPlanningAction action)
        {
            action = default;
            if (!isActiveAndEnabled || !decisionAvailable || Time.timeScale <= 0 || AudioListener.pause || !Ready(out _) ||
                Time.time - decidedAt > decisionInterval || decisionRoom != observation.Current.RoomId)
                return false;
            if (!decision.IsRest && !eligibility.Evaluate(decision.definition, decision.anchor).eligible)
                return false;
            action = decision;
            return true;
        }

        private bool Ready(out string reason)
        {
            reason = null;
            if (planner == null || !planner.isActiveAndEnabled || observation == null ||
                eligibility == null || !eligibility.isActiveAndEnabled || eventMemory == null || !eventMemory.IsReady)
                reason = "Missing or disabled planning references";
            else if (!Finite(decisionInterval) || decisionInterval < 0.1f ||
                !Finite(maximumObservationAge) || maximumObservationAge <= 0)
                reason = "Invalid timing settings";
            else
            {
                var sample = observation.Current;
                float age = Time.time - sample.SampledAt;
                if (!sample.IsValid || age < 0 || age > maximumObservationAge) reason = "Observation unavailable or stale";
                else if (!sample.IsAlive) reason = "Player is dead";
                else if (sample.Victory == DirectorSignalState.Active) reason = "Victory active";
                else if (eventMemory.EventActive) reason = "Waiting for active event to finish";
                else if (!eventMemory.GlobalIntervalReady) reason = "Waiting for minimum event spacing";
            }
            return reason == null;
        }

        private void Clear(string message)
        {
            decisionAvailable = false; decision = default; recommendedAction = "None";
            decisionRoom = ""; latestPlan = null; status = message;
        }
        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    }
}

