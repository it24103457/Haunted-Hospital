using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * A* search over the shared waypoint graph.
 * Knows nothing about thieves, doors or the player: the caller supplies the cost of each step, and this
 * returns the cheapest chain of nodes from start to goal. Steps that cost infinity are treated as blocked.
 * The distance estimate is the straight-line distance scaled by the cheapest cost per metre any step can
 * have, so it never overestimates the remaining cost and the route found is always the cheapest one.
 */
public static class ThiefPathfinder
{
    /*
     * The cheapest route from start to goal, start and goal included, or an empty list when no route exists.
     * edgeCost(from, to) prices one step between neighbouring nodes; minCostPerMetre is the lowest price per
     * metre edgeCost can return, used to scale the distance estimate.
     */
    public static List<WaypointNode> FindPath(WaypointNode start, WaypointNode goal,
                                              Func<WaypointNode, WaypointNode, float> edgeCost, float minCostPerMetre)
    {
        var route = new List<WaypointNode>();
        if (start == null || goal == null) return route;
        if (start == goal) { route.Add(start); return route; }

        var open = new List<WaypointNode> { start };
        var closed = new HashSet<WaypointNode>();
        var cameFrom = new Dictionary<WaypointNode, WaypointNode>();
        var costSoFar = new Dictionary<WaypointNode, float> { [start] = 0f };
        var estimatedTotal = new Dictionary<WaypointNode, float> { [start] = Estimate(start, goal, minCostPerMetre) };

        while (open.Count > 0)
        {
            WaypointNode current = Cheapest(open, estimatedTotal);
            if (current == goal) return Reconstruct(cameFrom, current);

            open.Remove(current);
            closed.Add(current);

            foreach (WaypointNode next in current.Neighbours)
            {
                if (next == null || closed.Contains(next)) continue;

                float step = edgeCost(current, next);
                if (float.IsPositiveInfinity(step)) continue;

                float cost = costSoFar[current] + step;
                if (costSoFar.TryGetValue(next, out float known) && cost >= known) continue;

                cameFrom[next] = current;
                costSoFar[next] = cost;
                estimatedTotal[next] = cost + Estimate(next, goal, minCostPerMetre);
                if (!open.Contains(next)) open.Add(next);
            }
        }

        return route;
    }

    // Straight-line distance to the goal, scaled so it can never exceed the real remaining cost.
    private static float Estimate(WaypointNode from, WaypointNode goal, float minCostPerMetre)
    {
        return Vector3.Distance(from.Position, goal.Position) * minCostPerMetre;
    }

    // The open node with the lowest estimated total cost. A linear scan is fast enough for a graph this size.
    private static WaypointNode Cheapest(List<WaypointNode> open, Dictionary<WaypointNode, float> estimatedTotal)
    {
        WaypointNode best = open[0];
        float bestCost = estimatedTotal[best];

        for (int i = 1; i < open.Count; i++)
        {
            float cost = estimatedTotal[open[i]];
            if (cost < bestCost) { bestCost = cost; best = open[i]; }
        }

        return best;
    }

    // Walks the cameFrom links back from the goal and returns the route in start-to-goal order.
    private static List<WaypointNode> Reconstruct(Dictionary<WaypointNode, WaypointNode> cameFrom, WaypointNode goal)
    {
        var route = new List<WaypointNode> { goal };
        WaypointNode node = goal;

        while (cameFrom.TryGetValue(node, out WaypointNode previous))
        {
            route.Add(previous);
            node = previous;
        }

        route.Reverse();
        return route;
    }
}
