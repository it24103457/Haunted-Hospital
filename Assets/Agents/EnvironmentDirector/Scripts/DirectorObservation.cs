using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [DisallowMultipleComponent]
    public sealed class DirectorObservation : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private DirectorRoomTracker roomTracker;
        [SerializeField] private DirectorSettings settings;
        [SerializeField] private DirectorHunterSignalSource hunterSource;
        [SerializeField] private DirectorPlayerSignals playerSignals;
        [SerializeField, Min(0.02f)] private float maximumHunterSignalAge = 0.5f;
        [SerializeField, Min(0.02f)] private float observationInterval = 0.1f;
        [Header("Runtime observations (display only; overwritten on refresh)")]
        [SerializeField] private bool referencesValid;
        [SerializeField] private string validationMessage = "Waiting for Play mode";
        [SerializeField] private DirectorObservationSnapshot snapshot;

        private PlayerStats subscribedStats;
        private DirectorRoomTracker subscribedTracker;
        private bool started, receivedDamage;
        private float damagedAt, enteredAt, nextSampleAt;
        private string observedRoom;

        public DirectorObservationSnapshot Current => isActiveAndEnabled ? snapshot : default;

        private void OnEnable()
        {
            // Re-enabling starts a fresh observation session; missed events are not inferred.
            receivedDamage = false;
            observedRoom = null;
            snapshot = default;
            nextSampleAt = 0f;
            Bind();
            if (started) RefreshNow();
        }

        private void Start() { started = true; RefreshNow(); }

        private void OnDisable()
        {
            Unbind();
            snapshot = default;
            referencesValid = false;
            validationMessage = "Observation disabled";
        }

        private void Bind()
        {
            Unbind();
            subscribedStats = playerStats;
            subscribedTracker = roomTracker;
            if (subscribedStats != null)
            {
                subscribedStats.Damaged += OnDamaged;
                subscribedStats.Died += OnDied;
            }
            if (subscribedTracker != null) subscribedTracker.RoomChanged += OnRoomChanged;
        }

        private void Unbind()
        {
            if (subscribedStats != null)
            {
                subscribedStats.Damaged -= OnDamaged;
                subscribedStats.Died -= OnDied;
            }
            if (subscribedTracker != null) subscribedTracker.RoomChanged -= OnRoomChanged;
            subscribedStats = null;
            subscribedTracker = null;
        }

        private void LateUpdate()
        {
            if (Time.time < nextSampleAt) return;
            RefreshNow();
            nextSampleAt = Time.time + Mathf.Max(0.02f, observationInterval);
        }

        private void OnDamaged(DamageInfo damage)
        {
            receivedDamage = true;
            damagedAt = Time.time;
            if (started) RefreshNow();
        }

        private void OnDied() { if (started) RefreshNow(); }

        private void OnRoomChanged(DirectorRoomZone previous, DirectorRoomZone next)
        {
            observedRoom = next == null ? "outside" : next.RoomId;
            enteredAt = Time.time;
            if (started) RefreshNow();
        }

        public bool TryValidate(out string error)
        {
            error = null;
            if (playerStats == null || playerTransform == null || roomTracker == null || settings == null)
                error = "Assign Player Stats, Player Transform, Room Tracker and Settings.";
            else if (playerStats.gameObject.scene != gameObject.scene ||
                playerTransform.gameObject.scene != gameObject.scene || roomTracker.gameObject.scene != gameObject.scene)
                error = "Player and tracker references must belong to this scene.";
            else if (roomTracker.Player != playerTransform ||
                !(playerStats.transform == playerTransform || playerStats.transform.IsChildOf(playerTransform)))
                error = "Observation and room tracking must refer to the same player.";
            else if (!roomTracker.isActiveAndEnabled || !playerStats.isActiveAndEnabled || !playerTransform.gameObject.activeInHierarchy)
                error = "The player and room tracker must be active.";
            else if (!Finite(observationInterval) || observationInterval < 0.02f ||
                !Finite(settings.DamageRecoverySeconds) || settings.DamageRecoverySeconds < 0f)
                error = "Observation interval and damage recovery duration must be valid.";
            else if (!Finite(playerStats.MaxHealth) || playerStats.MaxHealth <= 0f ||
                !Finite(playerStats.MaxSanity) || playerStats.MaxSanity <= 0f ||
                !Finite(playerStats.Health) || !Finite(playerStats.Sanity))
                error = "Player health and sanity must be finite, with positive maximum values.";
            return error == null;
        }

        // Read-only with respect to gameplay; useful for deterministic integration checks.
        public void RefreshNow()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            if (subscribedStats != playerStats || subscribedTracker != roomTracker)
            {
                receivedDamage = false;
                observedRoom = null;
                Bind();
            }
            referencesValid = TryValidate(out var error);
            if (!referencesValid)
            {
                snapshot = default;
                if (validationMessage != error) Debug.LogError("[Environment Director] " + error, this);
                validationMessage = error;
                return;
            }
            validationMessage = "Ready; optional gameplay signals are unavailable until connected";
            string room = roomTracker.CurrentRoomId;
            if (observedRoom != room) { observedRoom = room; enteredAt = Time.time; }
            snapshot = new DirectorObservationSnapshot(playerStats, playerTransform.position,
                room, Time.time, enteredAt, receivedDamage, damagedAt, settings.DamageRecoverySeconds);
            var hunter = DirectorSignalState.Unavailable;
            if (hunterSource != null && hunterSource.isActiveAndEnabled &&
                hunterSource.gameObject.scene == gameObject.scene &&
                hunterSource.TryRead(out var state, out var timestamp) &&
                Finite(timestamp) && Finite(maximumHunterSignalAge) && maximumHunterSignalAge > 0f &&
                timestamp <= Time.time && Time.time - timestamp <= maximumHunterSignalAge &&
                (state == DirectorSignalState.Active || state == DirectorSignalState.Inactive))
                hunter = state;
            snapshot = snapshot.WithHunterDanger(hunter);
            if (playerSignals != null && playerSignals.gameObject.scene == gameObject.scene)
            {
                playerSignals.Read(playerTransform, out var gunfire, out var keypad, out var ending);
                snapshot = snapshot.WithPlayerSignals(gunfire, keypad, ending);
            }
            validationMessage = "Ready; Hunter: " + hunter +
                (hunterSource is DirectorManualHunterSource ? " (manual simulation)" : "") +
                "; gunfire: " + snapshot.RecentGunfire + "; keypad: " + snapshot.KeypadInteraction +
                "; ending/pause: " + snapshot.Victory;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
