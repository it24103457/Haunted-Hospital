using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * The Sound Hunter's decision maker. It spawns, patrols, takes damage and respawns out of the player's sight.
 * It never moves itself: it publishes CurrentPath, State, SpeedMultiplier and LookTarget, and a mover
 * (HunterTestMover for now) acts on them. Other scripts only read it, through IHunterStatus.
 * Hearing and the other states are added in later steps.
 */
public class HunterBrain : MonoBehaviour, IDamageable, IHunterStatus
{
    [Header("References")]
    [Tooltip("All the Hunter's tuning numbers. Required.")]
    [SerializeField] private HunterSettings settings;
    [Tooltip("The shared waypoint graph. Found in the scene when left empty.")]
    [SerializeField] private WaypointGraph graph;
    [Tooltip("The player, used to keep respawns away from them. Found in the scene when left empty.")]
    [SerializeField] private PlayerStats player;
    [Tooltip("The camera the player looks through. Found on the player when left empty.")]
    [SerializeField] private Camera playerCamera;

    [Header("Spawning")]
    [Tooltip("Name prefix of the Hunter spawn markers in the scene.")]
    [SerializeField] private string spawnPointPrefix = "HunterSpawn";
    [Tooltip("Spawn as soon as play starts.")]
    [SerializeField] private bool spawnOnStart = true;

    // Output contract (what the mover and animation read)

    // What the Hunter is doing right now. Inactive until it spawns.
    public HunterState State { get; private set; } = HunterState.Inactive;

    // World positions the Hunter wants to walk through, in order. Empty while it stands still.
    public IReadOnlyList<Vector3> CurrentPath => currentPath;

    // Factor applied to the NavMeshAgent's base speed by whatever moves the Hunter. 0 means stand still.
    public float SpeedMultiplier { get; private set; }

    // A point the Hunter should face instead of facing where it walks, or null for none.
    public Vector3? LookTarget { get; private set; }

    public float Health { get; private set; }
    public float MaxHealth => settings != null ? settings.MaxHealth : 0f;

    // Raised whenever CurrentPath is replaced or cleared, with the new path.
    public event Action<IReadOnlyList<Vector3>> OnPathChanged;

    // Raised whenever State changes, with the new state.
    public event Action<HunterState> OnStateChanged;

    // Raised each time the Hunter appears in the level: the first spawn and every respawn. Memory should be cleared on this.
    public event Action OnSpawned;

    // Raised when the Hunter takes damage (and survives or not), with the hit. HunterEars will use this to look toward the shooter.
    public event Action<DamageInfo> OnDamaged;

    // Raised when the Hunter dies.
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
    private readonly List<Transform> spawnMarkers = new List<Transform>();
    private readonly List<Transform> allowedMarkers = new List<Transform>();
    private NavMeshPath scratchPath;
    private NavMeshAgent agent;
    private NavMeshQueryFilter navFilter;

    // The body: what gets switched off on death and back on at respawn.
    private Renderer[] renderers;
    private Collider[] colliders;
    private bool[] rendererWasOn;
    private bool[] colliderWasOn;

    // Patrol
    private Vector3 patrolDestination;
    private bool hasPatrolDestination;
    private float patrolWaitUntil;
    private float patrolLegStart;
    private bool reportedNoPatrolPoint;

    // Death and respawn
    private float deathTime;
    private bool bodyHidden;
    private float nextRespawnAttempt;
    private bool reportedWaitingToRespawn;
    private bool warnedNoMarkers;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        scratchPath = new NavMeshPath();

        // Path checks use the Hunter's own agent type, the one the hospital NavMesh is baked for.
        navFilter = new NavMeshQueryFilter { areaMask = NavMesh.AllAreas };
        if (agent != null) navFilter.agentTypeID = agent.agentTypeID;

