using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [CreateAssetMenu(
        fileName = "DirectorSettings_Hospital",
        menuName = "Haunted Hospital/Environment Director/Director Settings")]
    public sealed class DirectorSettings : ScriptableObject
    {
        [Header("Event catalogue")]
        [SerializeField]
        private List<DirectorEventDefinition> events =
            new List<DirectorEventDefinition>();

        [Header("Pacing - seconds")]
        [SerializeField, Min(0f)]
        private float minimumTimeBetweenEvents = 24f;

        [SerializeField, Min(0f)]
        private float damageRecoverySeconds = 10f;

        [SerializeField, Min(0f)]
        private float gunfireRecoverySeconds = 6f;

        [SerializeField, Min(0.1f)]
        private float restDuration = 5f;

        [Header("Planning")]
        [SerializeField, Range(1, 5)]
        private int planningDepth = 3;

        [SerializeField, Min(1)]
        private int expansionBudget = 64;

        [Header("Debugging")]
        [SerializeField]
        private bool enableDebugLogging = true;

        public IReadOnlyList<DirectorEventDefinition> Events => events;

        public float MinimumTimeBetweenEvents => minimumTimeBetweenEvents;
        public float DamageRecoverySeconds => damageRecoverySeconds;
        public float GunfireRecoverySeconds => gunfireRecoverySeconds;
        public float RestDuration => restDuration;

        public int PlanningDepth => planningDepth;
        public int ExpansionBudget => expansionBudget;

        public bool EnableDebugLogging => enableDebugLogging;

        // This Director is intentionally limited to one active effect.
        public int MaximumConcurrentEvents => 1;

        public bool TryValidate(out List<string> errors)
        {
            errors = new List<string>();

            ValidateNonNegative(
                minimumTimeBetweenEvents,
                "Minimum Time Between Events",
                errors);

            ValidateNonNegative(
                damageRecoverySeconds,
                "Damage Recovery Seconds",
                errors);

            ValidateNonNegative(
                gunfireRecoverySeconds,
                "Gunfire Recovery Seconds",
                errors);

            if (!IsFinite(restDuration) || restDuration <= 0f)
                errors.Add("Rest Duration must be greater than zero.");

            if (planningDepth < 1 || planningDepth > 5)
                errors.Add("Planning Depth must be between 1 and 5.");

            if (expansionBudget < planningDepth)
                errors.Add("Expansion Budget must be at least Planning Depth.");

            if (events == null || events.Count == 0)
            {
                errors.Add("The event catalogue is empty.");
                return false;
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var types = new HashSet<DirectorEventType>();

            int availableCount = 0;

            for (int i = 0; i < events.Count; i++)
            {
                DirectorEventDefinition definition = events[i];

                if (definition == null)
                {
                    errors.Add($"Event catalogue element {i} is empty.");
                    continue;
                }

                definition.CollectValidationErrors(errors);

                if (!string.IsNullOrWhiteSpace(definition.EventId))
                {
                    if (!ids.Add(definition.EventId.Trim()))
                    {
                        errors.Add(
                            $"Duplicate Event ID: '{definition.EventId}'.");
                    }
                }

                if (!types.Add(definition.EventType))
                {
                    errors.Add(
                        $"Duplicate Event Type: {definition.EventType}.");
                }

                if (definition.AvailableForSelection)
                    availableCount++;
            }

            foreach (DirectorEventType type in
                     Enum.GetValues(typeof(DirectorEventType)))
            {
                if (type == DirectorEventType.Rest)
                    continue;

                if (!types.Contains(type))
                    errors.Add($"Missing event definition: {type}.");
            }

            if (availableCount == 0)
                errors.Add("At least one event must be available for selection.");

            return errors.Count == 0;
        }

        [ContextMenu("Validate Director Settings")]
        public void ValidateInEditor()
        {
            if (!TryValidate(out List<string> errors))
            {
                foreach (string error in errors)
                    Debug.LogError($"[Environment Director] {error}", this);

                return;
            }

            int availableCount = 0;

            foreach (DirectorEventDefinition definition in events)
            {
                if (definition.AvailableForSelection)
                    availableCount++;
            }

            Debug.Log(
                $"[Environment Director] Settings valid. " +
                $"{events.Count} event definitions; " +
                $"{availableCount} available for selection. " +
                $"Rest remains a built-in planner action.",
                this);
        }

        private static void ValidateNonNegative(
            float value,
            string label,
            List<string> errors)
        {
            if (!IsFinite(value) || value < 0f)
                errors.Add($"{label} must be a valid non-negative number.");
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
