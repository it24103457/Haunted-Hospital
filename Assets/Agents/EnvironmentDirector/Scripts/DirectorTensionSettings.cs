using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [CreateAssetMenu(
        fileName = "DirectorTensionSettings_Hospital",
        menuName = "Haunted Hospital/Environment Director/Tension Settings")]
    public sealed class DirectorTensionSettings : ScriptableObject
    {
        [Header("Vulnerability weights")]
        [SerializeField, Min(0f)]
        private float healthWeight = 0.6f;

        [SerializeField, Min(0f)]
        private float sanityWeight = 0.4f;

        [Header("Pressure contributions")]
        [SerializeField, Range(0f, 1f)]
        private float vulnerabilityContribution = 0.5f;

        [SerializeField, Range(0f, 1f)]
        private float maximumDamageContribution = 0.25f;

        [Header("Event pressure")]
        [SerializeField, Range(0.01f, 1f)]
        private float maximumEventPressure = 1f;

        [SerializeField, Min(0f)]
        private float eventPressureDecayPerSecond = 0.02f;

        [Header("Memory")]
        [SerializeField, Range(1, 100)]
        private int historyCapacity = 20;

        [Header("Observation freshness")]
        [SerializeField, Min(0.1f)]
        private float maximumObservationAge = 0.5f;

        public float HealthWeight => healthWeight;
        public float SanityWeight => sanityWeight;
        public float VulnerabilityContribution => vulnerabilityContribution;
        public float MaximumDamageContribution => maximumDamageContribution;
        public float MaximumEventPressure => maximumEventPressure;
        public float EventPressureDecayPerSecond => eventPressureDecayPerSecond;
        public int HistoryCapacity => historyCapacity;
        public float MaximumObservationAge => maximumObservationAge;

        public bool IsValid()
        {
            return Finite(healthWeight) && healthWeight >= 0f
                && Finite(sanityWeight) && sanityWeight >= 0f
                && Finite(healthWeight + sanityWeight)
                && healthWeight + sanityWeight > 0f
                && UnitRange(vulnerabilityContribution)
                && UnitRange(maximumDamageContribution)
                && UnitRange(maximumEventPressure)
                && maximumEventPressure > 0f
                && Finite(eventPressureDecayPerSecond)
                && eventPressureDecayPerSecond >= 0f
                && historyCapacity >= 1
                && historyCapacity <= 100
                && Finite(maximumObservationAge)
                && maximumObservationAge >= 0.1f;
        }

        private static bool UnitRange(float value)
        {
            return Finite(value) && value >= 0f && value <= 1f;
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}