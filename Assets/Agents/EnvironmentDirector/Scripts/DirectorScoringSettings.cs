using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [CreateAssetMenu(fileName = "DirectorScoringSettings_Hospital",
        menuName = "Haunted Hospital/Environment Director/Scoring Settings")]
    public sealed class DirectorScoringSettings : ScriptableObject
    {
        [SerializeField, Min(0f)] private float targetDifferenceWeight = 1f;
        [SerializeField, Min(0f)] private float overshootWeight = 1f;
        [SerializeField, Min(0f)] private float vulnerabilityOvershootMultiplier = 2f;
        [SerializeField, Min(0f)] private float eventRepetitionWeight = 0.15f;
        [SerializeField, Min(0f)] private float anchorRepetitionWeight = 0.10f;
        [SerializeField, Min(0.1f)] private float repetitionWindowSeconds = 90f;

        [SerializeField, Range(0f, .5f)] private float liveChoiceTolerance = .16f;
        public float LiveChoiceTolerance => liveChoiceTolerance;

        public float TargetDifferenceWeight => targetDifferenceWeight;
        public float OvershootWeight => overshootWeight;
        public float VulnerabilityOvershootMultiplier => vulnerabilityOvershootMultiplier;
        public float EventRepetitionWeight => eventRepetitionWeight;
        public float AnchorRepetitionWeight => anchorRepetitionWeight;
        public float RepetitionWindowSeconds => repetitionWindowSeconds;

        public bool IsValid() => NonNegative(targetDifferenceWeight) && targetDifferenceWeight > 0f &&
            NonNegative(overshootWeight) && NonNegative(vulnerabilityOvershootMultiplier) &&
            NonNegative(eventRepetitionWeight) && NonNegative(anchorRepetitionWeight) &&
            NonNegative(repetitionWindowSeconds) && repetitionWindowSeconds >= 0.1f &&
            NonNegative(liveChoiceTolerance) && liveChoiceTolerance <= .5f;

        private static bool NonNegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }
}
