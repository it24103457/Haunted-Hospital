using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [Serializable]
    public struct DirectorScoredCandidate
    {
        public string action;
        public DirectorEventAnchor anchor;
        public bool eligible;
        public string reason;
        public float predictedPressure;
        public DirectorRepetitionEvidence repetition;
        public DirectorScoreBreakdown score;
    }

    [DisallowMultipleComponent]
    public sealed class DirectorScoringPreview : MonoBehaviour
    {
        [SerializeField] private DirectorScoringSettings scoringSettings;
        [SerializeField] private DirectorSettings directorSettings;
        [SerializeField] private DirectorTensionSettings tensionSettings;
        [SerializeField] private DirectorPredictionPreview prediction;
        [SerializeField] private DirectorDesiredTension desiredTension;
        [SerializeField] private DirectorEventEligibility eligibility;
        [SerializeField] private DirectorEventMemory eventMemory;
        [Header("Manual score inputs - no live eligibility implied")]
        [SerializeField, Range(0f, 1f)] private float manualPredictedPressure = 0.58f;
        [SerializeField, Range(0f, 1f)] private float manualDesiredTension = 0.55f;
        [SerializeField, Range(0f, 1f)] private float manualVulnerability = 0.2f;
        [SerializeField] private bool manualIsRest;
        [SerializeField, Range(0f, 1f)] private float manualEventRecency;
        [SerializeField, Range(0f, 1f)] private float manualAnchorRecency;
        [Header("Manual output")]
        [SerializeField] private DirectorScoreBreakdown manualScore;
        [Header("Live preview - refresh through component menu")]
        [SerializeField] private string status = "No preview requested";
        [SerializeField] private float targetUsed;
        [SerializeField] private int bestCandidateIndex = -1;
        [SerializeField] private List<DirectorScoredCandidate> candidates = new List<DirectorScoredCandidate>();

        [ContextMenu("Preview Score From Manual Inputs")]
        private void PreviewManual()
        {
            manualScore = DirectorHeuristic.Score(manualPredictedPressure, manualDesiredTension,
                manualVulnerability, manualIsRest, new DirectorRepetitionEvidence
                { eventRecency = manualEventRecency, anchorRecency = manualAnchorRecency }, scoringSettings);
        }

        [ContextMenu("Preview Live Candidate Scores (Play mode, no effects)")]
        private void PreviewLive()
        {
            candidates.Clear();
            bestCandidateIndex = -1;
            targetUsed = 0f;
            status = "Requires Play mode and valid references";
            if (!Application.isPlaying || !isActiveAndEnabled || scoringSettings == null || !scoringSettings.IsValid() ||
                prediction == null || desiredTension == null || eligibility == null || !eligibility.isActiveAndEnabled ||
                eventMemory == null || !eventMemory.IsReady || tensionSettings == null || !tensionSettings.IsValid()) return;
            if (!prediction.TryCaptureLive(out var source, out status)) return;
            float age = Time.time - desiredTension.SampledAt;
            if (!desiredTension.IsValid || age < 0f || age > tensionSettings.MaximumObservationAge)
            { status = "Desired tension unavailable or stale"; return; }
            targetUsed = desiredTension.DesiredTension;
            // Freeze inputs for this preview. Do not refresh inference or advance event memory.
            var history = new List<DirectorEventRecord>(eventMemory.History);
            float now = Time.time;
            var options = new List<DirectorEligibilityResult>();
            eligibility.CollectCandidates(options, true);
            foreach (var option in options)
            {
                var row = new DirectorScoredCandidate { action = option.IsRest ? "Rest" :
                    option.definition == null ? "Missing event" : option.definition.EventId,
                    anchor = option.anchor, eligible = option.eligible, reason = option.reason,
                    score = new DirectorScoreBreakdown { valid = false, reason = "Not scored", total = float.PositiveInfinity } };
                if (option.eligible)
                {
                    DirectorPlanningState next;
                    string reason;
                    bool valid = option.IsRest
                        ? DirectorStatePredictor.TryPredictRest(source, directorSettings, tensionSettings, out next, out reason)
                        : DirectorStatePredictor.TryPredictEvent(source, option.definition, directorSettings, tensionSettings, out next, out reason);
                    if (valid)
                    {
                        row.predictedPressure = next.EstimatedPressure;
                        // Recency at action selection time; waiting through an effect must not erase its repetition penalty.
                        row.repetition = option.IsRest ? default : DirectorHeuristic.GetRepetition(history,
                            option.definition.EventType, option.anchor.AnchorId, now, scoringSettings.RepetitionWindowSeconds);
                        row.score = DirectorHeuristic.Score(next.EstimatedPressure, targetUsed,
                            next.Vulnerability, option.IsRest, row.repetition, scoringSettings,
                            !option.IsRest && option.definition.IsApparition
                                ? DirectorHeuristic.ApparitionPreference(eventMemory.SecondsSinceApparitionSeen, scoringSettings) : 0f);
                        row.reason = row.score.reason;
                    }
                    else row.reason = reason;
                }
                int index = candidates.Count;
                candidates.Add(row);
                if (row.score.valid && (bestCandidateIndex < 0 || row.score.total < candidates[bestCandidateIndex].score.total))
                    bestCandidateIndex = index;
            }
            status = bestCandidateIndex < 0 ? "No valid score" :
                "Scores generated; best index is informational only. No action executed. Equal scores retain catalogue order.";
        }
    }
}
