using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [DisallowMultipleComponent]
    public sealed class DirectorTensionEstimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private DirectorObservation observation;

        [SerializeField]
        private DirectorEventMemory eventMemory;

        [SerializeField]
        private DirectorSettings directorSettings;

        [SerializeField]
        private DirectorTensionSettings tensionSettings;

        [Header("Sampling")]
        [SerializeField, Min(0.02f)]
        private float updateInterval = 0.1f;

        [Header("Runtime display")]
        [SerializeField]
        private bool estimateValid;

        [SerializeField]
        private bool playerAlive;

        [SerializeField]
        private string status = "Waiting for Play mode";

        [SerializeField]
        private float vulnerability;

        [SerializeField]
        private float vulnerabilityPressure;

        [SerializeField]
        private float recentDamagePressure;

        [SerializeField]
        private float recentEventPressure;

        [SerializeField]
        private float estimatedPressure;

        [SerializeField]
        private float quietSeconds;

        private float nextUpdateAt;

        public bool IsValid => isActiveAndEnabled && estimateValid;

        // Later eligibility checks must ALSO consider danger, keypad and victory.
        public bool PlayerAlive => IsValid && playerAlive;

        public float Vulnerability => vulnerability;
        public float EstimatedPressure => estimatedPressure;
        public float QuietSeconds => quietSeconds;

        private void OnEnable()
        {
            nextUpdateAt = 0f;
            Invalidate("Waiting for observation");
        }

        private void OnDisable()
        {
            Invalidate("Estimator disabled");
        }

        private void LateUpdate()
        {
            if (Time.time < nextUpdateAt)
                return;

            nextUpdateAt = Time.time + Mathf.Max(0.02f, updateInterval);
            RefreshEstimate();
        }

        public void RefreshEstimate()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
                return;

            if (observation == null ||
                eventMemory == null ||
                directorSettings == null ||
                tensionSettings == null)
            {
                Invalidate("Missing required reference");
                return;
            }

            if (!tensionSettings.IsValid() ||
                !eventMemory.IsReady ||
                !Finite(updateInterval) ||
                updateInterval < 0.02f ||
                !Finite(directorSettings.DamageRecoverySeconds) ||
                directorSettings.DamageRecoverySeconds < 0f)
            {
                Invalidate("Invalid settings or Event Memory is not ready");
                return;
            }

            DirectorObservationSnapshot snapshot = observation.Current;

            if (!snapshot.IsValid)
            {
                Invalidate("Player observation is unavailable");
                return;
            }

            float age = Time.time - snapshot.SampledAt;

            if (age < 0f || age > tensionSettings.MaximumObservationAge)
            {
                Invalidate("Player observation is stale");
                return;
            }

            estimateValid = true;
            playerAlive = snapshot.IsAlive;
            quietSeconds = eventMemory.QuietSeconds;

            if (!playerAlive)
            {
                vulnerability = 0f;
                vulnerabilityPressure = 0f;
                recentDamagePressure = 0f;
                recentEventPressure = 0f;
                estimatedPressure = 0f;

                status = "Player dead — normal Director activity blocked";
                return;
            }

            float healthVulnerability =
                1f - Mathf.Clamp01(snapshot.HealthFraction);

            float sanityVulnerability =
                1f - Mathf.Clamp01(snapshot.SanityFraction);

            float totalWeight =
                tensionSettings.HealthWeight +
                tensionSettings.SanityWeight;

            vulnerability = Mathf.Clamp01(
                (healthVulnerability * tensionSettings.HealthWeight +
                 sanityVulnerability * tensionSettings.SanityWeight)
                / totalWeight);

            vulnerabilityPressure =
                vulnerability *
                tensionSettings.VulnerabilityContribution;

            recentDamagePressure = 0f;

            float recoveryDuration =
                directorSettings.DamageRecoverySeconds;

            if (snapshot.HasReceivedDamage && recoveryDuration > 0f)
            {
                float recoveryRemaining = 1f - Mathf.Clamp01(
                    snapshot.SecondsSinceDamage / recoveryDuration);

                recentDamagePressure =
                    recoveryRemaining *
                    tensionSettings.MaximumDamageContribution;
            }

            recentEventPressure = eventMemory.EventPressure;

            estimatedPressure = Mathf.Clamp01(
                vulnerabilityPressure +
                recentDamagePressure +
                recentEventPressure);

            status =
                "Ready — estimate uses player condition, damage and event memory";
        }

        private void Invalidate(string reason)
        {
            estimateValid = false;
            playerAlive = false;

            vulnerability = 0f;
            vulnerabilityPressure = 0f;
            recentDamagePressure = 0f;
            recentEventPressure = 0f;
            estimatedPressure = 0f;
            quietSeconds = 0f;

            status = reason;
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}