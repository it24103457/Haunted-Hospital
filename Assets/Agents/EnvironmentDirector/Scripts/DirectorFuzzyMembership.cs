using System;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [Serializable]
    public struct DirectorMembershipDegrees
    {
        [SerializeField] private float low;
        [SerializeField] private float medium;
        [SerializeField] private float high;

        public float Low => low;
        public float Medium => medium;
        public float High => high;

        public DirectorMembershipDegrees(float low, float medium, float high)
        {
            this.low = low;
            this.medium = medium;
            this.high = high;
        }
    }

    // Pure calculations, also reusable by future predicted-state planning.
    public static class DirectorFuzzyMembership
    {
        public static bool IsFinite(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value);

        public static bool ValidKnots(Vector3 knots) =>
            IsFinite(knots.x) && IsFinite(knots.y) && IsFinite(knots.z) &&
            knots.x >= 0f && knots.x < knots.y && knots.y < knots.z;

        // X: end of full Low; Y: peak Medium; Z: start of full High.
        // The shoulders and triangle share knots, guaranteeing continuous coverage.
        public static DirectorMembershipDegrees Evaluate(float value, Vector3 knots)
        {
            if (!IsFinite(value) || !ValidKnots(knots))
                throw new ArgumentException("Finite input and ordered non-negative knots are required.");

            float low = 1f - Mathf.Clamp01((value - knots.x) / (knots.y - knots.x));
            float high = Mathf.Clamp01((value - knots.y) / (knots.z - knots.y));
            float medium = value <= knots.y
                ? Mathf.Clamp01((value - knots.x) / (knots.y - knots.x))
                : 1f - Mathf.Clamp01((value - knots.y) / (knots.z - knots.y));

            return new DirectorMembershipDegrees(low, medium, high);
        }
    }
}
