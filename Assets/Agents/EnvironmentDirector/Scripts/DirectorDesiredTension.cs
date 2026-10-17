using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [DisallowMultipleComponent]
    public sealed class DirectorDesiredTension : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DirectorFuzzifier fuzzifier;
        [SerializeField] private DirectorSugenoRules rules;
        [Tooltip("Use the same fuzzy settings asset as Director Fuzzifier. Only used for manual previews.")]
        [SerializeField] private DirectorFuzzySettings previewFuzzySettings;
        [SerializeField, Min(0.02f)] private float updateInterval = 0.1f;

        [Header("Live inference - display only")]
        [SerializeField] private bool outputValid;
        [SerializeField] private bool usedFallback;
        [SerializeField] private string status = "Waiting for Play mode";
        [SerializeField] private float sampledAt, desiredTension, totalActivation, weightedSum;
        [SerializeField] private List<DirectorRuleContribution> contributingRules = new List<DirectorRuleContribution>();

        [Header("Manual preview - separate from live decisions")]
        [SerializeField, Range(0f, 1f)] private float previewPressure = 0.4f;
        [SerializeField, Range(0f, 1f)] private float previewVulnerability = 0.2f;
        [SerializeField, Min(0f)] private float previewQuietSeconds = 45f;
        [SerializeField] private bool previewValid;
        [SerializeField] private string previewStatus = "Use the component menu to calculate";
        [SerializeField] private float previewDesiredTension, previewTotalActivation, previewWeightedSum;
        [SerializeField] private List<DirectorRuleContribution> previewContributions = new List<DirectorRuleContribution>();

        private float nextUpdateAt;
        // Consumers must check IsValid and SampledAt before using DesiredTension.
        public bool IsValid => isActiveAndEnabled && outputValid;
        public float DesiredTension => desiredTension;
        public float SampledAt => sampledAt;
        public IReadOnlyList<DirectorRuleContribution> Contributions => contributingRules.AsReadOnly();

        private void OnEnable() { nextUpdateAt = 0f; Invalidate("Waiting for valid fuzzy input"); }
        private void OnDisable() => Invalidate("Desired-tension inference disabled");
        private void LateUpdate()
        {
            if (Time.time < nextUpdateAt) return;
            nextUpdateAt = Time.time + Mathf.Max(0.02f, updateInterval);
            RefreshDesiredTension();
        }

        public void RefreshDesiredTension()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            if (fuzzifier == null || rules == null || fuzzifier.gameObject.scene != gameObject.scene)
            { Invalidate("Assign a same-scene Fuzzifier and the Rules asset"); return; }
            if (!DirectorFuzzyMembership.IsFinite(updateInterval) || updateInterval < 0.02f)
            { Invalidate("Update interval must be finite and at least 0.02 seconds"); return; }

            // Refresh propagates observation freshness, life-state and source-validity checks.
            fuzzifier.RefreshMemberships();
            if (!fuzzifier.IsValid) { Invalidate("Fuzzy input unavailable; normal planning blocked"); return; }
            var result = DirectorSugenoInference.Evaluate(rules, fuzzifier.Pressure,
                fuzzifier.Vulnerability, fuzzifier.QuietDuration, contributingRules);
            outputValid = result.IsValid;
            usedFallback = result.UsedFallback;
            status = result.Reason;
            desiredTension = result.DesiredTension;
            totalActivation = result.TotalActivation;
            weightedSum = result.WeightedSum;
            sampledAt = Time.time;
        }

        [ContextMenu("Preview Desired Tension From Manual Inputs")]
        private void PreviewDesiredTension()
        {
            previewValid = false;
            previewDesiredTension = previewTotalActivation = previewWeightedSum = 0f;
            previewContributions.Clear();
            if (previewFuzzySettings == null) { previewStatus = "Assign Preview Fuzzy Settings"; return; }
            if (!previewFuzzySettings.TryValidate(out var error)) { previewStatus = error; return; }
            if (!Unit(previewPressure) || !Unit(previewVulnerability) ||
                !DirectorFuzzyMembership.IsFinite(previewQuietSeconds) || previewQuietSeconds < 0f)
            { previewStatus = "Use finite pressure/vulnerability in 0–1 and non-negative quiet seconds"; return; }
            var result = DirectorSugenoInference.Evaluate(rules,
                DirectorFuzzyMembership.Evaluate(previewPressure, previewFuzzySettings.PressureKnots),
                DirectorFuzzyMembership.Evaluate(previewVulnerability, previewFuzzySettings.VulnerabilityKnots),
                DirectorFuzzyMembership.Evaluate(previewQuietSeconds, previewFuzzySettings.QuietSecondsKnots),
                previewContributions);
            previewValid = result.IsValid;
            previewStatus = result.Reason;
            previewDesiredTension = result.DesiredTension;
            previewTotalActivation = result.TotalActivation;
            previewWeightedSum = result.WeightedSum;
        }

        private static bool Unit(float value) => DirectorFuzzyMembership.IsFinite(value) && value >= 0f && value <= 1f;
        private void Invalidate(string reason)
        {
            outputValid = false; usedFallback = false; status = reason;
            sampledAt = desiredTension = totalActivation = weightedSum = 0f;
            contributingRules.Clear();
        }
    }
}
