using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [Serializable]
    public struct DirectorPlanningCooldown
    {
        public string eventId;
        public float remaining;
    }

    // A detached prediction node. Transitions create new nodes and copy the cooldown array.
    [Serializable]
    public sealed class DirectorPlanningState
    {
        [SerializeField] private float elapsedSeconds;
        [SerializeField] private float vulnerability;
        [SerializeField] private float eventPressure;
        [SerializeField] private float damageRecoveryRemaining;
        [SerializeField] private float quietSeconds;
        [SerializeField] private float globalCooldownRemaining;
        [SerializeField] private float estimatedPressure;
        [SerializeField] private float sanityFraction;
        [SerializeField] private float maxSanity;
        [SerializeField] private DirectorPlanningCooldown[] cooldowns;

        public float ElapsedSeconds => elapsedSeconds;
        public float Vulnerability => vulnerability;
        public float EventPressure => eventPressure;
        public float DamageRecoveryRemaining => damageRecoveryRemaining;
        public float QuietSeconds => quietSeconds;
        public float GlobalCooldownRemaining => globalCooldownRemaining;
        public float EstimatedPressure => estimatedPressure;
        public float SanityFraction => sanityFraction;
        public float MaxSanity => maxSanity;
        public IReadOnlyList<DirectorPlanningCooldown> Cooldowns => Array.AsReadOnly(cooldowns);

        internal DirectorPlanningState(float elapsed, float vulnerabilityValue, float pressure,
            float damageRemaining, float quiet, float globalRemaining, float total,
            DirectorPlanningCooldown[] eventCooldowns, float sanityValue = 1f, float maximumSanity = 100f)
        {
            elapsedSeconds = elapsed;
            vulnerability = vulnerabilityValue;
            eventPressure = pressure;
            damageRecoveryRemaining = damageRemaining;
            quietSeconds = quiet;
            globalCooldownRemaining = globalRemaining;
            estimatedPressure = total;
            sanityFraction = sanityValue;
            maxSanity = maximumSanity;
            cooldowns = (DirectorPlanningCooldown[])eventCooldowns.Clone();
        }

        public float CooldownRemaining(string eventId)
        {
            foreach (var item in cooldowns) if (item.eventId == eventId) return item.remaining;
            return 0f;
        }

        internal DirectorPlanningCooldown[] CopyCooldowns() => (DirectorPlanningCooldown[])cooldowns.Clone();
    }
}
