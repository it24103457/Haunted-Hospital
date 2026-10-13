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

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        navFilter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = NavMesh.AllAreas };

        if (graph == null) graph = FindAnyObjectByType<WaypointGraph>();
        if (player == null) player = FindAnyObjectByType<PlayerStats>();

        if (graph == null) Debug.LogError($"ThiefAgent '{name}' found no WaypointGraph in the scene.", this);
        if (player == null) Debug.LogError($"ThiefAgent '{name}' found no PlayerStats in the scene.", this);
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
