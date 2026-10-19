using System;
using System.Collections.Generic;

namespace HauntedHospital.EnvironmentAI
{
    [Serializable]
    public struct DirectorPlanningAction
    {
        public DirectorEventDefinition definition;
        public DirectorEventAnchor anchor;
        public string anchorId;
        public string roomId;
        public float durationOverride; // Zero uses the event definition; audio can supply its full length.
        public bool IsRest => definition == null;
        public string Label => IsRest ? "Rest" : definition.EventId;
    }

    [Serializable]
    public struct DirectorPlanStep
    {
        public DirectorPlanningAction action;
        public DirectorPlanningState predictedState;
        public DirectorScoreBreakdown localScore;
        public float branchScore;
    }

    [Serializable]
    public struct DirectorSearchTrace
    {
        public int nodeId;
        public int depth;
        public string lastAction;
        public float priority;
    }

    [Serializable]
    public sealed class DirectorSearchPlan
    {
        public bool valid;
        public bool manualOnly;
        public bool reachedHorizon;
        public string stopReason;
        public float sampledAt;
        public float desiredTension;
        public int expandedNodes;
        public int generatedNodes;
        public int maximumDepthReached;
        public int candidateCount;
        public float finalScore;
        public List<DirectorPlanStep> steps = new List<DirectorPlanStep>();
        public List<DirectorSearchTrace> expansionTrace = new List<DirectorSearchTrace>();
    }
}
