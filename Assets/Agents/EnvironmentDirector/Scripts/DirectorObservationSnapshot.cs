using System;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    public enum DirectorSignalState { Unavailable, Inactive, Active }

    // A value copy: consumers cannot change the observer or PlayerStats through it.
    [Serializable]
    public struct DirectorObservationSnapshot
    {
        [SerializeField] private bool valid;
        [SerializeField] private float sampledAt;
        [SerializeField] private float health, maxHealth, healthFraction;
        [SerializeField] private float sanity, maxSanity, sanityFraction;
        [SerializeField] private int reserveAmmo, maxReserveAmmo;
        [SerializeField] private bool alive, hasExitKey;
        [SerializeField] private Vector3 playerPosition;
        [SerializeField] private string roomId;
        [SerializeField] private float timeInRoom;
        [SerializeField] private bool hasReceivedDamage;
        [SerializeField] private float secondsSinceDamage;
        [SerializeField] private bool damageRecoveryActive;
        [SerializeField] private DirectorSignalState hunterDanger, recentGunfire, keypadInteraction, victory;

        public bool IsValid => valid;
        public float SampledAt => sampledAt;
        public float Health => health;
        public float MaxHealth => maxHealth;
        public float HealthFraction => healthFraction;
        public float Sanity => sanity;
        public float MaxSanity => maxSanity;
        public float SanityFraction => sanityFraction;
        public int ReserveAmmo => reserveAmmo;
        public int MaxReserveAmmo => maxReserveAmmo;
        public bool IsAlive => alive;
        public bool HasExitKey => hasExitKey;
        public Vector3 PlayerPosition => playerPosition;
        public string RoomId => roomId;
        public float TimeInRoom => timeInRoom;
        public bool HasReceivedDamage => hasReceivedDamage;
        // -1 means no damage was observed during the current observation session.
        public float SecondsSinceDamage => secondsSinceDamage;
        public bool DamageRecoveryActive => damageRecoveryActive;
        public DirectorSignalState HunterDanger => hunterDanger;
        public DirectorSignalState RecentGunfire => recentGunfire;
        public DirectorSignalState KeypadInteraction => keypadInteraction;
        public DirectorSignalState Victory => victory;

        internal DirectorObservationSnapshot WithHunterDanger(DirectorSignalState state)
        {
            var copy = this;
            copy.hunterDanger = state;
            return copy;
        }

        internal DirectorObservationSnapshot(PlayerStats stats, Vector3 position, string room,
            float now, float enteredAt, bool damaged, float damagedAt, float recoverySeconds)
        {
            valid = true;
            sampledAt = now;
            health = stats.Health; maxHealth = stats.MaxHealth;
            sanity = stats.Sanity; maxSanity = stats.MaxSanity;
            healthFraction = Mathf.Clamp01(health / maxHealth);
            sanityFraction = Mathf.Clamp01(sanity / maxSanity);
            reserveAmmo = stats.ReserveAmmo; maxReserveAmmo = stats.MaxReserveAmmo;
            alive = !stats.IsDead; hasExitKey = stats.HasExitKey;
            playerPosition = position; roomId = room;
            timeInRoom = Mathf.Max(0f, now - enteredAt);
            hasReceivedDamage = damaged;
            secondsSinceDamage = damaged ? Mathf.Max(0f, now - damagedAt) : -1f;
            damageRecoveryActive = damaged && secondsSinceDamage < recoverySeconds;
            hunterDanger = recentGunfire = keypadInteraction = victory = DirectorSignalState.Unavailable;
        }
    }
}
