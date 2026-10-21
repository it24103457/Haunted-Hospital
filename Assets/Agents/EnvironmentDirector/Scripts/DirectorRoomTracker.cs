using System;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [DisallowMultipleComponent]
    public sealed class DirectorRoomTracker : MonoBehaviour
    {
        // Scene binding for the reusable Director prefab.
        public void BindPlayer(Transform target) { player = target; }

        [SerializeField] private Transform player;
        [SerializeField] private DirectorSpatialRegistry registry;
        [SerializeField] private DirectorSettings settings;
        [SerializeField, Min(0.02f)] private float sampleInterval = 0.1f;
        [SerializeField, Min(0f)] private float transitionConfirmationSeconds = 0.15f;
        [SerializeField, Min(0f)] private float boundaryMargin = 0.04f;
        [Header("Runtime observation - read only")]
        [SerializeField] private string currentRoomId = "outside";
        private DirectorRoomZone currentRoom;
        private DirectorRoomZone pendingRoom;
        private float pendingSince;
        private float nextSampleTime;
        private bool initialized;

        public DirectorRoomZone CurrentRoom => currentRoom;
        public Transform Player => player;
        public string CurrentRoomId => currentRoomId;
        public event Action<DirectorRoomZone, DirectorRoomZone> RoomChanged;

        private void Start()
        {
            if (player == null || registry == null)
            {
                Debug.LogError("[Environment Director] Room tracker requires Player and Registry references.", this);
                enabled = false;
                return;
            }
            if (!registry.TryValidate(out var errors))
            {
                foreach (string error in errors) Debug.LogError($"[Environment Director] {error}", this);
                enabled = false;
                return;
            }
            ObservePosition(player.position, Time.time, true);
        }

        private void OnEnable()
        {
            initialized = false;
            pendingRoom = null;
            nextSampleTime = 0f;
        }

        private void Update()
        {
            if (player == null || registry == null || Time.time < nextSampleTime) return;
            nextSampleTime = Time.time + Mathf.Max(0.02f, sampleInterval);
            ObservePosition(player.position, Time.time);
        }

        // Also allows deterministic verification without moving the real player.
        public void ObservePosition(Vector3 position, float now, bool immediate = false)
        {
            if (registry == null) return;
            DirectorRoomZone candidate = registry.ResolveRoom(position, currentRoom);
            // Retain a room across tiny unassigned seams only, never across another room.
            if (candidate == null && currentRoom != null && currentRoom.Contains(position, boundaryMargin))
                candidate = currentRoom;
            if (!initialized || immediate)
            {
                initialized = true;
                pendingRoom = candidate;
                CommitRoom(candidate);
                return;
            }
            if (candidate == currentRoom) { pendingRoom = candidate; pendingSince = now; return; }
            if (candidate != pendingRoom) { pendingRoom = candidate; pendingSince = now; }
            if (now - pendingSince >= Mathf.Max(0f, transitionConfirmationSeconds)) CommitRoom(candidate);
        }

        private void CommitRoom(DirectorRoomZone next)
        {
            DirectorRoomZone previous = currentRoom;
            currentRoom = next;
            currentRoomId = next == null ? "outside" : next.RoomId;
            if (previous == next) return;
            if (settings != null && settings.EnableDebugLogging)
                Debug.Log($"[Environment Director] Room: {(previous == null ? "Outside" : previous.DisplayName)} -> " +
                    $"{(next == null ? "Outside" : next.DisplayName)}", this);
            RoomChanged?.Invoke(previous, next);
        }
    }
}
