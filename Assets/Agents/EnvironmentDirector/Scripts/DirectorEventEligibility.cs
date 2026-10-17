using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [DisallowMultipleComponent]
    public sealed class DirectorEventEligibility : MonoBehaviour
    {
        [SerializeField] private DirectorSettings settings;
        [SerializeField] private DirectorObservation observation;
        [SerializeField] private DirectorRoomTracker roomTracker;
        [SerializeField] private DirectorSpatialRegistry registry;
        [SerializeField] private DirectorEventMemory eventMemory;
        [SerializeField] private DirectorTargetReservations reservations;
        [SerializeField] private Transform directorLightsRoot;
        [SerializeField] private Transform essentialLightsRoot;
        [SerializeField] private Camera playerCamera;
        [SerializeField, Min(0.01f)] private float maximumObservationAge = 0.5f;
        [SerializeField] private LayerMask visibilityMask = Physics.DefaultRaycastLayers;
        [SerializeField, Min(0f)] private float apparitionEyeHeight = 1f;
        [Header("Preview only - no events are executed")]
        [SerializeField] private int eligibleEventCount;
        [SerializeField] private List<DirectorEligibilityResult> preview = new List<DirectorEligibilityResult>();
        private readonly List<string> errors = new List<string>();

        // Rest means wait/do nothing. It never reserves a target or changes pressure.
        public void CollectCandidates(List<DirectorEligibilityResult> results, bool includeRejected = false)
        {
            results.Clear();
            results.Add(new DirectorEligibilityResult { eligible = true, reason = "Rest: no effect" });
            if (settings == null || registry == null) return;
            foreach (var definition in settings.Events)
            {
                if (definition == null) continue;
                bool found = false;
                foreach (var anchor in registry.Anchors)
                {
                    if (anchor == null || !anchor.SupportsEvent(definition)) continue;
                    found = true;
                    var result = Evaluate(definition, anchor);
                    if (includeRejected || result.eligible) results.Add(result);
                }
                if (!found && includeRejected) results.Add(new DirectorEligibilityResult
                    { definition = definition, reason = "No compatible anchor" });
            }
        }

        // Call again immediately before starting an effect. A previous preview is not permission.
        public DirectorEligibilityResult Evaluate(DirectorEventDefinition definition,
            DirectorEventAnchor anchor, string reservationToken = null)
        {
            var result = new DirectorEligibilityResult { definition = definition, anchor = anchor };
            result.reason = RejectionReason(definition, anchor, reservationToken, out result.distance);
            result.eligible = result.reason == null;
            if (result.eligible) result.reason = "Eligible (executor/assets readiness still required)";
            return result;
        }

        private string RejectionReason(DirectorEventDefinition definition, DirectorEventAnchor anchor,
            string reservationToken, out float distance)
        {
            distance = 0f;
            if (!Application.isPlaying || !isActiveAndEnabled) return "Requires active component in Play mode";
            if (settings == null || observation == null || roomTracker == null || registry == null ||
                eventMemory == null || reservations == null || !registry.isActiveAndEnabled ||
                !roomTracker.isActiveAndEnabled || !reservations.isActiveAndEnabled)
                return "Missing or disabled dependency";
            if (maximumObservationAge <= 0f || float.IsNaN(maximumObservationAge) ||
                float.IsInfinity(maximumObservationAge)) return "Invalid observation age setting";
            if (definition == null || anchor == null) return "Missing event or anchor";
            bool registered = false;
            foreach (var item in settings.Events) if (item == definition) registered = true;
            if (!registered) return "Event is outside the catalogue";
            registered = false;
            foreach (var item in registry.Anchors) if (item == anchor) registered = true;
            if (!registered) return "Anchor is outside the registry";
            errors.Clear();
            definition.CollectValidationErrors(errors);
            anchor.CollectValidationErrors(directorLightsRoot, essentialLightsRoot, errors);
            if (errors.Count > 0) return errors[0];
            if (!definition.AvailableForSelection) return "Event disabled";
            if (!anchor.IsAvailable || !anchor.SupportsEvent(definition)) return "Anchor unavailable or incompatible";
            var state = observation.Current;
            if (!state.IsValid || Time.time - state.SampledAt > maximumObservationAge ||
                state.SampledAt > Time.time) return "Observation unavailable or stale";
            if (!state.IsAlive) return "Player dead";
            if (roomTracker.CurrentRoom == null || roomTracker.CurrentRoomId != state.RoomId)
                return "Player room unavailable or changing";
            distance = Vector3.Distance(state.PlayerPosition, anchor.Position);
            if (float.IsNaN(distance) || float.IsInfinity(distance) ||
                distance < definition.MinimumPlayerDistance || distance > definition.MaximumPlayerDistance)
                return "Outside event distance range";
            if (definition.RequiredTarget == DirectorTargetType.DirectorLightGroup &&
                anchor.Room != roomTracker.CurrentRoom) return "Lighting must target the player's current room";
            if (!eventMemory.IsReady) return "Event memory unavailable";
            if (eventMemory.EventActive) return "Another event is active";
            if (!eventMemory.GlobalIntervalReady) return "Global event spacing";
            if (eventMemory.CooldownRemaining(definition.EventId) > 0f) return "Event type cooldown";
            if (reservations.IsReserved && !reservations.Owns(anchor, reservationToken))
                return "A target is already reserved";
            if (state.DamageRecoveryActive) return "Damage recovery";
            if (state.Victory != DirectorSignalState.Inactive) return "Victory state active or unavailable";
            if (definition.BlockDuringKeypadEntry && state.KeypadInteraction != DirectorSignalState.Inactive)
                return "Keypad state active or unavailable";
            if (definition.BlockDuringImmediateDanger && state.HunterDanger != DirectorSignalState.Inactive)
                return "Hunter danger active or unavailable";
            if (state.RecentGunfire != DirectorSignalState.Inactive) return "Gunfire recovery active or unavailable";
            if (definition.RequiresVisibility && !IsVisible(anchor)) return "Target not visible or player camera unavailable";
            return null;
        }

        private bool IsVisible(DirectorEventAnchor anchor)
        {
            if (playerCamera == null || !playerCamera.isActiveAndEnabled || roomTracker.Player == null ||
                !playerCamera.transform.IsChildOf(roomTracker.Player)) return false;
            Vector3 target = anchor.Position;
            if (anchor.TargetType == DirectorTargetType.ApparitionAnchor) target += Vector3.up * apparitionEyeHeight;
            Vector3 viewport = playerCamera.WorldToViewportPoint(target);
            if (viewport.z < playerCamera.nearClipPlane || viewport.z > playerCamera.farClipPlane ||
                viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) return false;
            Vector3 origin = playerCamera.transform.position;
            Vector3 delta = target - origin;
            foreach (var hit in Physics.RaycastAll(origin, delta.normalized, delta.magnitude,
                         visibilityMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(roomTracker.Player)) continue;
                if (anchor.SourceObject != null && hit.transform.IsChildOf(anchor.SourceObject)) continue;
                if (hit.distance < delta.magnitude - 0.03f) return false;
            }
            return true;
        }

        public bool TryReserveCandidate(DirectorEventDefinition definition, DirectorEventAnchor anchor,
            out string token)
        {
            token = null;
            return Evaluate(definition, anchor).eligible && reservations.TryReserve(anchor, 2f, out token);
        }

        [ContextMenu("Preview Eligibility (Play mode, no effects)")]
        private void PreviewEligibility()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play mode first.", this); return; }
            CollectCandidates(preview, true);
            eligibleEventCount = 0;
            foreach (var result in preview) if (result.eligible && !result.IsRest) eligibleEventCount++;
            Debug.Log($"Director eligibility: {eligibleEventCount} event/anchor pairs, plus Rest. See Preview for reasons.", this);
        }
    }
}
