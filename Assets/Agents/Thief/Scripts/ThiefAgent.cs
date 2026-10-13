using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// The Thief's high-level states, published through ThiefAgent.State for animation and debugging.
public enum ThiefState
{
    Deciding,
    GoToItem,
    Stealing,
    GoToStash,
    Delivering,
    WaitAtDoor,
    Dead
}

/*
 * Resource Thief agent: steals the supplies the player needs and delivers them to a stash.
 * It decides on a target, plans routes over the shared waypoint graph and publishes them through the
 * output contract below. It never moves itself: the Agent Controller (or the test mover in the Thief test
 * scene) turns CurrentPath into movement and applies SpeedMultiplier to the NavMeshAgent's speed.
 * Loop: Deciding -> GoToItem -> Stealing -> GoToStash -> Delivering -> Deciding,
 * with WaitAtDoor during either trip, and Dead until it respawns at a random Thief spawn.
 */
[RequireComponent(typeof(NavMeshAgent))]
public class ThiefAgent : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Shared waypoint graph. Found in the scene when left empty.")]
    [SerializeField] private WaypointGraph graph;
    [Tooltip("The player's stats, read at each decision. Found in the scene when left empty.")]
    [SerializeField] private PlayerStats player;

    [Header("Path costs")]
    [Tooltip("Extra cost for entering a doorway node, for the wait while the door swings open.")]
    [SerializeField] private float doorPenalty = 2f;
    [Tooltip("Cost multiplier for edges touching a Hunter spawn node, so routes past Hunter spawns look cheaper.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float hunterSpawnDiscount = 0.5f;
    [Tooltip("Name prefix of the Hunter spawn points; each is matched to its nearest graph node.")]
    [SerializeField] private string hunterSpawnPrefix = "HunterSpawn";

    [Header("Travel")]
    [Tooltip("Horizontal distance to the last path point at which the Thief counts as arrived.")]
    [SerializeField] private float arriveDistance = 0.3f;
    [Tooltip("Seconds the Thief waits at a door after opening it, covering the door's swing.")]
    [SerializeField] private float doorOpenWait = 1f;
    [Tooltip("Minimum clearance from walls and furniture for the spot the Thief stands on beside an item.")]
    [SerializeField] private float standClearance = 0.4f;
    [Tooltip("Furthest an item's standing spot may be from the item, measured flat.")]
    [SerializeField] private float standSearchRadius = 1.5f;

    // Output contract

    // World positions the Thief wants to walk through, in order. Empty while it stands still.
    public IReadOnlyList<Vector3> CurrentPath => currentPath;

    // What the Thief is doing right now.
    public ThiefState State { get; private set; } = ThiefState.Deciding;

    // True while a stolen item is held.
    public bool IsCarrying => carriedItem != null;

    // Factor applied to the NavMeshAgent's base speed by whatever moves the Thief.
    public float SpeedMultiplier { get; private set; } = 1f;

    // Raised whenever CurrentPath is replaced or cleared, with the new path.
    public event Action<IReadOnlyList<Vector3>> OnPathChanged;

    // Raised whenever State changes, with the new state.
    public event Action<ThiefState> OnStateChanged;

    private readonly List<Vector3> currentPath = new List<Vector3>();
    private PickupInteractable carriedItem = null;
    private NavMeshAgent agent;

    // NavMesh queries use the Thief's own agent type, the one the hospital NavMesh is baked for.
    private NavMeshQueryFilter navFilter;

    // Graph nodes at the Hunter spawn points, whose edges get the discount.
    private readonly HashSet<WaypointNode> hunterSpawnNodes = new HashSet<WaypointNode>();

    // Barricade nodes this Thief has found intact. Planning treats every other barricade as passable.
    private readonly HashSet<WaypointNode> knownBlocked = new HashSet<WaypointNode>();

    /*
     * The current trip: the planned node route, split into segments that end in front of each doorway or
     * barricade node (a checkpoint). segmentStart is the route index where the next segment begins.
     */
    private readonly List<WaypointNode> route = new List<WaypointNode>();
    private readonly List<Vector3> finalStretch = new List<Vector3>();
    private int segmentStart;
    private WaypointNode checkpoint;
    private ThiefState tripState;
    private float doorReadyTime;

    // True once the Thief stands at the end of its trip.
    private bool TripArrived { get; set; }

    // True when a door on the route turned out to be locked, so the route needs replanning.
    private bool TripBlocked { get; set; }

    // True while the Thief is on a trip, including waiting at a door along the way.
    private bool IsTravelling =>
        State == ThiefState.GoToItem || State == ThiefState.GoToStash || State == ThiefState.WaitAtDoor;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        navFilter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = NavMesh.AllAreas };

        if (graph == null) graph = FindAnyObjectByType<WaypointGraph>();
        if (player == null) player = FindAnyObjectByType<PlayerStats>();

        if (graph == null) Debug.LogError($"ThiefAgent '{name}' found no WaypointGraph in the scene.", this);
        if (player == null) Debug.LogError($"ThiefAgent '{name}' found no PlayerStats in the scene.", this);
    }

    // Start runs after every Awake, so the graph has collected its nodes by now.
    private void Start()
    {
        FindHunterSpawnNodes();
    }

    private void Update()
    {
        if (IsTravelling) TickTrip();
    }

    // Trips and door checkpoints

    /*
     * Begins walking the given node route in the given travel state (GoToItem or GoToStash).
     * The route is published one segment at a time, each ending in front of the next doorway or barricade.
     * Optional extra points (the final stretch) are walked after the last node, for targets off the graph.
     */
    private void StartTrip(List<WaypointNode> newRoute, ThiefState travelState, IEnumerable<Vector3> extraPoints = null)
    {
        route.Clear();
        route.AddRange(newRoute);
        finalStretch.Clear();
        if (extraPoints != null) finalStretch.AddRange(extraPoints);
        segmentStart = 0;
        tripState = travelState;
        TripArrived = false;
        TripBlocked = false;

        SetState(travelState);
        PublishNextSegment();
    }

    /*
     * Publishes route nodes from segmentStart onward, stopping at the node just before the next doorway or
     * barricade node. That node becomes the checkpoint, and the following segment starts on it.
     */
    private void PublishNextSegment()
    {
        var points = new List<Vector3>();
        checkpoint = null;

        int i = segmentStart;
        for (; i < route.Count; i++)
        {
            points.Add(route[i].Position);

            bool nextIsCheckpoint = i + 1 < route.Count && route[i + 1].Type != WaypointType.Normal;
            if (nextIsCheckpoint)
            {
                checkpoint = route[i + 1];
                i++;
                break;
            }
        }

        segmentStart = i;

        // The last segment carries on past the final node into the final stretch.
        if (checkpoint == null) points.AddRange(finalStretch);

        SetPath(points);
    }

    // Final stretch

    /*
     * The closest open floor point beside an item: within standSearchRadius of it (measured flat), at least
     * standClearance from walls and furniture, and on the item's side of any wall (short, complete NavMesh path
     * from the floor just below the item). Falls back to the nearest NavMesh point when none qualifies.
     */
    private Vector3 FindStandPoint(Vector3 item)
    {
        NavMesh.SamplePosition(new Vector3(item.x, 0f, item.z), out NavMeshHit near, 2f, navFilter);
        var path = new NavMeshPath();

        for (float r = 0.5f; r <= standSearchRadius; r += 0.25f)
        {
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                Vector3 p = new Vector3(item.x + Mathf.Cos(a) * r, 0f, item.z + Mathf.Sin(a) * r);

                if (!NavMesh.SamplePosition(p, out NavMeshHit hit, 0.1f, navFilter)) continue;
                if (!NavMesh.FindClosestEdge(hit.position, out NavMeshHit edge, navFilter) || edge.distance < standClearance) continue;

                // A long or broken path means the point is on the other side of a wall.
                if (!NavMesh.CalculatePath(near.position, hit.position, navFilter, path) ||
                    path.status != NavMeshPathStatus.PathComplete || PathLength(path) > 3f) continue;

                return hit.position;
            }
        }

        return near.position;
    }

    /*
     * NavMesh corner points from a graph node to an off-graph target, so the last bit of a trip bends around
     * furniture. Null when no complete NavMesh path exists, which marks the target as unreachable.
     */
    private List<Vector3> FinalStretchTo(WaypointNode fromNode, Vector3 target)
    {
        if (!NavMesh.SamplePosition(fromNode.Position, out NavMeshHit start, 1f, navFilter)) return null;

        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(start.position, target, navFilter, path) ||
            path.status != NavMeshPathStatus.PathComplete) return null;

        // The first corner is the node's own position, already the end of the node route.
        var corners = new List<Vector3>(path.corners);
        if (corners.Count > 0) corners.RemoveAt(0);
        return corners;
    }

    private static float PathLength(NavMeshPath path)
    {
        float length = 0f;
        for (int i = 1; i < path.corners.Length; i++)
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        return length;
    }

    /*
     * Runs every frame during a trip. On reaching the end of a segment it either finishes the trip,
     * opens and waits at a door, or carries on past a barricade. After the door wait it resumes the trip.
     */
    private void TickTrip()
    {
        if (State == ThiefState.WaitAtDoor)
        {
            if (!DoorReady(checkpoint.Door)) return;

            SetState(tripState);
            PublishNextSegment();
            return;
        }

        if (!ArrivedAtPathEnd()) return;

        if (checkpoint == null)
        {
            TripArrived = true;
            return;
        }

        if (checkpoint.Type == WaypointType.Door && checkpoint.Door != null)
        {
            if (checkpoint.Door.IsLocked)
            {
                TripBlocked = true;
                return;
            }

            BeginOpening(checkpoint.Door);
            return;
        }

        // Barricade checkpoints are walked straight through for now.
        PublishNextSegment();
    }

    // Opens the door and waits doorOpenWait seconds, even if it was already open, so it has finished swinging.
    private void BeginOpening(Door door)
    {
        door.Open();
        doorReadyTime = Time.time + doorOpenWait;
        SetState(ThiefState.WaitAtDoor);
    }

    // True once the door is passable and the wait has passed. The only place that decides when to walk on.
    private bool DoorReady(Door door)
    {
        if (door == null) return true;
        return door.IsOpen && Time.time >= doorReadyTime;
    }

    // True when the Thief stands within arriveDistance of the last point of its published path.
    private bool ArrivedAtPathEnd()
    {
        if (currentPath.Count == 0) return false;

        Vector3 offset = currentPath[currentPath.Count - 1] - transform.position;
        offset.y = 0f;
        return offset.magnitude <= arriveDistance;
    }

    // Path costs

    /*
     * Cost of stepping from one node to a neighbour: the edge's length, halved (by default) when either end
     * is a Hunter spawn node, plus the door penalty when the step enters a doorway node.
     * A blocked step costs infinity, so the search never takes it.
     */
    private float EdgeCost(WaypointNode from, WaypointNode to)
    {
        if (IsBlocked(to)) return float.PositiveInfinity;

        float cost = Vector3.Distance(from.Position, to.Position);
        if (hunterSpawnNodes.Contains(from) || hunterSpawnNodes.Contains(to)) cost *= hunterSpawnDiscount;
        if (to.Type == WaypointType.Door) cost += doorPenalty;

        return cost;
    }

    /*
     * A doorway is blocked while its door is locked. A barricade is blocked only once this Thief has found it
     * intact; until then it plans as if the way is open.
     */
    private bool IsBlocked(WaypointNode node)
    {
        switch (node.Type)
        {
            case WaypointType.Door: return node.Door != null && node.Door.IsLocked;
            case WaypointType.Barricade: return knownBlocked.Contains(node);
            default: return false;
        }
    }

    /*
     * Cheapest possible cost per metre of any edge. The search scales its distance estimate by this,
     * so the estimate never exceeds the real remaining cost even on discounted edges.
     */
    private float MinCostPerMetre => Mathf.Min(1f, hunterSpawnDiscount);

    // The cheapest route between two nodes under this Thief's cost rules; empty when none exists.
    private List<WaypointNode> PlanRoute(WaypointNode from, WaypointNode to)
    {
        return ThiefPathfinder.FindPath(from, to, EdgeCost, MinCostPerMetre);
    }

    private void FindHunterSpawnNodes()
    {
        hunterSpawnNodes.Clear();
        if (graph == null) return;

        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (!t.name.StartsWith(hunterSpawnPrefix)) continue;

            WaypointNode node = graph.GetNearestNode(t.position);
            if (node != null) hunterSpawnNodes.Add(node);
        }
    }

    // State and path bookkeeping

    // Switches state and announces it; repeated calls with the same state do nothing.
    private void SetState(ThiefState next)
    {
        if (State == next) return;

        State = next;
        OnStateChanged?.Invoke(State);
    }

    // Replaces the published path with the given points and announces it.
    private void SetPath(IEnumerable<Vector3> points)
    {
        currentPath.Clear();
        currentPath.AddRange(points);
        OnPathChanged?.Invoke(currentPath);
    }

    // Empties the published path, which tells the mover to stand still.
    private void ClearPath()
    {
        if (currentPath.Count == 0) return;

        currentPath.Clear();
        OnPathChanged?.Invoke(currentPath);
    }
}
