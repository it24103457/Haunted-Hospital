using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * The Sound Hunter's decision maker (skeleton: it spawns, patrols and can be shot dead).
 *
 * Like the Thief, it never moves itself. It publishes what it wants through the output contract below,
 * and a mover (HunterTestMover for now, the Agent Controller later) turns CurrentPath into movement and applies
 * SpeedMultiplier to the NavMeshAgent's speed.
 *
 * Other scripts READ it through IHunterStatus (the Director) or the contract below (the mover);
 * only this script changes the state.
 *
 * What works now:
 *   Spawn   at a random "HunterSpawn" marker (or where it is placed if there are none).
 *   Patrol  picks a reachable Normal waypoint far enough away, publishes it as a one-point path, waits there
 *           for a moment, then picks the next. The NavMeshAgent does the actual route finding, so closed
 *           doors and intact barricades are simply not crossed yet. The Hunter's own A* and door handling
 *           replace this in a later step.
 *   Damage  implements IDamageable, so the player's shots hurt it; at 0 health it becomes Dead.
 * Hearing (HunterEars) and the other states are added later.
 */
public class HunterBrain : MonoBehaviour, IDamageable, IHunterStatus
{
    [Header("References")]
    [Tooltip("All the Hunter's tuning numbers. Required.")]
    [SerializeField] private HunterSettings settings;
    [Tooltip("The shared waypoint graph. Found in the scene when left empty.")]
    [SerializeField] private WaypointGraph graph;

    [Header("Spawning")]
    [Tooltip("Name prefix of the Hunter spawn markers in the scene.")]
    [SerializeField] private string spawnPointPrefix = "HunterSpawn";
    [Tooltip("Spawn as soon as play starts.")]
    [SerializeField] private bool spawnOnStart = true;

    // Output contract (what the mover and animation read)

    /// <summary>What the Hunter is doing right now. Inactive until it spawns.</summary>
    public HunterState State { get; private set; } = HunterState.Inactive;

    /// <summary>World positions the Hunter wants to walk through, in order. Empty while it stands still.</summary>
    public IReadOnlyList<Vector3> CurrentPath => currentPath;

    /// <summary>Factor applied to the NavMeshAgent's base speed by whatever moves the Hunter. 0 means stand still.</summary>
    public float SpeedMultiplier { get; private set; }

    /// <summary>A point the Hunter should face instead of facing where it walks, or null for none.</summary>
    public Vector3? LookTarget { get; private set; }

    public float Health { get; private set; }
    public float MaxHealth => settings != null ? settings.MaxHealth : 0f;

    /// <summary>Raised whenever CurrentPath is replaced or cleared, with the new path.</summary>
    public event Action<IReadOnlyList<Vector3>> OnPathChanged;

    /// <summary>Raised whenever State changes, with the new state.</summary>
    public event Action<HunterState> OnStateChanged;

    /// <summary>Raised each time the Hunter appears in the level.</summary>
    public event Action OnSpawned;

    /// <summary>Raised when the Hunter takes damage (and survives or not), with the hit. HunterEars will use this to look toward the shooter.</summary>
    public event Action<DamageInfo> OnDamaged;

    /// <summary>Raised when the Hunter dies.</summary>
    public event Action OnDied;

    // IHunterStatus (read-only view for the Director)

    public bool IsReady { get; private set; }
    public bool IsActive => State != HunterState.Inactive && State != HunterState.Dead;
    public bool IsAlive => State != HunterState.Dead;
    public bool IsChasingPlayer => State == HunterState.Chase;
    public bool IsAttackingPlayer => State == HunterState.Attack;
    public event Action StateChanged;

    // Internals

    private readonly List<Vector3> currentPath = new List<Vector3>();
    private readonly List<WaypointNode> candidates = new List<WaypointNode>();
    private NavMeshPath scratchPath;
    private NavMeshAgent agent;
    private NavMeshQueryFilter navFilter;

