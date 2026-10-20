using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    /// <summary>Read-only bridge from the Hunter's public status to Director danger.</summary>
    [DisallowMultipleComponent]
    public sealed class DirectorLiveHunterSource : DirectorHunterSignalSource
    {
        // Scene binding for the reusable Director prefab.
        public void BindHunter(MonoBehaviour target) { hunter = target; }

        [Tooltip("Same-scene component implementing IHunterStatus, normally HunterBrain.")]
        [SerializeField] private MonoBehaviour hunter;
        [SerializeField] private string status = "Waiting for Hunter";
        private float nextLookup;

        public override bool TryRead(out DirectorSignalState danger, out float sampledAt)
        {
            danger = DirectorSignalState.Unavailable;
            sampledAt = Time.time;
            if (!Application.isPlaying || !isActiveAndEnabled) return false;
            // Also supports a Hunter instantiated after the Director starts.
            if (hunter == null && Time.time >= nextLookup)
            {
                nextLookup = Time.time + 1f;
                foreach (var candidate in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (candidate != null && candidate.gameObject.scene == gameObject.scene && candidate is IHunterStatus)
                    { hunter = candidate; break; }
            }
            if (hunter == null || hunter.gameObject.scene != gameObject.scene ||
                !(hunter is IHunterStatus source) || !source.IsReady)
            { status = "Hunter missing or not ready"; return false; }
            if (!source.IsAlive || !source.IsActive)
            { danger = DirectorSignalState.Inactive; status = "Hunter inactive or dead"; return true; }
            if (!hunter.isActiveAndEnabled)
            { status = "Active Hunter component disabled"; return false; }
            danger = source.IsChasingPlayer || source.IsAttackingPlayer
                ? DirectorSignalState.Active : DirectorSignalState.Inactive;
            status = danger == DirectorSignalState.Active ? "Hunter chasing or attacking" : "Hunter not in immediate combat";
            return true;
        }
    }
}
