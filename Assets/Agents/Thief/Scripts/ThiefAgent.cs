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
