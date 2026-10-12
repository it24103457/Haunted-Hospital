using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/*
 * The waypoint graph shared by the Thief and the Hunter. All WaypointNodes are children of this object,
 * which sits at (0,0,0) so the prefab lines up with the level in any scene.
 * Edges are generated in the editor from the baked NavMesh: two nodes are linked when a straight,
 * walkable NavMesh line exists between them.
 * Door and Barricade nodes only link to close nodes (their front and behind nodes), and no edge passes
 * through a doorway without its Door or Barricade node, so every route through one is front -> on -> behind.
 */
public class WaypointGraph : MonoBehaviour
{
    [Header("Rebuild Edges (editor only)")]
    [Tooltip("Maximum length of an edge between two Normal nodes.")]
    [SerializeField] private float maxLinkDistance = 12f;
    [Tooltip("Maximum length of an edge touching a Door or Barricade node (reaches its front and behind nodes).")]
    [SerializeField] private float specialLinkDistance = 1.5f;
    [Tooltip("Minimum distance between an edge and any Door or Barricade node it doesn't connect to.")]
    [SerializeField] private float doorwayClearance = 0.6f;

    [Header("NavMesh queries")]
    [Tooltip("Search radius for snapping points onto the NavMesh.")]
    [SerializeField] private float sampleRadius = 1f;
    [Tooltip("Maximum distance GetNearestNode considers.")]
    [SerializeField] private float maxNearestDistance = 15f;

    private readonly List<WaypointNode> nodes = new List<WaypointNode>();

    public IReadOnlyList<WaypointNode> Nodes => nodes;

    private void Awake()
    {
        GetComponentsInChildren(false, nodes);
    }

    /*
     * The closest node with a clear walkable line to the position.
     * Falls back to the plain closest node when none is visible; null when the graph is empty.
     */
    public WaypointNode GetNearestNode(Vector3 position)
    {
        WaypointNode bestVisible = null;
        WaypointNode bestAny = null;
        float bestVisibleDistance = float.MaxValue;
        float bestAnyDistance = float.MaxValue;

        foreach (WaypointNode node in nodes)
        {
            float distance = Vector3.Distance(position, node.Position);

            if (distance < bestAnyDistance)
            {
                bestAnyDistance = distance;
                bestAny = node;
            }

            if (distance > maxNearestDistance || distance >= bestVisibleDistance) continue;

            if (HasClearLine(position, node.Position))
            {
                bestVisibleDistance = distance;
                bestVisible = node;
            }
        }

        return bestVisible != null ? bestVisible : bestAny;
    }

    // True when a straight line between the two points stays on walkable NavMesh.
    public bool HasClearLine(Vector3 from, Vector3 to)
    {
        if (!NavMesh.SamplePosition(from, out NavMeshHit a, sampleRadius, NavMesh.AllAreas)) return false;
        if (!NavMesh.SamplePosition(to, out NavMeshHit b, sampleRadius, NavMesh.AllAreas)) return false;

        return !NavMesh.Raycast(a.position, b.position, out _, NavMesh.AllAreas);
    }

#if UNITY_EDITOR
    // Clears and regenerates every edge, then reports node, edge and isolated-node counts.
    [ContextMenu("Rebuild Edges")]
    private void RebuildEdges()
    {
        WaypointNode[] all = GetComponentsInChildren<WaypointNode>(true);

        foreach (WaypointNode n in all)
        {
            Undo.RecordObject(n, "Rebuild Edges");
            n.EditorClearNeighbours();
        }

        int edges = 0;

        for (int i = 0; i < all.Length; i++)
        {
            for (int j = i + 1; j < all.Length; j++)
            {
                if (!CanLink(all[i], all[j], all)) continue;

                all[i].EditorAddNeighbour(all[j]);
                all[j].EditorAddNeighbour(all[i]);
                edges++;
            }
        }

        int isolated = 0;
        foreach (WaypointNode n in all)
        {
            if (n.Neighbours.Count == 0)
            {
                isolated++;
                Debug.LogWarning($"[WaypointGraph] '{n.name}' has no neighbours.", n);
            }

            EditorUtility.SetDirty(n);
            PrefabUtility.RecordPrefabInstancePropertyModifications(n);
        }

        Debug.Log($"[WaypointGraph] Rebuilt: {all.Length} nodes, {edges} edges, {isolated} isolated.", this);
    }

    /*
     * Two nodes are linked when they are within range, the edge keeps clear of other doorway nodes,
     * and the straight line between them is walkable.
     */
    private bool CanLink(WaypointNode a, WaypointNode b, WaypointNode[] all)
    {
        float distance = Vector3.Distance(a.Position, b.Position);
        bool special = a.Type != WaypointType.Normal || b.Type != WaypointType.Normal;

        if (distance > (special ? specialLinkDistance : maxLinkDistance)) return false;

        // An edge passing close to a Door or Barricade node would skip that doorway node.
        foreach (WaypointNode s in all)
        {
            if (s == a || s == b || s.Type == WaypointType.Normal) continue;
            if (DistanceToSegment(s.Position, a.Position, b.Position) < doorwayClearance) return false;
        }

        return HasClearLine(a.Position, b.Position);
    }

    private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
        return Vector3.Distance(p, a + t * ab);
    }
#endif
}