using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [CreateAssetMenu(
        fileName = "ED_NewEvent",
        menuName = "Haunted Hospital/Environment Director/Event Definition")]
    public sealed class DirectorEventDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string eventId;
        [SerializeField] private DirectorEventType eventType;
        [SerializeField] private bool availableForSelection = true;

        [Header("Timing - seconds")]
        [SerializeField, Min(0.1f)] private float duration = 3f;
        [SerializeField, Min(0f)] private float cooldown = 45f;

        [Header("Predicted effect")]
        [SerializeField] private DirectorEventIntensity intensity;
        [SerializeField, Range(0f, 1f)]
        private float predictedTensionIncrease = 0.1f;
        [Tooltip("Applied once when delivered. Apparitions apply this only on the first visible sighting.")]
        [SerializeField, Min(0f)] private float sanityCost;

        [Header("Target")]
        [SerializeField] private DirectorTargetType requiredTarget;
        [SerializeField, Min(0f)] private float minimumPlayerDistance = 2f;
        [SerializeField, Min(0f)] private float maximumPlayerDistance = 12f;
        [Tooltip("Camera-facing requirement for non-apparition events. Apparitions always use same-room, unobstructed 360-degree sight in DirectorEventEligibility.")]
        [SerializeField] private bool requiresVisibility;

        [Header("Selection restrictions")]
        [SerializeField] private bool blockDuringImmediateDanger = true;
        [SerializeField] private bool blockDuringKeypadEntry = true;

        public string EventId => eventId;
        public DirectorEventType EventType => eventType;
        public bool AvailableForSelection => availableForSelection;

        public float Duration => duration;
        public float Cooldown => cooldown;

        public DirectorEventIntensity Intensity => intensity;
        public float PredictedTensionIncrease => predictedTensionIncrease;
        public float SanityCost => sanityCost;
        public bool IsApparition => eventType == DirectorEventType.DistantApparition ||
            eventType == DirectorEventType.LightPulseApparition;
        public bool CanStartDuringCombat => eventType == DirectorEventType.LightFlicker ||
            eventType == DirectorEventType.LocalBlackout;

        public DirectorTargetType RequiredTarget => requiredTarget;
        public float MinimumPlayerDistance => minimumPlayerDistance;
        public float MaximumPlayerDistance => maximumPlayerDistance;
        public bool RequiresVisibility => requiresVisibility;

        public bool BlockDuringImmediateDanger => !CanStartDuringCombat && blockDuringImmediateDanger;
        public bool BlockDuringKeypadEntry => blockDuringKeypadEntry;

        public void CollectValidationErrors(List<string> errors)
        {
            string prefix = $"Event asset '{name}'";

            if (string.IsNullOrWhiteSpace(eventId))
                errors.Add($"{prefix}: Event ID is missing.");
            else if (eventId != eventId.Trim())
                errors.Add($"{prefix}: Event ID has leading or trailing spaces.");

            if (!Enum.IsDefined(typeof(DirectorEventType), eventType))
                errors.Add($"{prefix}: Event Type is invalid.");

            if (eventType == DirectorEventType.Rest)
                errors.Add(
                    $"{prefix}: Rest is a built-in planner action. " +
                    "Do not create an event asset for it.");

            if (!Enum.IsDefined(typeof(DirectorEventIntensity), intensity))
                errors.Add($"{prefix}: Intensity is invalid.");

            if (!Enum.IsDefined(typeof(DirectorTargetType), requiredTarget))
                errors.Add($"{prefix}: Required Target is invalid.");

            if (!IsFinite(duration) || duration <= 0f)
                errors.Add($"{prefix}: Duration must be greater than zero.");

            if (!IsFinite(cooldown) || cooldown < 0f)
                errors.Add($"{prefix}: Cooldown cannot be negative.");
            if (!IsFinite(sanityCost) || sanityCost < 0f)
                errors.Add($"{prefix}: Sanity cost must be finite and non-negative.");

            if (!IsFinite(predictedTensionIncrease) ||
                predictedTensionIncrease < 0f ||
                predictedTensionIncrease > 1f)
            {
                errors.Add(
                    $"{prefix}: Predicted Tension Increase must be between 0 and 1.");
            }

            if (!IsFinite(minimumPlayerDistance) ||
                !IsFinite(maximumPlayerDistance) ||
                minimumPlayerDistance < 0f ||
                maximumPlayerDistance <= minimumPlayerDistance)
            {
                errors.Add(
                    $"{prefix}: Maximum Player Distance must be greater " +
                    "than Minimum Player Distance, and both must be valid.");
            }

            if (requiredTarget != ExpectedTarget(eventType))
            {
                errors.Add(
                    $"{prefix}: {eventType} requires target type " +
                    $"{ExpectedTarget(eventType)}.");
            }

            // Apparitions use mandatory same-room and unobstructed sight checks in
            // DirectorEventEligibility, independent of camera-facing visibility.
            // Do not reject their catalogue entries when requiresVisibility is false.

            if (intensity == DirectorEventIntensity.Strong && !CanStartDuringCombat &&
                !blockDuringImmediateDanger)
            {
                errors.Add(
                    $"{prefix}: Strong events must be blocked during immediate danger.");
            }

            if (!blockDuringKeypadEntry)
                errors.Add($"{prefix}: Events must be blocked during keypad entry.");
        }

        private static DirectorTargetType ExpectedTarget(DirectorEventType type)
        {
            switch (type)
            {
                case DirectorEventType.LightFlicker:
                case DirectorEventType.LocalBlackout:
                    return DirectorTargetType.DirectorLightGroup;

                case DirectorEventType.PhoneRing:
                    return DirectorTargetType.Phone;

                case DirectorEventType.Whisper:
                case DirectorEventType.Knock:
                    return DirectorTargetType.AudioAnchor;

                case DirectorEventType.HospitalAnnouncement:
                    return DirectorTargetType.Loudspeaker;

                case DirectorEventType.DistantApparition:
                case DirectorEventType.LightPulseApparition:
                    return DirectorTargetType.ApparitionAnchor;

                default:
                    return DirectorTargetType.AudioAnchor;
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
