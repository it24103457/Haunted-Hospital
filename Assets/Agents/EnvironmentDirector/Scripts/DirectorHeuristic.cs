using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [Serializable]
    public struct DirectorScoreBreakdown
    {
        public bool valid;
        public string reason;
        public float targetDifference;
        public float overshootPenalty;
        public float eventRepetitionPenalty;
        public float anchorRepetitionPenalty;
        public float total;
    }

    [Serializable]
    public struct DirectorRepetitionEvidence
    {
        public float eventRecency;
        public float anchorRecency;
    }

    // Pure heuristic for bounded greedy best-first search. Lower scores are preferable.
    // This is not an admissible A* distance estimate and never authorizes execution.
    public static class DirectorHeuristic
    {
        public static DirectorScoreBreakdown Score(float predictedPressure, float desiredTension,
            float vulnerability, bool isRest, DirectorRepetitionEvidence repetition, DirectorScoringSettings settings)
        {
            var result = new DirectorScoreBreakdown { reason = "Invalid scoring settings or input", total = float.PositiveInfinity };
            if (settings == null || !settings.IsValid() || !Unit(predictedPressure) || !Unit(desiredTension) ||
                !Unit(vulnerability) || !Unit(repetition.eventRecency) || !Unit(repetition.anchorRecency)) return result;
            result.targetDifference = Mathf.Abs(predictedPressure - desiredTension) * settings.TargetDifferenceWeight;
            result.overshootPenalty = Mathf.Max(0f, predictedPressure - desiredTension) * settings.OvershootWeight *
                (1f + vulnerability * settings.VulnerabilityOvershootMultiplier);
            result.eventRepetitionPenalty = isRest ? 0f : repetition.eventRecency * settings.EventRepetitionWeight;
            result.anchorRepetitionPenalty = isRest ? 0f : repetition.anchorRecency * settings.AnchorRepetitionWeight;
            result.total = result.targetDifference + result.overshootPenalty +
                result.eventRepetitionPenalty + result.anchorRepetitionPenalty;
            result.valid = !float.IsNaN(result.total) && !float.IsInfinity(result.total);
            result.reason = result.valid ? "Scored; lower is preferable" : "Score overflow; reject candidate";
            if (!result.valid) result.total = float.PositiveInfinity;
            return result;
        }

        // Read the supplied snapshot only. Future search can also supply branch history
        // containing predicted deliveries with simulated timestamps.
        public static DirectorRepetitionEvidence GetRepetition(IReadOnlyList<DirectorEventRecord> history,
            DirectorEventType eventType, string anchorId, float evaluatedAt, float windowSeconds)
        {
            if (history == null || !Finite(evaluatedAt) || !Finite(windowSeconds) || windowSeconds <= 0f)
                return new DirectorRepetitionEvidence { eventRecency = float.NaN, anchorRecency = float.NaN };
            var result = new DirectorRepetitionEvidence();
            foreach (var record in history)
            {
                if (record.debugRecord || !record.effectDelivered) continue;
                if (!Finite(record.endedAt) || record.endedAt > evaluatedAt) continue;
                float recency = Mathf.Clamp01(1f - (evaluatedAt - record.endedAt) / windowSeconds);
                if (record.eventType == eventType) result.eventRecency = Mathf.Clamp01(result.eventRecency + recency);
                if (!string.IsNullOrEmpty(anchorId) && record.anchorId == anchorId)
                    result.anchorRecency = Mathf.Clamp01(result.anchorRecency + recency);
            }
            return result;
        }

        private static bool Unit(float value) => Finite(value) && value >= 0f && value <= 1f;
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
