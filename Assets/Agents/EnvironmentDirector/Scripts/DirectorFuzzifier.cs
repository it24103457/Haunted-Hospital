using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [DisallowMultipleComponent]
    public sealed class DirectorFuzzifier : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DirectorTensionEstimator tensionEstimator;
        [SerializeField] private DirectorEventMemory eventMemory;
        [SerializeField] private DirectorFuzzySettings settings;
        [SerializeField, Min(0.02f)] private float updateInterval = 0.1f;

        [Header("Runtime readings - refreshed automatically")]
        [SerializeField] private bool outputValid;
        [SerializeField] private string status = "Waiting for Play mode";
        [SerializeField] private float sampledAt;
        [SerializeField] private float pressureInput;
        [SerializeField] private float vulnerabilityInput;
        [SerializeField] private float quietSecondsInput;
        [SerializeField] private DirectorMembershipDegrees pressure;
        [SerializeField] private DirectorMembershipDegrees vulnerability;
        [Tooltip("Low = Short, Medium = Medium, High = Long.")]
        [SerializeField] private DirectorMembershipDegrees quietDuration;

        [Header("Manual preview - separate from live output")]
        [SerializeField, Range(0f, 1f)] private float previewPressure = 0.4f;
        [SerializeField, Range(0f, 1f)] private float previewVulnerability = 0.2f;
        [SerializeField, Min(0f)] private float previewQuietSeconds = 45f;
        [SerializeField] private DirectorMembershipDegrees pressurePreview;
        [SerializeField] private DirectorMembershipDegrees vulnerabilityPreview;
        [SerializeField] private DirectorMembershipDegrees quietPreview;

        private float nextUpdateAt;
        public bool IsValid => isActiveAndEnabled && outputValid;
        public float SampledAt => sampledAt;
        public DirectorMembershipDegrees Pressure => pressure;
        public DirectorMembershipDegrees Vulnerability => vulnerability;
        public DirectorMembershipDegrees QuietDuration => quietDuration;

        private void OnEnable() { nextUpdateAt = 0f; Invalidate("Waiting for observation"); }
        private void OnDisable() => Invalidate("Fuzzifier disabled");

        private void LateUpdate()
        {
            if (Time.time < nextUpdateAt) return;
            nextUpdateAt = Time.time + Mathf.Max(0.02f, updateInterval);
            RefreshMemberships();
        }

        public bool TryValidate(out string error)
        {
            error = null;
            if (tensionEstimator == null || eventMemory == null || settings == null)
                error = "Assign Tension Estimator, Event Memory and Fuzzy Settings.";
            else if (tensionEstimator.gameObject.scene != gameObject.scene ||
                eventMemory.gameObject.scene != gameObject.scene)
                error = "Fuzzifier references must belong to this scene.";
            else if (!DirectorFuzzyMembership.IsFinite(updateInterval) || updateInterval < 0.02f)
                error = "Fuzzifier interval must be finite and at least 0.02 seconds.";
            else return settings.TryValidate(out error);
            return false;
        }

        public void RefreshMemberships()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            if (!TryValidate(out var error)) { Invalidate(error); return; }
            // Refresh validates the source observation's age and avoids stale execution order.
            tensionEstimator.RefreshEstimate();
            if (!tensionEstimator.IsValid || !eventMemory.IsReady)
            { Invalidate("Waiting for valid tension and event memory"); return; }
            if (!tensionEstimator.PlayerAlive)
            { Invalidate("Player dead - fuzzy decisions blocked"); return; }

            float p = tensionEstimator.EstimatedPressure;
            float v = tensionEstimator.Vulnerability;
            float q = eventMemory.QuietSeconds;
            if (!ValidInputs(p, v, q)) { Invalidate("Invalid input values"); return; }

            pressureInput = p; vulnerabilityInput = v; quietSecondsInput = q;
            pressure = DirectorFuzzyMembership.Evaluate(p, settings.PressureKnots);
            vulnerability = DirectorFuzzyMembership.Evaluate(v, settings.VulnerabilityKnots);
            quietDuration = DirectorFuzzyMembership.Evaluate(q, settings.QuietSecondsKnots);
            sampledAt = Time.time;
            outputValid = true;
            status = "Ready - memberships only; inference rules are the next milestone";
        }

        [ContextMenu("Preview Memberships From Manual Inputs")]
        private void PreviewMemberships()
        {
            if (settings == null) { Debug.LogError("Assign Fuzzy Settings first.", this); return; }
            if (!settings.TryValidate(out var error)) { Debug.LogError(error, this); return; }
            if (!ValidInputs(previewPressure, previewVulnerability, previewQuietSeconds))
            { Debug.LogError("Preview inputs must be finite: pressure/vulnerability 0-1 and seconds >= 0.", this); return; }
            pressurePreview = DirectorFuzzyMembership.Evaluate(previewPressure, settings.PressureKnots);
            vulnerabilityPreview = DirectorFuzzyMembership.Evaluate(previewVulnerability, settings.VulnerabilityKnots);
            quietPreview = DirectorFuzzyMembership.Evaluate(previewQuietSeconds, settings.QuietSecondsKnots);
            Debug.Log("[Environment Director] Manual membership preview refreshed. Live output is unaffected.", this);
        }

        private static bool ValidInputs(float p, float v, float q) =>
            DirectorFuzzyMembership.IsFinite(p) && p >= 0f && p <= 1f &&
            DirectorFuzzyMembership.IsFinite(v) && v >= 0f && v <= 1f &&
            DirectorFuzzyMembership.IsFinite(q) && q >= 0f;

        private void Invalidate(string reason)
        {
            outputValid = false; status = reason; sampledAt = 0f;
            pressureInput = vulnerabilityInput = quietSecondsInput = 0f;
            pressure = vulnerability = quietDuration = default;
        }
    }
}
