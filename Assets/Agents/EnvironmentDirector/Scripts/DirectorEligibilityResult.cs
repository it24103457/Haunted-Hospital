using System;

namespace HauntedHospital.EnvironmentAI
{
    [Serializable]
    public struct DirectorEligibilityResult
    {
        public DirectorEventDefinition definition;
        public DirectorEventAnchor anchor;
        public bool eligible;
        public string reason;
        public float distance;

        public bool IsRest => definition == null && anchor == null && eligible;
    }
}
