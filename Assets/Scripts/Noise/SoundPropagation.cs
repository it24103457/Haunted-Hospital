using System;
using System.Collections.Generic;
using UnityEngine;

// How sound loses loudness as it travels. Tune these; every value has a physical meaning.
[Serializable]
public class SoundPropagationSettings
{
    [Tooltip("Loudness lost per metre travelled, as an exponential decay: L = L0 * e^(-k * distance).")]
    public float attenuationPerMetre = 0.12f;

    [Tooltip("Fraction of loudness that gets through a closed door node (1 = no damping).")]
    [Range(0.01f, 1f)] public float closedDoorFactor = 0.4f;

    [Tooltip("Fraction of loudness that gets through an intact barricade node (1 = no damping).")]
    [Range(0.01f, 1f)] public float barricadeFactor = 0.25f;
}

// Spreads a sound through the shared waypoint graph instead of through the air.
// Why a graph and not straight-line distance: walls have no edges, so sound cannot cross them, and a sound
// in the next room is only heard if it can travel round through the doorway.
// The maths: every step multiplies loudness by a factor (e^(-k*d) along an edge, times a door or barricade
// factor when entering such a node). Taking logs turns the product into a sum, so with
//     cost(path) = sum of ( k * edgeLength  +  -ln(damping of the node entered) )
// the loudest route to a node is the cheapest one, and the loudness there is  L0 * e^(-cost).
// All costs are non-negative, so Dijkstra's algorithm finds that route exactly.
// A sound is dropped once its cost passes ln(L0 / hearingThreshold), which keeps the flood small.
public static class SoundPropagation
{
    private static readonly Dictionary<WaypointNode, float> bestCost = new Dictionary<WaypointNode, float>();
    private static readonly NodeHeap heap = new NodeHeap();

    // Fills 'result' with the loudness reaching every node that can hear the sound (at or above minLoudness).
    // 'result' is cleared first, so callers can reuse one dictionary and avoid allocating.
 
    public static void Propagate(WaypointGraph graph, Vector3 position, float loudness, float minLoudness,
                                 SoundPropagationSettings settings, Dictionary<WaypointNode, float> result)
    {
        result.Clear();
        Run(graph, position, loudness, minLoudness, settings, null, result, out _);
    }

    // Asks one question: how loud does this sound arrive at the listener's node?
    // Stops as soon as that node is reached. Returns false (and 0) if it is too quiet to hear.
    public static bool TryHear(WaypointGraph graph, Vector3 position, float loudness, WaypointNode listener,
                               float minLoudness, SoundPropagationSettings settings, out float heardLoudness)
    {
        Run(graph, position, loudness, minLoudness, settings, listener, null, out heardLoudness);
        return heardLoudness >= minLoudness;
    }

    private static void Run(WaypointGraph graph, Vector3 position, float loudness, float minLoudness,
                            SoundPropagationSettings settings, WaypointNode target,
                            Dictionary<WaypointNode, float> result, out float targetLoudness)
    {
        targetLoudness = 0f;

        // minLoudness must be positive: the cut-off below takes a logarithm of loudness / minLoudness.
        if (graph == null || settings == null || minLoudness <= 0f || loudness <= minLoudness) return;

        WaypointNode source = graph.GetNearestNode(position);
        if (source == null) return;

        float k = settings.attenuationPerMetre;
        float maxCost = Mathf.Log(loudness / minLoudness);

        // The sound first travels from where it happened to the nearest node.
        float startCost = k * Vector3.Distance(position, source.Position);
        if (startCost > maxCost) return;

        bestCost.Clear();
        heap.Clear();

        bestCost[source] = startCost;
        heap.Push(source, startCost);

        while (heap.Count > 0)
        {
            heap.Pop(out WaypointNode node, out float cost);

            // A cheaper route to this node was found after this entry was queued.
            if (cost > bestCost[node]) continue;

            float arriving = loudness * Mathf.Exp(-cost);
            if (result != null) result[node] = arriving;

            if (node == target)
            {
                targetLoudness = arriving;
                return;
            }

            foreach (WaypointNode next in node.Neighbours)
            {
                if (next == null) continue;

                float nextCost = cost + k * Vector3.Distance(node.Position, next.Position) + Damping(next, settings);
                if (nextCost > maxCost) continue;

                if (bestCost.TryGetValue(next, out float known) && known <= nextCost) continue;

                bestCost[next] = nextCost;
                heap.Push(next, nextCost);
            }
        }
    }

    // Extra cost for entering a node that muffles sound. 0 for ordinary nodes and for open doors.
    private static float Damping(WaypointNode node, SoundPropagationSettings settings)
    {
        float factor = 1f;

        if (node.Type == WaypointType.Door)
        {
            if (node.Door != null && !node.Door.IsOpen) factor = settings.closedDoorFactor;
        }
        else if (node.Type == WaypointType.Barricade)
        {
            if (node.IsBarricadeIntact) factor = settings.barricadeFactor;
        }

        return -Mathf.Log(Mathf.Clamp(factor, 0.001f, 1f));
    }

    // A small binary min-heap of (node, cost). Reused between calls so a noise does not allocate.
    private sealed class NodeHeap
    {
        private WaypointNode[] nodes = new WaypointNode[64];
        private float[] costs = new float[64];

        public int Count { get; private set; }

        public void Clear()
        {
            Array.Clear(nodes, 0, Count);
            Count = 0;
        }

        public void Push(WaypointNode node, float cost)
        {
            if (Count == nodes.Length)
            {
                Array.Resize(ref nodes, Count * 2);
                Array.Resize(ref costs, Count * 2);
            }

            int i = Count++;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (costs[parent] <= cost) break;

                nodes[i] = nodes[parent];
                costs[i] = costs[parent];
                i = parent;
            }

            nodes[i] = node;
            costs[i] = cost;
        }

        public void Pop(out WaypointNode node, out float cost)
        {
            node = nodes[0];
            cost = costs[0];

            Count--;
            if (Count == 0)
            {
                nodes[0] = null;
                return;
            }

            WaypointNode lastNode = nodes[Count];
            float lastCost = costs[Count];
            nodes[Count] = null;

            int i = 0;
            while (true)
            {
                int child = 2 * i + 1;
                if (child >= Count) break;
                if (child + 1 < Count && costs[child + 1] < costs[child]) child++;
                if (costs[child] >= lastCost) break;

                nodes[i] = nodes[child];
                costs[i] = costs[child];
                i = child;
            }

            nodes[i] = lastNode;
            costs[i] = lastCost;
        }
    }
}