    private Vector3 patrolDestination;
    private bool hasPatrolDestination;
    private float patrolWaitUntil;
    private float patrolLegStart;
    private bool reportedNoPatrolPoint;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        scratchPath = new NavMeshPath();

        // Path checks use the Hunter's own agent type, the one the hospital NavMesh is baked for.
        navFilter = new NavMeshQueryFilter { areaMask = NavMesh.AllAreas };
        if (agent != null) navFilter.agentTypeID = agent.agentTypeID;
    }

    private void Start()
    {
        if (graph == null) graph = FindAnyObjectByType<WaypointGraph>();

        // Report a missing reference once, and expose the Hunter as unavailable instead of throwing errors every frame.
        if (settings == null)
            Debug.LogError("[Hunter] HunterSettings is not assigned, so the Hunter is unavailable.", this);
        else if (graph == null)
            Debug.LogError("[Hunter] No WaypointGraph found in the scene, so the Hunter is unavailable.", this);
        else if (graph.Nodes.Count == 0)
            Debug.LogError("[Hunter] The WaypointGraph has no nodes, so the Hunter is unavailable.", this);
        else
            IsReady = true;

        StateChanged?.Invoke();

        if (IsReady && spawnOnStart) Spawn();
    }

    private void Update()
    {
        if (!IsReady) return;

        switch (State)
        {
            case HunterState.Patrol:
                UpdatePatrol();
                break;
            // The other states are added in later build steps.
        }
    }

    // Spawning

    /// <summary>Puts the Hunter in the level at full health and starts it patrolling.</summary>
    public void Spawn()
    {
        if (!IsReady) return;

        Health = settings.MaxHealth;
        MoveTo(PickSpawnPosition());

        hasPatrolDestination = false;
        patrolWaitUntil = 0f;
        reportedNoPatrolPoint = false;

        if (agent != null && !agent.isOnNavMesh)
        {
            Debug.LogWarning("[Hunter] The Hunter is not on the NavMesh after spawning. Check the NavMeshAgent's Agent Type " +
                             "matches the NavMesh baked in this scene, and that the spawn marker is on walkable floor.", this);
        }

        SetState(HunterState.Patrol);
        OnSpawned?.Invoke();
    }

    // Chooses a random spawn marker; falls back to where the Hunter was placed.
    private Vector3 PickSpawnPosition()
    {
        var markers = new List<Transform>();
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (t.name.StartsWith(spawnPointPrefix)) markers.Add(t);
        }

        if (markers.Count == 0)
        {
            Debug.LogWarning("[Hunter] No spawn markers found, so it stays where it is placed.", this);
            return transform.position;
        }

        Vector3 position = markers[UnityEngine.Random.Range(0, markers.Count)].position;

        // The filter overload matters: the plain area-mask overload only looks at the Humanoid NavMesh,
        // and this hospital is baked for the Hospital_Blockout agent type.
        return NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, navFilter) ? hit.position : position;
    }

    // Teleports the Hunter, through the NavMeshAgent when there is one so it stays on the NavMesh.
    private void MoveTo(Vector3 position)
    {
        // Warp also works for an agent that started off the NavMesh; it returns false if the spot is not on the mesh.
        if (agent != null && agent.enabled && agent.Warp(position)) return;

        transform.position = position;
    }

    // Patrol

    private void UpdatePatrol()
    {
        if (!hasPatrolDestination)
        {
            if (Time.time >= patrolWaitUntil) PickPatrolDestination();
            return;
        }

        Vector3 offset = patrolDestination - transform.position;
        offset.y = 0f;

        if (offset.magnitude <= settings.ArrivalRadius)
        {
            // Arrived: stand still for a moment, then choose the next point.
            hasPatrolDestination = false;
            SetPath(null);
            patrolWaitUntil = Time.time + settings.PatrolWaitSeconds;
        }
        else if (Time.time - patrolLegStart > settings.PatrolLegTimeoutSeconds)
        {
            // Stuck, or the way is blocked: give up on this point.
            hasPatrolDestination = false;
            SetPath(null);
            patrolWaitUntil = Time.time;
        }
    }

    // Picks a random Normal node that is far enough away and reachable on the NavMesh.
    private void PickPatrolDestination()
    {
        candidates.Clear();
        float minDistance = settings.PatrolMinLegDistance;

        foreach (WaypointNode node in graph.Nodes)
        {
            if (node == null || node.Type != WaypointType.Normal) continue;

            Vector3 offset = node.Position - transform.position;
            offset.y = 0f;
            if (offset.magnitude >= minDistance) candidates.Add(node);
        }

        // Start from the nearest NavMesh point, so a root that sits slightly off the mesh does not fail every check.
        Vector3 from = transform.position;
        bool startOnMesh = NavMesh.SamplePosition(from, out NavMeshHit startHit, 2f, navFilter);
        if (startOnMesh) from = startHit.position;

        int totalCandidates = candidates.Count;
        int noPath = 0, partial = 0;

        // Try a handful of random candidates, which is cheaper than checking all of them.
        for (int attempt = 0; attempt < 12 && candidates.Count > 0; attempt++)
        {
            int i = UnityEngine.Random.Range(0, candidates.Count);
            WaypointNode node = candidates[i];
            candidates.RemoveAt(i);

            if (!NavMesh.CalculatePath(from, node.Position, navFilter, scratchPath)) { noPath++; continue; }
            if (scratchPath.status != NavMeshPathStatus.PathComplete) { partial++; continue; }

            patrolDestination = node.Position;
            hasPatrolDestination = true;
            patrolLegStart = Time.time;
            SetPath(new[] { patrolDestination });
            return;
        }

        // Nothing reachable right now (doors shut, no far node). Try again in a moment, and say so only once.
        if (!reportedNoPatrolPoint && settings.EnableDebugLogging)
        {
            Debug.Log($"[Hunter] No reachable patrol point found; will keep trying. " +
                      $"Agent on NavMesh: {(agent != null && agent.isOnNavMesh)}, start snapped to NavMesh: {startOnMesh}, " +
                      $"far Normal nodes: {totalCandidates}, no path: {noPath}, blocked/partial path: {partial}, " +
                      $"agent type: {navFilter.agentTypeID}, position: {transform.position}.", this);
            reportedNoPatrolPoint = true;
        }
        patrolWaitUntil = Time.time + 1f;
    }

    // Damage and death

    public void TakeDamage(DamageInfo info)
    {
        if (!IsActive || info.Amount <= 0f) return;

        Health = Mathf.Max(0f, Health - info.Amount);
        OnDamaged?.Invoke(info);

        if (settings != null && settings.EnableDebugLogging)
            Debug.Log($"[Hunter] Took {info.Amount} damage, health {Health}/{MaxHealth}.", this);

        if (Health <= 0f) Die();
    }

    private void Die()
    {
        hasPatrolDestination = false;
        LookTarget = null;
        SetPath(null);
        SetState(HunterState.Dead);
        OnDied?.Invoke();
    }

    // State and path bookkeeping

    // Switches state and announces it; repeated calls with the same state do nothing.
    private void SetState(HunterState next)
    {
        if (State == next) return;

        State = next;
        SpeedMultiplier = SpeedFor(next);

        if (settings != null && settings.EnableDebugLogging)
            Debug.Log($"[Hunter] State: {next}", this);

        OnStateChanged?.Invoke(next);
        StateChanged?.Invoke();
    }

    private float SpeedFor(HunterState state)
    {
        switch (state)
        {
            case HunterState.Patrol: return settings.PatrolSpeed;
            case HunterState.Investigate: return settings.InvestigateSpeed;
            case HunterState.Search: return settings.SearchSpeed;
            case HunterState.Chase: return settings.ChaseSpeed;
            default: return 0f; // Inactive, Attack and Dead stand still
        }
    }

    // Replaces the published path (null or empty clears it) and announces it.
    private void SetPath(IReadOnlyList<Vector3> path)
    {
        currentPath.Clear();
        if (path != null) currentPath.AddRange(path);
        OnPathChanged?.Invoke(currentPath);
    }
}
