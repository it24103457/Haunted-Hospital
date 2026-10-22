using System;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    // One pending target at a time, matching the Director's single-event policy.
    // Tokens prevent an old executor from releasing a newer executor's reservation.
    [DisallowMultipleComponent]
    public sealed class DirectorTargetReservations : MonoBehaviour
    {
        [SerializeField] private DirectorEventAnchor reservedAnchor;
        [SerializeField] private float expiresAt;
        private string token;

        public bool IsReserved
        {
            get
            {
                if (reservedAnchor == null || !reservedAnchor.IsAvailable || Time.time >= expiresAt)
                    Clear();
                return token != null;
            }
        }

        public bool Owns(DirectorEventAnchor anchor, string ownerToken) =>
            IsReserved && anchor == reservedAnchor && !string.IsNullOrEmpty(ownerToken) && token == ownerToken;

        public bool TryReserve(DirectorEventAnchor anchor, float seconds, out string ownerToken)
        {
            ownerToken = null;
            if (!Application.isPlaying || !isActiveAndEnabled || anchor == null ||
                !anchor.IsAvailable || float.IsNaN(seconds) || float.IsInfinity(seconds) ||
                seconds <= 0f || IsReserved) return false;
            reservedAnchor = anchor;
            expiresAt = Time.time + seconds;
            token = Guid.NewGuid().ToString("N");
            ownerToken = token;
            return true;
        }

        public bool Renew(string ownerToken, float seconds)
        {
            if (!IsReserved || token != ownerToken || seconds <= 0f ||
                float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
            expiresAt = Time.time + seconds;
            return true;
        }

        public bool Release(string ownerToken)
        {
            if (!IsReserved || token != ownerToken) return false;
            Clear();
            return true;
        }

        private void Awake() => Clear();
        private void OnDisable() => Clear();
        private void Clear() { reservedAnchor = null; token = null; expiresAt = 0f; }
    }
}
