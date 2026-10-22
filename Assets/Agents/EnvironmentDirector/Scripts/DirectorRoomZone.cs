using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    // Detection volumes are data only: they add no colliders or navigation obstacles.
    [DisallowMultipleComponent]
    public sealed class DirectorRoomZone : MonoBehaviour
    {
        [SerializeField] private string roomId;
        [SerializeField] private string displayName;
        [SerializeField] private int priority;
        [SerializeField] private Bounds[] localVolumes = Array.Empty<Bounds>();
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 0.9f, 0.65f);

        public string RoomId => roomId;
        public string DisplayName => displayName;
        public int Priority => priority;
        public IReadOnlyList<Bounds> LocalVolumes => localVolumes;

        public bool Contains(Vector3 worldPosition, float localMargin = 0f)
        {
            if (!isActiveAndEnabled || localVolumes == null) return false;
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            foreach (Bounds volume in localVolumes)
            {
                Bounds expanded = volume;
                expanded.Expand(Mathf.Max(0f, localMargin) * 2f);
                if (expanded.Contains(local)) return true;
            }
            return false;
        }

        public void CollectValidationErrors(List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(roomId) || roomId != roomId.Trim())
                errors.Add($"{name}: provide a non-empty Room ID without surrounding spaces.");
            if (string.IsNullOrWhiteSpace(displayName))
                errors.Add($"{name}: Display Name is missing.");
            if (localVolumes == null || localVolumes.Length == 0)
                errors.Add($"{name}: at least one detection volume is required.");
            else
                foreach (Bounds volume in localVolumes)
                    if (!Finite(volume.center) || !Finite(volume.size) ||
                        volume.size.x <= 0f || volume.size.y <= 0f || volume.size.z <= 0f)
                        errors.Add($"{name}: detection volume has invalid centre or size.");
            if (!Finite(transform.lossyScale) || transform.lossyScale.x <= 0f ||
                transform.lossyScale.y <= 0f || transform.lossyScale.z <= 0f)
                errors.Add($"{name}: zone scale must be positive and finite.");
        }

        private static bool Finite(Vector3 v) =>
            !float.IsNaN(v.x) && !float.IsInfinity(v.x) &&
            !float.IsNaN(v.y) && !float.IsInfinity(v.y) &&
            !float.IsNaN(v.z) && !float.IsInfinity(v.z);

        private void OnDrawGizmosSelected()
        {
            if (localVolumes == null) return;
            Matrix4x4 previous = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = gizmoColor;
            foreach (Bounds volume in localVolumes)
                Gizmos.DrawWireCube(volume.center, volume.size);
            Gizmos.matrix = previous;
            Gizmos.color = previousColor;
#if UNITY_EDITOR
            if (localVolumes.Length > 0)
                UnityEditor.Handles.Label(transform.TransformPoint(localVolumes[0].center), displayName);
#endif
        }
    }
}
