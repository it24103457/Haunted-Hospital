using System;
using System.Collections.Generic;

namespace HauntedHospital.EnvironmentAI
{
    // Bounded greedy best-first tree search. Frontier priority is h, not A* g+h.
    // Branches retain their own history; no scene, physics, timers or effects are mutated.
    public static class DirectorGreedyPlanner
    {
        public const int MaximumGeneratedNodes = 8192;
        public const int MaximumCandidates = 256;
        private const int MaximumTraceRows = 256;

        private sealed class Node
        {
            public int id, depth;
            public Node parent;
            public DirectorPlanStep step;
            public DirectorPlanningState state;
            public List<DirectorEventRecord> history;
            public float eventPenalty, anchorPenalty, priority;
        }

        public static DirectorSearchPlan Search(DirectorPlanningState source,
            IReadOnlyList<DirectorPlanningAction> suppliedActions, IReadOnlyList<DirectorEventRecord> history,
            float desiredTension, float sampledAt, bool manualOnly, DirectorSettings director,
            DirectorTensionSettings tension, DirectorScoringSettings scoring)
        {
            var result = new DirectorSearchPlan { manualOnly = manualOnly, sampledAt = sampledAt,
                desiredTension = desiredTension, stopReason = "Invalid search inputs" };
            if (source == null || suppliedActions == null || history == null || director == null ||
                tension == null || !tension.IsValid() || scoring == null || !scoring.IsValid() ||
                !Finite(desiredTension) || desiredTension < 0f || desiredTension > 1f || !Finite(sampledAt)) return result;
            if (!director.TryValidate(out var errors)) { result.stopReason = string.Join("; ", errors); return result; }
            if (director.PlanningDepth < 1 || director.PlanningDepth > 5 || director.ExpansionBudget > 4096)
            { result.stopReason = "Use depth 1-5 and expansion budget no greater than 4096"; return result; }

            // Always include one Rest action, then deduplicate event/anchor pairs in input order.
            var actions = new List<DirectorPlanningAction> { new DirectorPlanningAction() };
            var keys = new HashSet<string>();
            foreach (var action in suppliedActions)
            {
                if (action.IsRest) continue;
                if (string.IsNullOrWhiteSpace(action.anchorId) || string.IsNullOrWhiteSpace(action.roomId))
                { result.stopReason = "Event action requires anchor and room IDs"; return result; }
                if (!manualOnly && (action.anchor == null || action.anchor.AnchorId != action.anchorId ||
                    action.anchor.Room == null || action.anchor.Room.RoomId != action.roomId))
                { result.stopReason = "Live action has mismatched anchor references"; return result; }
                string key = action.definition.EventId + "\n" + action.anchorId;
                if (keys.Add(key)) actions.Add(action);
            }
            result.candidateCount = actions.Count;
            if (actions.Count > MaximumCandidates)
            { result.stopReason = "Candidate count exceeds bounded search capacity"; return result; }
            var root = new Node { id = 0, state = source, history = new List<DirectorEventRecord>(history) };
            var frontier = new List<Node> { root };
            Node bestLeaf = null, bestPartial = null;
            result.generatedNodes = 1;
            bool generationLimit = false;
            while (frontier.Count > 0 && result.expandedNodes < director.ExpansionBudget && !generationLimit)
            {
                int bestIndex = 0;
                for (int i = 1; i < frontier.Count; i++)
                    if (Better(frontier[i], frontier[bestIndex])) bestIndex = i;
                Node current = frontier[bestIndex];
                frontier.RemoveAt(bestIndex);
                result.expandedNodes++;
                if (result.expansionTrace.Count < MaximumTraceRows)
                    result.expansionTrace.Add(new DirectorSearchTrace { nodeId = current.id, depth = current.depth,
                        lastAction = current.parent == null ? "Root" : current.step.action.Label, priority = current.priority });
                foreach (var action in actions)
                {
                    if (result.generatedNodes >= MaximumGeneratedNodes) { generationLimit = true; break; }
                    DirectorPlanningState next;
                    string reason;
                    bool predicted = action.IsRest
                        ? DirectorStatePredictor.TryPredictRest(current.state, director, tension, out next, out reason)
                        : DirectorStatePredictor.TryPredictEvent(current.state, action.definition, director, tension, out next, out reason);
                    if (!predicted) continue;
                    float start = sampledAt + current.state.ElapsedSeconds;
                    float end = sampledAt + next.ElapsedSeconds;
                    if (!Finite(start) || !Finite(end)) continue;
                    var repetition = action.IsRest ? default : DirectorHeuristic.GetRepetition(current.history,
                        action.definition.EventType, action.anchorId, start, scoring.RepetitionWindowSeconds);
                    var score = DirectorHeuristic.Score(next.EstimatedPressure, desiredTension,
                        next.Vulnerability, action.IsRest, repetition, scoring);
                    if (!score.valid) continue;
                    var child = new Node { id = result.generatedNodes++, depth = current.depth + 1,
                        parent = current, state = next, history = new List<DirectorEventRecord>(current.history),
                        eventPenalty = current.eventPenalty + score.eventRepetitionPenalty,
                        anchorPenalty = current.anchorPenalty + score.anchorRepetitionPenalty };
                    // Retain repetition penalties across Rest: waiting must not erase the
                    // earlier cost of selecting a repetitive scare in this branch.
                    child.priority = score.targetDifference + score.overshootPenalty + child.eventPenalty + child.anchorPenalty;
                    if (!Finite(child.priority)) continue;
                    child.step = new DirectorPlanStep { action = action, predictedState = next,
                        localScore = score, branchScore = child.priority };
                    if (!action.IsRest)
                        child.history.Add(new DirectorEventRecord { eventId = action.definition.EventId,
                            eventType = action.definition.EventType, anchorId = action.anchorId, roomId = action.roomId,
                            startedAt = start, endedAt = end, outcome = DirectorEventOutcome.Completed,
                            effectDelivered = true, debugRecord = false });
                    result.maximumDepthReached = Math.Max(result.maximumDepthReached, child.depth);
                    if (Better(child, bestPartial)) bestPartial = child;
                    if (child.depth == director.PlanningDepth)
                    {
                        if (Better(child, bestLeaf)) bestLeaf = child;
                    }
                    else frontier.Add(child);
                }
            }
            Node chosen = bestLeaf ?? bestPartial;
            result.stopReason = generationLimit ? "Generated-node limit reached" : frontier.Count == 0
                ? "Frontier exhausted within depth limit" : "Expansion budget reached";
            if (chosen == null) { result.stopReason = "No valid predicted transition"; return result; }
            result.valid = true;
            result.reachedHorizon = bestLeaf != null;
            result.finalScore = chosen.priority;
            for (Node node = chosen; node.parent != null; node = node.parent) result.steps.Add(node.step);
            result.steps.Reverse();
            if (!result.reachedHorizon) result.stopReason += "; returning best explored partial plan";
            return result;
        }

        private static bool Better(Node a, Node b) => b == null || a.priority < b.priority ||
            (a.priority == b.priority && a.id < b.id);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
