using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    // Pure calculations: no Time, scene queries, reservations, or live memory writes.
    public static class DirectorStatePredictor
    {
        public static bool TryCreate(float vulnerability, float eventPressure, float damageRemaining,
            float quietSeconds, float globalRemaining, DirectorPlanningCooldown[] cooldowns,
            DirectorSettings director, DirectorTensionSettings tension,
            out DirectorPlanningState state, out string reason)
        {
            state = null;
            if (!ValidSettings(director, tension, out reason)) return false;
            if (!NonNegative(vulnerability) || vulnerability > 1f || !NonNegative(eventPressure) ||
                eventPressure > tension.MaximumEventPressure || !NonNegative(damageRemaining) ||
                damageRemaining > director.DamageRecoverySeconds || !NonNegative(quietSeconds) ||
                !NonNegative(globalRemaining) || cooldowns == null)
            { reason = "Invalid planning inputs"; return false; }
            var ids = new HashSet<string>();
            foreach (var item in cooldowns)
                if (string.IsNullOrWhiteSpace(item.eventId) || !NonNegative(item.remaining) || !ids.Add(item.eventId))
                { reason = "Invalid or duplicate cooldown entry"; return false; }
            state = Make(0f, vulnerability, eventPressure, damageRemaining, quietSeconds,
                globalRemaining, cooldowns, director, tension);
            reason = "Ready";
            return true;
        }

        public static bool TryPredictRest(DirectorPlanningState source, DirectorSettings director,
            DirectorTensionSettings tension, out DirectorPlanningState next, out string reason)
        {
            next = null;
            if (!ValidSettings(director, tension, out reason)) return false;
            return Transition(source, null, director.RestDuration, director, tension, out next, out reason);
        }

        public static bool TryPredictEvent(DirectorPlanningState source, DirectorEventDefinition definition,
            DirectorSettings director, DirectorTensionSettings tension,
            out DirectorPlanningState next, out string reason, float durationOverride = 0f)
        {
            next = null;
            if (!ValidSettings(director, tension, out reason)) return false;
            if (source == null || definition == null) { reason = "Missing state or event"; return false; }
            var errors = new List<string>();
            definition.CollectValidationErrors(errors);
            if (errors.Count > 0) { reason = errors[0]; return false; }
            bool registered = false;
            foreach (var item in director.Events) if (item == definition) registered = true;
            if (!registered || !definition.AvailableForSelection)
            { reason = "Event disabled or outside catalogue"; return false; }
            if (source.GlobalCooldownRemaining > 0f || source.CooldownRemaining(definition.EventId) > 0f ||
                source.DamageRecoveryRemaining > 0f)
            { reason = "Predicted cooldown or damage recovery blocks event; predict Rest first"; return false; }
            if (!NonNegative(durationOverride)) { reason = "Invalid duration override"; return false; }
            return Transition(source, definition, durationOverride > 0f ? durationOverride : definition.Duration,
                director, tension, out next, out reason);
        }

        private static bool Transition(DirectorPlanningState source, DirectorEventDefinition definition,
            float duration, DirectorSettings director, DirectorTensionSettings tension,
            out DirectorPlanningState next, out string reason)
        {
            next = null;
            if (source == null) { reason = "Missing source state"; return false; }
            float elapsed = source.ElapsedSeconds + duration;
            float quiet = definition == null ? source.QuietSeconds + duration : 0f;
            if (!NonNegative(elapsed) || !NonNegative(quiet))
            { reason = "Prediction time overflow"; return false; }
            // Delivered effects sustain pressure while active, matching live memory. Rest decays it.
            float pressure = Mathf.Clamp(source.EventPressure +
                (definition == null ? 0f : definition.PredictedTensionIncrease), 0f, tension.MaximumEventPressure);
            if (definition == null) pressure = Mathf.Max(0f, pressure - tension.EventPressureDecayPerSecond * duration);
            var cooldowns = new List<DirectorPlanningCooldown>(source.CopyCooldowns());
            for (int i = 0; i < cooldowns.Count; i++)
                cooldowns[i] = new DirectorPlanningCooldown { eventId = cooldowns[i].eventId,
                    remaining = Mathf.Max(0f, cooldowns[i].remaining - duration) };
            if (definition != null)
            {
                int index = cooldowns.FindIndex(c => c.eventId == definition.EventId);
                var entry = new DirectorPlanningCooldown { eventId = definition.EventId, remaining = definition.Cooldown };
                if (index < 0) cooldowns.Add(entry); else cooldowns[index] = entry;
            }
            next = Make(elapsed, source.Vulnerability, pressure,
                Mathf.Max(0f, source.DamageRecoveryRemaining - duration), quiet,
                definition == null ? Mathf.Max(0f, source.GlobalCooldownRemaining - duration) : director.MinimumTimeBetweenEvents,
                cooldowns.ToArray(), director, tension);
            reason = "Predicted only; live eligibility and executor readiness still required";
            return true;
        }

        private static DirectorPlanningState Make(float elapsed, float vulnerability, float pressure,
            float damage, float quiet, float global, DirectorPlanningCooldown[] cooldowns,
            DirectorSettings director, DirectorTensionSettings tension)
        {
            float damageContribution = director.DamageRecoverySeconds > 0f
                ? damage / director.DamageRecoverySeconds * tension.MaximumDamageContribution : 0f;
            float total = Mathf.Clamp01(vulnerability * tension.VulnerabilityContribution + damageContribution + pressure);
            return new DirectorPlanningState(elapsed, vulnerability, pressure, damage, quiet, global, total, cooldowns);
        }

        private static bool ValidSettings(DirectorSettings director, DirectorTensionSettings tension, out string reason)
        {
            reason = "Missing or invalid settings";
            if (director == null || tension == null || !tension.IsValid()) return false;
            if (!director.TryValidate(out var errors)) { reason = string.Join("; ", errors); return false; }
            return true;
        }

        private static bool NonNegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }
}
