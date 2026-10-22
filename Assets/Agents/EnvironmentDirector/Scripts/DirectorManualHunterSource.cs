using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [DisallowMultipleComponent]
    public sealed class DirectorManualHunterSource : DirectorHunterSignalSource
    {
        [Tooltip("Explicit opt-in. Simulated information only; replace with a real Hunter adapter for release.")]
        [SerializeField] private bool enableSimulation;
        [SerializeField] private bool signalAvailable = true;
        [SerializeField] private bool dangerActive;
        [Tooltip("Stop refreshing the timestamp to check stale-signal handling.")]
        [SerializeField] private bool freezeTimestamp;
        private float lastSample = -1f;

        private void OnEnable() => lastSample = -1f;
        private void OnDisable() => lastSample = -1f;

        public override bool TryRead(out DirectorSignalState danger, out float sampledAt)
        {
            danger = DirectorSignalState.Unavailable;
            sampledAt = lastSample;
            if (!Application.isPlaying || !isActiveAndEnabled || !enableSimulation || !signalAvailable)
                return false;
            if (!freezeTimestamp) lastSample = Time.time;
            sampledAt = lastSample;
            danger = dangerActive ? DirectorSignalState.Active : DirectorSignalState.Inactive;
            return lastSample >= 0f;
        }
    }
}
