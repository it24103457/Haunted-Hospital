using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [Serializable]
    public struct DirectorRuleContribution
    {
        public string ruleId;
        public DirectorFuzzyLevel pressure, vulnerability, quietDuration;
        public float activation;
        public float ruleOutput;
        public float weightedOutput;
    }

    public readonly struct DirectorSugenoResult
    {
        public bool IsValid { get; }
        public bool UsedFallback => !IsValid;
        public float DesiredTension { get; }
        public float TotalActivation { get; }
        public float WeightedSum { get; }
        public string Reason { get; }
        public DirectorSugenoResult(bool valid, float target, float activation, float sum, string reason)
        { IsValid = valid; DesiredTension = target; TotalActivation = activation; WeightedSum = sum; Reason = reason; }
    }

    // Zero-order Sugeno: constant consequents, product AND, normalized weighted sum.
    // Pure calculation; can be reused by the future planner without touching the scene.
    public static class DirectorSugenoInference
    {
        public static DirectorSugenoResult Evaluate(DirectorSugenoRules settings,
            DirectorMembershipDegrees pressure, DirectorMembershipDegrees vulnerability,
            DirectorMembershipDegrees quiet, List<DirectorRuleContribution> contributions = null)
        {
            contributions?.Clear();
            float fallback = settings != null && Unit(settings.FallbackTension) ? settings.FallbackTension : 0f;
            if (settings == null) return Invalid(fallback, "Rules asset is missing");
            if (!settings.TryValidate(out var error)) return Invalid(fallback, error);
            if (!Valid(pressure) || !Valid(vulnerability) || !Valid(quiet))
                return Invalid(fallback, "Memberships must be finite values within 0–1");

            double total = 0, sum = 0;
            foreach (var rule in settings.Rules)
            {
                float activation = Get(pressure, rule.pressure) * Get(vulnerability, rule.vulnerability) * Get(quiet, rule.quietDuration);
                float weighted = activation * rule.desiredTension;
                total += activation;
                sum += weighted;
                if (activation > 0f) contributions?.Add(new DirectorRuleContribution {
                    ruleId = rule.Id, pressure = rule.pressure, vulnerability = rule.vulnerability,
                    quietDuration = rule.quietDuration, activation = activation,
                    ruleOutput = rule.desiredTension, weightedOutput = weighted
                });
            }
            if (total <= 0.000001) return Invalid(fallback, "No meaningful rule activation; normal planning must remain blocked");
            return new DirectorSugenoResult(true, Mathf.Clamp01((float)(sum / total)),
                (float)total, (float)sum, "Ready");
        }

        private static DirectorSugenoResult Invalid(float fallback, string reason) =>
            new DirectorSugenoResult(false, fallback, 0f, 0f, reason);
        private static bool Unit(float value) => DirectorFuzzyMembership.IsFinite(value) && value >= 0f && value <= 1f;
        private static bool Valid(DirectorMembershipDegrees d) => Unit(d.Low) && Unit(d.Medium) && Unit(d.High);
        private static float Get(DirectorMembershipDegrees d, DirectorFuzzyLevel level) =>
            level == DirectorFuzzyLevel.Low ? d.Low : level == DirectorFuzzyLevel.Medium ? d.Medium : d.High;
    }
}
