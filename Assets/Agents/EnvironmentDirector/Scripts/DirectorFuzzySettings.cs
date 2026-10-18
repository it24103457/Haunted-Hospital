using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [CreateAssetMenu(fileName = "DirectorFuzzySettings_Hospital",
        menuName = "Haunted Hospital/Environment Director/Fuzzy Settings")]
    public sealed class DirectorFuzzySettings : ScriptableObject
    {
        [Tooltip("X: full Low ends. Y: Medium peak. Z: full High begins. Domain: 0 to 1.")]
        [SerializeField] private Vector3 pressureKnots = new Vector3(0.2f, 0.5f, 0.8f);

        [Tooltip("X: full Low ends. Y: Medium peak. Z: full High begins. Domain: 0 to 1.")]
        [SerializeField] private Vector3 vulnerabilityKnots = new Vector3(0.2f, 0.5f, 0.8f);

        [Tooltip("Seconds. X: full Short ends. Y: Medium peak. Z: full Long begins; Long stays 1 beyond Z.")]
        [SerializeField] private Vector3 quietSecondsKnots = new Vector3(10f, 30f, 60f);

        public Vector3 PressureKnots => pressureKnots;
        public Vector3 VulnerabilityKnots => vulnerabilityKnots;
        public Vector3 QuietSecondsKnots => quietSecondsKnots;

        public bool TryValidate(out string error)
        {
            error = null;
            if (!DirectorFuzzyMembership.ValidKnots(pressureKnots) || pressureKnots.z > 1f)
                error = "Pressure knots must satisfy 0 <= X < Y < Z <= 1.";
            else if (!DirectorFuzzyMembership.ValidKnots(vulnerabilityKnots) || vulnerabilityKnots.z > 1f)
                error = "Vulnerability knots must satisfy 0 <= X < Y < Z <= 1.";
            else if (!DirectorFuzzyMembership.ValidKnots(quietSecondsKnots))
                error = "Quiet-time knots must be finite seconds satisfying 0 <= X < Y < Z.";
            return error == null;
        }

        [ContextMenu("Validate Fuzzy Settings")]
        private void ValidateSettings()
        {
            if (TryValidate(out var error))
                Debug.Log("[Environment Director] Fuzzy settings valid; shared knots guarantee full input coverage.", this);
            else Debug.LogError("[Environment Director] " + error, this);
        }
    }
}
