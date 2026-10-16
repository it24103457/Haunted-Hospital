using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [DisallowMultipleComponent]
    public sealed class DirectorSpatialRegistry : MonoBehaviour
    {
        [SerializeField] private Transform roomZonesRoot;
        [SerializeField] private Transform eventAnchorsRoot;
        [SerializeField] private Transform directorLightsRoot;
        [SerializeField] private Transform essentialLightsRoot;
        private DirectorRoomZone[] rooms = Array.Empty<DirectorRoomZone>();
        private DirectorEventAnchor[] anchors = Array.Empty<DirectorEventAnchor>();

        public IReadOnlyList<DirectorRoomZone> Rooms => rooms;
        public IReadOnlyList<DirectorEventAnchor> Anchors => anchors;

        private void Awake() => Rebuild();

        [ContextMenu("Rebuild Spatial Registry")]
        public void Rebuild()
        {
            rooms = roomZonesRoot == null ? Array.Empty<DirectorRoomZone>() :
                roomZonesRoot.GetComponentsInChildren<DirectorRoomZone>(true);
            anchors = eventAnchorsRoot == null ? Array.Empty<DirectorEventAnchor>() :
                eventAnchorsRoot.GetComponentsInChildren<DirectorEventAnchor>(true);
            Array.Sort(rooms, (a, b) => StringComparer.Ordinal.Compare(a.RoomId, b.RoomId));
            Array.Sort(anchors, (a, b) => StringComparer.Ordinal.Compare(a.AnchorId, b.AnchorId));
        }

        // Highest priority wins. In equal-priority overlaps keep the current room;
        // otherwise stable ID ordering makes selection independent of hierarchy order.
        public DirectorRoomZone ResolveRoom(Vector3 position, DirectorRoomZone preferred = null)
        {
            DirectorRoomZone best = null;
            foreach (DirectorRoomZone candidate in rooms)
            {
                if (candidate == null || !candidate.Contains(position)) continue;
                if (best == null || candidate.Priority > best.Priority ||
                    (candidate.Priority == best.Priority && candidate == preferred))
                    best = candidate;
            }
            return best;
        }

        public void GetAnchors(DirectorRoomZone room, DirectorTargetType type,
            List<DirectorEventAnchor> results, bool availableOnly = true)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            foreach (DirectorEventAnchor anchor in anchors)
                if (anchor != null && anchor.Room == room && anchor.TargetType == type &&
                    (!availableOnly || anchor.IsAvailable)) results.Add(anchor);
        }

        public bool TryValidate(out List<string> errors)
        {
            Rebuild();
            errors = new List<string>();
            foreach (Transform root in new[]{roomZonesRoot, eventAnchorsRoot, directorLightsRoot, essentialLightsRoot})
                if (root == null) errors.Add("Spatial registry is missing a required root reference.");
                else if (root.gameObject.scene != gameObject.scene)
                    errors.Add($"Registry root {root.name} belongs to another scene.");
            if (directorLightsRoot != null && essentialLightsRoot != null &&
                (directorLightsRoot.IsChildOf(essentialLightsRoot) || essentialLightsRoot.IsChildOf(directorLightsRoot)))
                errors.Add("Director and Essential lighting roots must be separate.");
            if (rooms.Length == 0) errors.Add("No room zones are registered.");
            if (anchors.Length == 0) errors.Add("No event anchors are registered.");
            var roomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var anchorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var registeredRooms = new HashSet<DirectorRoomZone>(rooms);
            foreach (DirectorRoomZone room in rooms)
            {
                room.CollectValidationErrors(errors);
                if (!roomIds.Add(room.RoomId ?? "")) errors.Add($"Duplicate Room ID: {room.RoomId}.");
            }
            foreach (DirectorEventAnchor anchor in anchors)
            {
                anchor.CollectValidationErrors(directorLightsRoot, essentialLightsRoot, errors);
                if (!anchorIds.Add(anchor.AnchorId ?? "")) errors.Add($"Duplicate Anchor ID: {anchor.AnchorId}.");
                if (anchor.Room != null && !registeredRooms.Contains(anchor.Room))
                    errors.Add($"{anchor.name}: assigned room is not in this registry.");
            }
            return errors.Count == 0;
        }

        [ContextMenu("Validate Spatial Setup")]
        public void ValidateSpatialSetup()
        {
            if (!TryValidate(out var errors))
            {
                foreach (string error in errors) Debug.LogError($"[Environment Director] {error}", this);
                return;
            }
            Debug.Log($"[Environment Director] Spatial setup ready: {rooms.Length} zones, {anchors.Length} anchors.", this);
        }
    }
}