        // Remember what was on at the start, so respawning restores exactly that.
        renderers = GetComponentsInChildren<Renderer>(true);
        rendererWasOn = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) rendererWasOn[i] = renderers[i].enabled;

        colliders = GetComponentsInChildren<Collider>(true);
        colliderWasOn = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++) colliderWasOn[i] = colliders[i].enabled;
    }

    private void Start()
    {
        if (graph == null) graph = FindAnyObjectByType<WaypointGraph>();
        if (player == null) player = FindAnyObjectByType<PlayerStats>();
        if (playerCamera == null && player != null) playerCamera = player.GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;

        // Report a missing reference once, and expose the Hunter as unavailable instead of throwing errors every frame.
        if (settings == null)
            Debug.LogError("[Hunter] HunterSettings is not assigned, so the Hunter is unavailable.", this);
        else if (graph == null)
            Debug.LogError("[Hunter] No WaypointGraph found in the scene, so the Hunter is unavailable.", this);
        else if (graph.Nodes.Count == 0)
            Debug.LogError("[Hunter] The WaypointGraph has no nodes, so the Hunter is unavailable.", this);
        else
            IsReady = true;

        // Without the player the Hunter still works; it just cannot keep respawns away from them.
        if (IsReady && player == null)
            Debug.LogWarning("[Hunter] No PlayerStats found, so respawns cannot avoid the player.", this);

        CacheSpawnMarkers();
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
            case HunterState.Dead:
                UpdateDead();
                break;
            // The other states are added in later build steps.
        }
    }

    // Spawning

    /// <summary>
    /// Puts the Hunter in the level at full health and starts it patrolling.
    /// Prefers a marker away from the player; if none qualifies (this is the first spawn), uses any marker.
    /// </summary>
    public void Spawn()
    {
        if (!TrySpawn(true)) TrySpawn(false);
    }

    // Spawns at an allowed marker; returns false (and changes nothing) if applyPlayerRules is on and no marker qualifies.
    private bool TrySpawn(bool applyPlayerRules)
    {
        if (!IsReady) return false;
        if (!TryPickSpawnPosition(applyPlayerRules, out Vector3 position)) return false;

        SetCollisionActive(true);
        SetVisible(true);
        bodyHidden = false;

        Health = settings.MaxHealth;
        MoveTo(position);

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
        return true;
    }

    private void CacheSpawnMarkers()
    {
        spawnMarkers.Clear();
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (t.name.StartsWith(spawnPointPrefix)) spawnMarkers.Add(t);
        }
    }

    // Chooses a random marker (one that passes the player rules, when asked to); falls back to where the Hunter was placed.
    private bool TryPickSpawnPosition(bool applyPlayerRules, out Vector3 position)
    {
        position = transform.position;

        if (spawnMarkers.Count == 0)
        {
            if (!warnedNoMarkers)
            {
                Debug.LogWarning("[Hunter] No spawn markers found, so it stays where it is placed.", this);
                warnedNoMarkers = true;
            }
            return true;
        }

        allowedMarkers.Clear();
        foreach (Transform marker in spawnMarkers)
        {
            if (marker == null) continue;
            if (applyPlayerRules && !IsAllowedRespawnPoint(marker.position)) continue;
            allowedMarkers.Add(marker);
        }

        if (allowedMarkers.Count == 0) return false;

        Vector3 point = allowedMarkers[UnityEngine.Random.Range(0, allowedMarkers.Count)].position;

        // The filter overload matters: the plain area-mask overload only looks at the Humanoid NavMesh,
        // and this hospital is baked for the Hospital_Blockout agent type.
        position = NavMesh.SamplePosition(point, out NavMeshHit hit, 2f, navFilter) ? hit.position : point;
        return true;
    }

    // A respawn point must be far from the player and, when asked, out of the player's view.
    private bool IsAllowedRespawnPoint(Vector3 point)
    {
        if (player == null) return true;

        Vector3 toPlayer = player.transform.position - point;
        toPlayer.y = 0f;
        if (toPlayer.magnitude < settings.RespawnMinDistanceFromPlayer) return false;

        if (settings.RespawnAvoidPlayerView && IsInPlayerView(point)) return false;
        return true;
    }

    // True when the point is inside the player's camera view with nothing solid in between.
    private bool IsInPlayerView(Vector3 point)
    {
        if (playerCamera == null) return false;

        Vector3 target = point + Vector3.up * 1f;
        Vector3 view = playerCamera.WorldToViewportPoint(target);
        if (view.z <= 0f || view.x < 0f || view.x > 1f || view.y < 0f || view.y > 1f) return false;

        // A wall in between hides the point, so it does not count as seen.
        return !Physics.Linecast(playerCamera.transform.position, target, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
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
        SetPath(null);          // while the agent is still on, so the mover stops it cleanly
        SetState(HunterState.Dead);

        deathTime = Time.time;
        bodyHidden = false;
        nextRespawnAttempt = 0f;
        reportedWaitingToRespawn = false;

        SetCollisionActive(false);   // a dead Hunter must not block the player or soak up shots
        OnDied?.Invoke();
    }

    // While Dead: hide the body after a moment, then respawn after the delay at an allowed marker.
    private void UpdateDead()
    {
        float sinceDeath = Time.time - deathTime;

        if (!bodyHidden && sinceDeath >= settings.DeathLingerSeconds)
        {
            SetVisible(false);
            bodyHidden = true;
        }

        // A delay of 0 means the Hunter never comes back.
        if (settings.RespawnDelaySeconds <= 0f) return;
        if (sinceDeath < settings.RespawnDelaySeconds || Time.time < nextRespawnAttempt) return;

        if (TrySpawn(true)) return;

        // No marker is far enough away and out of sight right now; try again shortly.
        nextRespawnAttempt = Time.time + settings.RespawnRetrySeconds;
        if (!reportedWaitingToRespawn && settings.EnableDebugLogging)
        {
            Debug.Log("[Hunter] Waiting to respawn: no marker is far enough from the player and out of their view yet.", this);
            reportedWaitingToRespawn = true;
        }
    }

    // Colliders and the NavMeshAgent together: off while dead, back to how they were when respawned.
    private void SetCollisionActive(bool on)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = on && colliderWasOn[i];
        }

        if (agent != null) agent.enabled = on;
    }

    private void SetVisible(bool on)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].enabled = on && rendererWasOn[i];
        }
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
