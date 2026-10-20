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
            public float eventPenalty, anchorPenalty, priority, trackingScore;
            public float sightingBonus;
            public bool predictsSighting;
        }

        public static DirectorSearchPlan Search(DirectorPlanningState source,
            IReadOnlyList<DirectorPlanningAction> suppliedActions, IReadOnlyList<DirectorEventRecord> history,
            float desiredTension, float sampledAt, bool manualOnly, DirectorSettings director,
            DirectorTensionSettings tension, DirectorScoringSettings scoring, bool scoreWholeTrajectory = false,
            bool varyLiveChoice = false, float secondsSinceApparitionSeen = 0f)
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
            var alternatives = new Dictionary<string, Node>();
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
                        : DirectorStatePredictor.TryPredictEvent(current.state, action.definition, director, tension, out next, out reason, action.durationOverride);
                    if (!predicted) continue;
                    float start = sampledAt + current.state.ElapsedSeconds;
                    float end = sampledAt + next.ElapsedSeconds;
                    if (!Finite(start) || !Finite(end)) continue;
                    var repetition = action.IsRest ? default : DirectorHeuristic.GetRepetition(current.history,
                        action.definition.EventType, action.anchorId, start, scoring.RepetitionWindowSeconds);
                    var score = DirectorHeuristic.Score(next.EstimatedPressure, desiredTension,
                        next.Vulnerability, action.IsRest, repetition, scoring,
                        !action.IsRest && action.definition.IsApparition && !current.predictsSighting
                            ? DirectorHeuristic.ApparitionPreference(secondsSinceApparitionSeen, scoring) : 0f);
                    if (!score.valid) continue;
                    var child = new Node { id = result.generatedNodes++, depth = current.depth + 1,
                        parent = current, state = next, history = new List<DirectorEventRecord>(current.history),
                        eventPenalty = current.eventPenalty + score.eventRepetitionPenalty,
                        anchorPenalty = current.anchorPenalty + score.anchorRepetitionPenalty,
                        sightingBonus = current.sightingBonus + score.apparitionSightingBonus,
                        predictsSighting = current.predictsSighting || (!action.IsRest && action.definition.IsApparition) };
                    // Retain repetition penalties across Rest: waiting must not erase the
                    // earlier cost of selecting a repetitive scare in this branch.
                    child.trackingScore = (scoreWholeTrajectory ? current.trackingScore : 0f) +
                        score.targetDifference + score.overshootPenalty;
                    // Live replanning scores intermediate states too, preventing a useful
                    // scare being pushed to the last step on every one-second replan.
                    // Credit one predicted sighting per branch; Rest cannot erase it or farm extra credit.
                    // Use current absence, not future waiting, so postponement earns no extra bonus.
                    child.priority = child.trackingScore + child.eventPenalty + child.anchorPenalty - child.sightingBonus;
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
                        Node first = child;
                        while (first.parent != root) first = first.parent;
                        string type = first.step.action.Label;
                        if (!alternatives.TryGetValue(type, out var previous) || Better(child, previous))
                            alternatives[type] = child;
                    }
                    else frontier.Add(child);
                }
            }
            Node chosen = bestLeaf ?? bestPartial;
            // One best plan per event type: rooms with many light anchors get no extra lottery tickets.
            // Search remains heuristic-driven; only similarly scored complete live plans participate.
            if (varyLiveChoice && !manualOnly && bestLeaf != null && scoring.LiveChoiceTolerance > 0f)
            {
                var pool = new List<Node>();
                var weights = new List<float>();
                float total = 0f;
                foreach (var candidate in alternatives.Values)
                {
                    float difference = (candidate.priority - bestLeaf.priority) / director.PlanningDepth;
                    if (difference > scoring.LiveChoiceTolerance) continue;
                    float weight = (float)Math.Exp(-2f * difference / scoring.LiveChoiceTolerance);
                    pool.Add(candidate); weights.Add(weight); total += weight;
                }
                float pick = UnityEngine.Random.value * total;
                for (int i = 0; i < pool.Count; i++)
                { pick -= weights[i]; if (pick <= 0f || i == pool.Count - 1) { chosen = pool[i]; break; } }
            }
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
