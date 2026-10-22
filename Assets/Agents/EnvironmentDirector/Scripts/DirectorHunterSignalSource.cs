using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    // Real Hunter adapters implement this contract without changing the planner.
    public abstract class DirectorHunterSignalSource : MonoBehaviour
    {
        public abstract bool TryRead(out DirectorSignalState danger, out float sampledAt);
    }
}
