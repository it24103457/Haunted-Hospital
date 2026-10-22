using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * The Sound Hunter's decision maker. It spawns, patrols, takes damage and respawns out of the player's sight.
 * It listens through HunterEars: a noise worth following sends it to Investigate, and when it gets there it
 * Searches the area (HunterSearch picks the points) before going back to Patrol. A loud player noise, being shot,
 * or the player being close by path starts a Chase; within reach it Attacks.
 * It never moves itself: it publishes CurrentPath, State, SpeedMultiplier and LookTarget, and a mover
 * (HunterTestMover for now) acts on them. Other scripts only read it, through IHunterStatus.
 * All walking, and every question about what is reachable, goes through HunterNavigator.
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

    // The tuning numbers, shared with HunterEars so they are only assigned once.
    public HunterSettings Settings => settings;

    /// <summary>Walks routes (doors included). Used by Patrol, Investigate and Search, and by Chase later.</summary>
    public HunterNavigator Navigator => navigator;

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
    private NavMeshAgent agent;
    private NavMeshQueryFilter navFilter;
    private HunterNavigator navigator;
    private HunterEars ears;

    // Investigate and Search
    private int investigateTargetId = -1;
    private HunterSearch search;
    private float searchEndTime;
    private float searchWaitUntil;
    private bool hasSearchPoint;

    // Chase and Attack
    private Vector3 lastKnownPlayerPos;
    private float lastSignalTime;
    private float shotAlertUntil;
    private float nextSenseTime;
    private float lastReachTime = float.NegativeInfinity;
    private Vector3 chaseDestination;
    private bool hasChaseDestination;
    private float nextChaseRepath;
    private float nextAttackTime;
    private readonly RaycastHit[] reachHits = new RaycastHit[8];

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
        ears = GetComponent<HunterEars>();

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

        if (IsReady)
        {
            navigator = new HunterNavigator(settings, graph, transform, gameObject, SetPath);
            search = new HunterSearch(navigator, settings);
        }

        if (ears != null) ears.NoiseHeard += OnNoiseHeard;

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

        if (IsActive) navigator.Tick();

        if (IsActive && Time.time >= nextSenseTime)
        {
            nextSenseTime = Time.time + settings.SenseIntervalSeconds;
            SensePlayer();
        }

        switch (State)
        {
            case HunterState.Patrol:
                UpdatePatrol();
                break;
            case HunterState.Investigate:
                UpdateInvestigate();
                break;
            case HunterState.Search:
                UpdateSearch();
                break;
            case HunterState.Chase:
                UpdateChase();
                break;
            case HunterState.Attack:
                UpdateAttack();
                break;
            case HunterState.Dead:
                UpdateDead();
                break;
        }
    }

    private void OnDestroy()
    {
        if (ears != null) ears.NoiseHeard -= OnNoiseHeard;
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

        navigator.Stop();
        investigateTargetId = -1;
        hasChaseDestination = false;
        lastReachTime = float.NegativeInfinity;
        shotAlertUntil = 0f;
        LookTarget = null;
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
        if (TryStartInvestigating()) return;

        if (!hasPatrolDestination)
        {
            if (Time.time >= patrolWaitUntil) PickPatrolDestination();
            return;
        }

        switch (navigator.Status)
        {
            case HunterNavStatus.Arrived:
                // Arrived: stand still for a moment, then choose the next point.
                hasPatrolDestination = false;
                patrolWaitUntil = Time.time + settings.PatrolWaitSeconds;
                break;

            case HunterNavStatus.Failed:
            case HunterNavStatus.Idle:
                // The way is blocked (a door that will not open, say): choose another point.
                hasPatrolDestination = false;
                patrolWaitUntil = Time.time;
                break;

            default:
                if (Time.time - patrolLegStart > settings.PatrolLegTimeoutSeconds)
                {
                    navigator.Stop();
                    hasPatrolDestination = false;
                    patrolWaitUntil = Time.time;
                }
                break;
        }
    }

    // Picks a random Normal node that is far enough away and that the navigator can find a full route to.
    // The candidates come from one flood fill of what the Hunter can really reach from here, so a Hunter shut in by a
    // locked door or barricade chooses among the nodes inside its area instead of mostly missing on unreachable ones.
    private void PickPatrolDestination()
    {
        List<WaypointNode> reachable = navigator.NodesReachable(transform.position);

        candidates.Clear();
        float minDistance = settings.PatrolMinLegDistance;
        WaypointNode farthest = null;
        float farthestDistance = 0f;

        foreach (WaypointNode node in reachable)
        {
            if (node == null || node.Type != WaypointType.Normal) continue;

            Vector3 offset = node.Position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;

            if (distance >= minDistance) candidates.Add(node);
            if (distance > farthestDistance)
            {
                farthestDistance = distance;
                farthest = node;
            }
        }

        // A small closed-in area has nothing far enough away: walk to its far end instead of standing still.
        if (candidates.Count == 0 && farthest != null && farthestDistance > settings.ArrivalRadius)
            candidates.Add(farthest);

        int totalCandidates = candidates.Count;

        // Try a handful of random candidates (a banned door can still make one of them unreachable for a moment).
        for (int attempt = 0; attempt < 12 && candidates.Count > 0; attempt++)
        {
            int i = UnityEngine.Random.Range(0, candidates.Count);
            WaypointNode node = candidates[i];
            candidates.RemoveAt(i);

            // Patrol only goes where it can really get to, so no partial routes.
            if (!navigator.GoTo(node.Position, false)) continue;

            hasPatrolDestination = true;
            patrolLegStart = Time.time;
            return;
        }

        // Nothing reachable right now (doors locked, no far node). Try again in a moment, and say so only once.
        if (!reportedNoPatrolPoint && settings.EnableDebugLogging)
        {
            Debug.Log($"[Hunter] No reachable patrol point found; will keep trying. Reachable nodes: {reachable.Count}, candidates: {totalCandidates}, " +
                      $"position: {transform.position}.", this);
            reportedNoPatrolPoint = true;
        }
        patrolWaitUntil = Time.time + 1f;
    }

    // Investigate and Search

    // Patrol -> Investigate when the ears have a noise strong enough to go and look at.
    private bool TryStartInvestigating()
    {
        return ears != null && ears.WantsToInvestigate && BeginInvestigate(ears.Target);
    }

    // Walks to where the noise came from. If it cannot be reached, the Hunter goes as near as the building allows.
    private bool BeginInvestigate(HeardNoise noise)
    {
        if (!navigator.GoTo(noise.Position, true))
        {
            ears.Forget(noise.Id);   // no node anywhere near it, so there is nothing to walk to
            return false;
        }

        investigateTargetId = noise.Id;
        hasPatrolDestination = false;

        if (settings.EnableDebugLogging)
            Debug.Log($"[Hunter] Investigating {noise.Type} (score {ears.ScoreOf(noise):0.#})" +
                      $"{(navigator.IsPartialRoute ? ", can only get near it" : "")}.", this);

        SetState(HunterState.Investigate);
        return true;
    }

    private void UpdateInvestigate()
    {
        // A clearly better noise (the ears only change target once it beats the current one by the margin).
        if (ears != null && ears.HasTarget && ears.Target.Id != investigateTargetId && ears.WantsToInvestigate)
        {
            if (BeginInvestigate(ears.Target)) return;
        }

        switch (navigator.Status)
        {
            case HunterNavStatus.Arrived:
                ForgetInvestigated();      // so the same noise is not investigated twice
                BeginSearch(transform.position);
                break;

            case HunterNavStatus.Failed:
            case HunterNavStatus.Idle:
                ForgetInvestigated();
                EnterPatrol();
                break;
        }
    }

    private void ForgetInvestigated()
    {
        if (ears != null && investigateTargetId >= 0) ears.Forget(investigateTargetId);
        investigateTargetId = -1;
    }

    // Looks around the place it just reached: visits a few nearby points for a while, then goes back to patrolling.
    private void BeginSearch(Vector3 center)
    {
        search.Begin(center);
        searchEndTime = Time.time + settings.SearchSeconds;
        searchWaitUntil = Time.time + settings.SearchPauseSeconds;
        hasSearchPoint = false;

        navigator.Stop();

        if (settings.EnableDebugLogging) Debug.Log("[Hunter] Searching around here.", this);
        SetState(HunterState.Search);
    }

    private void UpdateSearch()
    {
        // A new noise interrupts the search.
        if (TryStartInvestigating()) return;

        if (Time.time >= searchEndTime)
        {
            if (settings.EnableDebugLogging) Debug.Log("[Hunter] Search finished, back to patrol.", this);
            EnterPatrol();
            return;
        }

        if (!hasSearchPoint)
        {
            if (Time.time >= searchWaitUntil) PickSearchPoint();
            return;
        }

        switch (navigator.Status)
        {
            case HunterNavStatus.Arrived:
                hasSearchPoint = false;
                searchWaitUntil = Time.time + settings.SearchPauseSeconds;
                break;

            case HunterNavStatus.Failed:
            case HunterNavStatus.Idle:
                hasSearchPoint = false;
                searchWaitUntil = Time.time + 0.5f;
                break;
        }
    }

    // Walks to the next unvisited search point (HunterSearch knows which are within the radius by path).
    private void PickSearchPoint()
    {
        // Try the next point if the Hunter cannot get to one right now.
        while (search.TryPickNext(out Vector3 point))
        {
            if (!navigator.GoTo(point, false)) continue;

            hasSearchPoint = true;
            return;
        }

        // Every nearby point has been looked at (or there were none, in a small sealed room): the search is over.
        if (settings.EnableDebugLogging) Debug.Log("[Hunter] Nowhere left to look, back to patrol.", this);
        EnterPatrol();
    }

    private void EnterPatrol()
    {
        navigator.Stop();
        hasPatrolDestination = false;
        patrolWaitUntil = Time.time;
        SetState(HunterState.Patrol);
    }

    // Chase and Attack

    // The ears heard a noise. A loud one made by the player starts a Chase; during a Chase any player noise refreshes the trail.
    private void OnNoiseHeard(HeardNoise noise)
    {
        if (!IsActive || !IsPlayerSource(noise.Source)) return;

        if (State == HunterState.Chase)
            MarkPlayerSignal(noise.Position);
        else if (State != HunterState.Attack && noise.HeardLoudness >= settings.ChaseLoudness)
            BeginChase(noise.Position);
    }

    private bool IsPlayerSource(GameObject source)
    {
        return source != null && source.GetComponentInParent<PlayerStats>() != null;
    }

    // Shot by the player: head for where the shot came from, and stay on alert for a while.
    private void AlertToShooter(DamageInfo info)
    {
        if (!IsPlayerSource(info.Source)) return;

        shotAlertUntil = Time.time + settings.ShotAlertSeconds;

        if (State == HunterState.Chase) MarkPlayerSignal(info.Source.transform.position);
        else if (State != HunterState.Attack) BeginChase(info.Source.transform.position);
    }

    // Where the player was last known to be, and when. The Chase ends after a stretch of silence.
    private void MarkPlayerSignal(Vector3 position)
    {
        lastKnownPlayerPos = position;
        lastSignalTime = Time.time;
    }

    private void BeginChase(Vector3 knownPosition)
    {
        MarkPlayerSignal(knownPosition);

        // It is hunting the player directly now, so older noises no longer matter.
        if (ears != null) ears.ClearMemory();

        investigateTargetId = -1;
        hasPatrolDestination = false;
        hasChaseDestination = false;
        nextChaseRepath = 0f;
        navigator.Stop();

        if (settings.EnableDebugLogging) Debug.Log("[Hunter] Chasing the player.", this);
        SetState(HunterState.Chase);
    }

    // Every sense interval: is the player within reach, or close by path? (Hearing is separate: HunterEars.)
    private void SensePlayer()
    {
        if (player == null || player.IsDead) return;

        // Once attacking, the player has to get a little further away to break it off (hysteresis).
        float range = State == HunterState.Attack ? settings.AttackExitRange : settings.AttackRange;
        bool reach = CanReachPlayerNow(range);
        if (reach) lastReachTime = Time.time;

        bool near = reach || IsPlayerNearByPath();
        Vector3 position = player.transform.position;

        switch (State)
        {
            case HunterState.Patrol:
            case HunterState.Investigate:
            case HunterState.Search:
                if (near) BeginChase(position);
                break;

            case HunterState.Chase:
                if (near) MarkPlayerSignal(position);
                break;
        }
    }

    // Close by PATH, not just in a straight line: someone on the far side of a wall is not near.
    private bool IsPlayerNearByPath()
    {
        float limit = settings.ChaseProximity;
        if (limit <= 0f) return false;

        Vector3 offset = player.transform.position - transform.position;
        offset.y = 0f;
        if (offset.magnitude > limit) return false;   // a path is never shorter than a straight line

        return navigator.TryGetPathCost(player.transform.position, out float cost) && cost <= limit;
    }

    // True when the player is close enough to hit, at about the same height, with nothing solid between us.
    private bool CanReachPlayerNow(float range)
    {
        Vector3 from = transform.position + Vector3.up;
        Vector3 to = player.transform.position + Vector3.up;
        Vector3 delta = to - from;
        if (delta.sqrMagnitude < 0.0001f) return true;

        Vector3 flat = delta;
        flat.y = 0f;
        if (flat.magnitude > range || Mathf.Abs(delta.y) > range) return false;

        // The nearest thing in the way must be the player: a wall (or a vent grille) means no reach.
        int count = Physics.RaycastNonAlloc(from, delta.normalized, reachHits, delta.magnitude,
                                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        bool nearestIsPlayer = true;

        for (int i = 0; i < count; i++)
        {
            Collider hit = reachHits[i].collider;
            if (hit.transform.IsChildOf(transform)) continue;            // the Hunter's own body
            if (reachHits[i].distance >= nearest) continue;

            nearest = reachHits[i].distance;
            nearestIsPlayer = hit.GetComponentInParent<PlayerStats>() != null;
        }

        return nearestIsPlayer;
    }

    // The player's position was confirmed within reach recently enough to count.
    private bool HasFreshReach => Time.time - lastReachTime <= settings.FreshReachSeconds;

    private void UpdateChase()
    {
        if (player != null && player.IsDead)
        {
            EnterPatrol();
            return;
        }

        // Silence for long enough (and not recently shot): lose the trail and search where the player was last known.
        bool onAlert = Time.time < shotAlertUntil;
        if (!onAlert && Time.time - lastSignalTime > settings.ChaseLoseSeconds)
        {
            if (settings.EnableDebugLogging) Debug.Log("[Hunter] Lost the player; searching where they were last known.", this);
            BeginSearch(lastKnownPlayerPos);
            return;
        }

        if (player != null && HasFreshReach)
        {
            BeginAttack();
            return;
        }

        FollowTrail();
    }

    // Walks toward where the player was last known, planning again when that spot moves or the last attempt failed.
    private void FollowTrail()
    {
        if (navigator.Status == HunterNavStatus.OpeningDoor) return;   // never interrupt a door

        bool moved = !hasChaseDestination ||
                     Vector3.Distance(chaseDestination, lastKnownPlayerPos) > settings.ArrivalRadius;
        if (!moved && navigator.Status != HunterNavStatus.Failed) return;
        if (Time.time < nextChaseRepath) return;

        nextChaseRepath = Time.time + settings.ReplanIntervalSeconds;

        if (!navigator.GoTo(lastKnownPlayerPos, true)) return;

        chaseDestination = lastKnownPlayerPos;
        hasChaseDestination = true;
    }

    private void BeginAttack()
    {
        navigator.Stop();
        hasChaseDestination = false;
        nextAttackTime = Time.time + settings.AttackWindupSeconds;
        LookTarget = player.transform.position;

        if (settings.EnableDebugLogging) Debug.Log("[Hunter] Attacking the player.", this);
        SetState(HunterState.Attack);
    }

    private void UpdateAttack()
    {
        if (player == null || player.IsDead)
        {
            LookTarget = null;
            EnterPatrol();
            return;
        }

        // The player got out of reach (or behind something): give chase again.
        if (!HasFreshReach)
        {
            LookTarget = null;
            MarkPlayerSignal(player.transform.position);
            hasChaseDestination = false;
            SetState(HunterState.Chase);
            return;
        }

        LookTarget = player.transform.position;
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + settings.AttackCooldownSeconds;
        player.TakeDamage(new DamageInfo(settings.AttackDamage, gameObject, player.transform.position));
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
        else AlertToShooter(info);
    }

    private void Die()
    {
        hasPatrolDestination = false;
        LookTarget = null;
        navigator.Stop();
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
