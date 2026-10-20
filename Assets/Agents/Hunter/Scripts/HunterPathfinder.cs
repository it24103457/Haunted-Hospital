using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * The Hunter's A* over the shared waypoint graph. A step costs its length, plus extra for a closed door.
 * Locked doors, doors the Hunter just failed to open, and intact barricades are blocked.
 * The estimate is the straight-line distance, which never exceeds the real cost, so the route found is the cheapest.
 */
public static class HunterPathfinder
{
    /// <summary>The cheapest route from start to goal (both included), or an empty list when there is none.</summary>
    public static List<WaypointNode> FindPath(WaypointNode start, WaypointNode goal, HunterSettings settings, Func<Door, bool> isBanned)
    {
        var route = new List<WaypointNode>();
        if (start == null || goal == null) return route;
        if (start == goal) { route.Add(start); return route; }

        var open = new List<WaypointNode> { start };
        var closed = new HashSet<WaypointNode>();
        var cameFrom = new Dictionary<WaypointNode, WaypointNode>();
        var costSoFar = new Dictionary<WaypointNode, float> { [start] = 0f };
        var estimatedTotal = new Dictionary<WaypointNode, float> { [start] = Estimate(start, goal) };

        while (open.Count > 0)
        {
            WaypointNode current = Cheapest(open, estimatedTotal);
            if (current == goal) return Reconstruct(cameFrom, current);

            open.Remove(current);
            closed.Add(current);

            foreach (WaypointNode next in current.Neighbours)
            {
                if (next == null || closed.Contains(next)) continue;

                float step = StepCost(current, next, settings, isBanned);
                if (float.IsPositiveInfinity(step)) continue;

                float cost = costSoFar[current] + step;
                if (costSoFar.TryGetValue(next, out float known) && cost >= known) continue;

                cameFrom[next] = current;
                costSoFar[next] = cost;
                estimatedTotal[next] = cost + Estimate(next, goal);
                if (!open.Contains(next)) open.Add(next);
            }
        }

        return route;
    }

    /// <summary>
    /// For an unreachable goal: the route to the reachable node that is closest (in a straight line) to the target.
    /// The result is just the start node when nothing nearer can be reached.
    /// </summary>
    public static List<WaypointNode> FindPathToNearest(WaypointNode start, Vector3 target, HunterSettings settings, Func<Door, bool> isBanned)
    {
        if (start == null) return new List<WaypointNode>();

        var seen = new HashSet<WaypointNode> { start };
        var queue = new Queue<WaypointNode>();
        queue.Enqueue(start);

        WaypointNode best = start;
        float bestDistance = Vector3.Distance(start.Position, target);

        while (queue.Count > 0)
        {
            WaypointNode current = queue.Dequeue();

            foreach (WaypointNode next in current.Neighbours)
            {
                if (next == null || seen.Contains(next) || IsBlocked(next, isBanned)) continue;

                seen.Add(next);
                queue.Enqueue(next);

                float distance = Vector3.Distance(next.Position, target);
                if (distance >= bestDistance) continue;

                bestDistance = distance;
                best = next;
            }
        }

        return FindPath(start, best, settings, isBanned);
    }

    /// <summary>The price of one step onto a neighbouring node. Infinity means blocked.</summary>
    public static float StepCost(WaypointNode from, WaypointNode to, HunterSettings settings, Func<Door, bool> isBanned)
    {
        if (IsBlocked(to, isBanned)) return float.PositiveInfinity;

        float cost = Vector3.Distance(from.Position, to.Position);

        // A closed door costs the time it takes to open it, as extra distance.
        if (to.Type == WaypointType.Door && to.Door != null && !to.Door.IsOpen) cost += settings.ClosedDoorExtraCost;

        return cost;
    }

    /// <summary>True for a locked door, a door the Hunter is avoiding for now, or an intact barricade.</summary>
    public static bool IsBlocked(WaypointNode node, Func<Door, bool> isBanned)
    {
        switch (node.Type)
        {
            case WaypointType.Door:
                return node.Door != null && (node.Door.IsLocked || (isBanned != null && isBanned(node.Door)));
            case WaypointType.Barricade:
                return node.IsBarricadeIntact;
            default:
                return false;
        }
    }

    private static float Estimate(WaypointNode from, WaypointNode goal)
    {
        return Vector3.Distance(from.Position, goal.Position);
    }

    // The open node with the lowest estimated total. A linear scan is fast enough for a graph this size.
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
