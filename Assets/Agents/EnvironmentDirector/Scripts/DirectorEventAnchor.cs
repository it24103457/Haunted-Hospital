using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    public enum DirectorAudioAnchorKind { Whisper = 0, Knock = 1 }
    public enum DirectorApparitionAnchorKind { Both = 0, NormalOnly = 1, LightPulseOnly = 2 }

    [DisallowMultipleComponent]
    public sealed class DirectorEventAnchor : MonoBehaviour
    {
        [SerializeField] private string anchorId;
        [SerializeField] private DirectorTargetType targetType;
        [SerializeField] private DirectorRoomZone room;
        [SerializeField] private bool availableForSelection = true;
        [Tooltip("Existing phone or loudspeaker. Scene binding; not stored in a standalone prefab.")]
        [SerializeField] private Transform sourceObject;
        [Tooltip("Used only by AudioAnchor targets. Keeps knocks at authored wall/doorway positions.")]
        [SerializeField] private DirectorAudioAnchorKind audioKind;
        [SerializeField] private DirectorApparitionAnchorKind apparitionKind;
        [Tooltip("Explicit Director lights. Also used by the light-pulse apparition variant.")]
        [SerializeField] private Light[] directorLights = Array.Empty<Light>();

        public string AnchorId => anchorId;
        public DirectorTargetType TargetType => targetType;
        public DirectorRoomZone Room => room;
        public bool IsAvailable => isActiveAndEnabled && availableForSelection &&
            room != null && room.isActiveAndEnabled &&
            ((targetType != DirectorTargetType.Phone && targetType != DirectorTargetType.Loudspeaker) ||
             (sourceObject != null && sourceObject.gameObject.activeInHierarchy));
        public Transform SourceObject => sourceObject;
        public Vector3 Position => sourceObject != null ? sourceObject.position : transform.position;
        public Vector3 Forward => sourceObject != null ? sourceObject.forward : transform.forward;
        public IReadOnlyList<Light> DirectorLights => directorLights;

        // This is a target compatibility check, not the later runtime eligibility policy.
        public bool SupportsEvent(DirectorEventDefinition definition)
        {
            if (definition == null || definition.RequiredTarget != targetType) return false;
            if (targetType == DirectorTargetType.ApparitionAnchor)
            {
                if (definition.EventType == DirectorEventType.DistantApparition)
                    return apparitionKind != DirectorApparitionAnchorKind.LightPulseOnly;
                if (definition.EventType == DirectorEventType.LightPulseApparition)
                    return apparitionKind != DirectorApparitionAnchorKind.NormalOnly && directorLights != null && directorLights.Length > 0;
                return false;
            }
            if (targetType == DirectorTargetType.AudioAnchor)
                return audioKind == DirectorAudioAnchorKind.Whisper
                    ? definition.EventType == DirectorEventType.Whisper
                    : definition.EventType == DirectorEventType.Knock;
            if (definition.EventType == DirectorEventType.LightPulseApparition)
                return directorLights != null && directorLights.Length > 0;
            return true;
        }

        public void CollectValidationErrors(Transform directorRoot, Transform essentialRoot, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(anchorId) || anchorId != anchorId.Trim())
                errors.Add($"{name}: provide a non-empty Anchor ID without surrounding spaces.");
            if (!Enum.IsDefined(typeof(DirectorTargetType), targetType))
                errors.Add($"{name}: invalid target type.");
            if (room == null) errors.Add($"{name}: Room is missing.");
            if (room != null && room.gameObject.scene != gameObject.scene)
                errors.Add($"{name}: Room belongs to another scene.");
            if (targetType == DirectorTargetType.Phone || targetType == DirectorTargetType.Loudspeaker)
            {
                if (sourceObject == null) errors.Add($"{name}: phone/speaker Source Object is missing.");
                else if (sourceObject.gameObject.scene != gameObject.scene)
                    errors.Add($"{name}: Source Object belongs to another scene.");
            }
            if (targetType == DirectorTargetType.AudioAnchor &&
                !Enum.IsDefined(typeof(DirectorAudioAnchorKind), audioKind))
                errors.Add($"{name}: invalid audio anchor kind.");
            if (targetType == DirectorTargetType.DirectorLightGroup &&
                (directorLights == null || directorLights.Length == 0))
                errors.Add($"{name}: light-group target has no Director lights.");
            var seen = new HashSet<Light>();
            if (directorLights == null) return;
            foreach (Light light in directorLights)
            {
                if (light == null) { errors.Add($"{name}: missing light reference."); continue; }
                if (!seen.Add(light)) errors.Add($"{name}: duplicate light {light.name}.");
                if (light.gameObject.scene != gameObject.scene)
                    errors.Add($"{name}: {light.name} belongs to another scene.");
                if (directorRoot == null || !light.transform.IsChildOf(directorRoot))
                    errors.Add($"{name}: {light.name} is outside the Director lighting group.");
                if (essentialRoot != null && light.transform.IsChildOf(essentialRoot))
                    errors.Add($"{name}: Essential light {light.name} must never be an event target.");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Color old = Gizmos.color;
            Gizmos.color = availableForSelection ? Color.yellow : Color.gray;
            Gizmos.DrawWireSphere(Position, 0.15f);
            Gizmos.DrawLine(Position, Position + Forward * 0.65f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(Position + Vector3.up * 0.2f, anchorId);
#endif
            Gizmos.color = old;
        }
    }
}
