using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * The real Item Spawn Director: keeps a changing supply of Medkits, Ammo Boxes and Sanity Bottles
 * on the SP_S markers of the hospital, and replaces items when they are taken.
 *
 * How it fits together:
 *  Start: places the agreed initial layout - the handoff's even split (a minSpawnDistance away from
 *         the player) - solved as a constraint problem: every planned item is a variable over the
 *         free markers of its group and no two items share a marker; stages widen the domains until
 *         a solution exists (details in PlaceHardSplit). With Hard Split off it falls back to a
 *         random fill up to Target Count. Barricaded markers bind to the nearest NavMeshObstacle
 *         that is not a Door (same rule as WaypointNode), polled live - clearing a barricade flips
 *         its markers reachable again.
 *  PickupInteractable.AnyCollected: frees that spot and queues a replacement; a stolen delivery waits
 *         Stolen Respawn Delay, a player pickup waits Respawn Delay.
 *  PickupInteractable.AnyReleased (Thief shot, item dropped): the spot is NOT freed - the dropped
 *         item still occupies it and counts toward its type's cap, so nothing duplicates.
 *  Every replacement respects the dynamic cap: a binary plateau - veryLowCount while the matching
 *         stat is below its veryLowThreshold (default 30% of its max), normalCount at or above it
 *         (default 1 and 2). The cap is read fresh at respawn time, never latched. Existing items
 *         are never despawned when a cap shrinks; caps only gate future spawns. A respawn also
 *         keeps the reachability floor: while fewer than max(1, cap-1) of the type's alive items sit
 *         on reachable (not barricaded-intact) markers, replacements go only onto reachable markers.
 *
 * It only uses PickupInteractable's and PlayerStats' public APIs, so it can replace ThiefTestSpawner
 * without any Thief changes. Drop it on an empty GameObject in your own scene copy.
 */
public class ItemSpawnDirector : MonoBehaviour
{
    // Per-type starter counts for the initial even split.
    [System.Serializable]
    private class SupplyCount
    {
        public int health = 1;
        public int ammo = 1;
        public int sanity = 1;
    }

    // Per-type binary cap: two plateaus with one threshold between them. While the matching stat
    // sits below the very-low threshold the count drops to veryLowCount ("scarce when desperate");
    // at or above the threshold it stays at normalCount. Exactly the threshold counts as normal.
    [System.Serializable]
    private class SupplyCap
    {
        [Tooltip("How many of this type may exist while the player's stat is below the very-low threshold.")]
        public int veryLowCount = 1;
        [Tooltip("How many of this type may exist once the stat is at/above the very-low threshold.")]
        public int normalCount = 2;
        [Tooltip("Fraction of the stat's max below which the count drops to the very-low count (0-1).")]
        [Range(0f, 1f)] public float veryLowThreshold = 0.3f;
    }

    [Header("Items")]
    [Tooltip("Item prefabs to spawn. Each needs a PickupInteractable; its Type decides what spawns.")]
    [SerializeField] private GameObject[] itemPrefabs;

    [Header("Markers")]
    [Tooltip("Name prefix of the spawn markers searched for in the scene.")]
    [SerializeField] private string markerPrefix = "SP_S";
    [Tooltip("Markers that share a room with a barricade. Served first at start.")]
    [SerializeField] private string[] barricadeMarkerNames = { "SP_S04", "SP_S08", "SP_S09", "SP_S10", "SP_S12" };
    [Tooltip("Metres: a barricaded marker binds to the nearest NavMeshObstacle that is not a Door within this range.")]
    [SerializeField, Min(0f)] private float barricadeSearchRadius = 5f;
    [Tooltip("Start with the even split (barricaded counts first, then ordinary). Off = random fill up to Target Count.")]
    [SerializeField] private bool useHardSplit = true;

    [Header("Initial layout")]
    [Tooltip("Fallback only (Hard Split off): how many items are placed at start.")]
    [SerializeField, Min(0)] private int targetCount = 9;
    [Tooltip("Hard Split on: items placed on barricaded markers.")]
    [SerializeField] private SupplyCount barricadedCounts = new SupplyCount { health = 1, ammo = 1, sanity = 1 };
    [Tooltip("Hard Split on: items placed on ordinary markers.")]
    [SerializeField] private SupplyCount ordinaryCounts = new SupplyCount { health = 2, ammo = 2, sanity = 2 };

    [Header("Distance from the player")]
    [Tooltip("Metres: no item spawns closer than this to the player (0 = off). Relaxes when no marker qualifies, and logs it.")]
    [SerializeField, Min(0f)] private float minSpawnDistance = 8f;

    [Header("Respawn")]
    [Tooltip("Seconds before a replacement appears after the player picks an item up.")]
    [SerializeField] private float respawnDelay = 10f;
    [Tooltip("Seconds before a replacement appears after the Thief delivers a stolen item.")]
    [SerializeField] private float stolenRespawnDelay = 20f;

    [Header("Dynamic caps (replacements only)")]
    [Tooltip("Cap replacements by how much the player needs each stat: the very-low count while the stat is below its threshold, the normal count otherwise. Off = always the normal count. The initial layout is never capped.")]
    [SerializeField] private bool useDynamicCaps = true;
    [SerializeField] private SupplyCap healthCap = new SupplyCap();
    [SerializeField] private SupplyCap ammoCap = new SupplyCap();
    [SerializeField] private SupplyCap sanityCap = new SupplyCap();

    [Header("Player")]
    [Tooltip("Used for the distance rule and the stat need. Found automatically when empty.")]
    [SerializeField] private PlayerStats player;

    [Header("Debug")]
    [SerializeField] private bool logDetails = true;

    // Runtime

    private readonly List<Transform> markers = new List<Transform>();
    private readonly List<Transform> barricadedPool = new List<Transform>();
    private readonly Dictionary<PickupInteractable, Transform> occupied = new Dictionary<PickupInteractable, Transform>();
    private readonly Dictionary<PickupType, GameObject> prefabByType = new Dictionary<PickupType, GameObject>();
    private readonly List<PickupType> supplyTypes = new List<PickupType>();
    private readonly HashSet<string> barricadeNames = new HashSet<string>();
    private readonly Dictionary<string, GameObject> barricadeBindings = new Dictionary<string, GameObject>();
    private readonly List<Transform> ordinaryPool = new List<Transform>();
    private int distanceRelaxations;
    private int solverBacktracks;
    private int solverStopStage;

    /// Items alive on their spots. Collected items are gone; dropped/carried ones still count.
    public int AliveCount(PickupType type)
    {
        int count = 0;
        foreach (PickupInteractable pickup in occupied.Keys)
            if (pickup != null && pickup.Type == type) count++;
        return count;
    }

    /// The cap in force right now (dynamic curve, or the per-type max when the feature is off).
    public int CurrentCap(PickupType type) => ComputeCap(type);

    /// Spots with no item on them.
    public int FreeMarkerCount
    {
        get
        {
            int free = 0;
            foreach (Transform m in markers)
                if (!occupied.ContainsValue(m)) free++;
            return free;
        }
    }

    /// True while this item holds its spot (spawned, carried, or dropped - but not yet collected).
    public bool IsReserved(PickupInteractable pickup) => occupied.ContainsKey(pickup);

    /// True when an item may be placed here: not sealed behind an intact barricade. Barricaded
    /// labels whose barricade cannot be observed count as sealed (conservative), everything else
    /// is open. Polled live, so a cleared barricade flips this to true.
    public bool IsReachableMarker(string markerName)
    {
        if (!barricadeNames.Contains(markerName)) return true;
        return !BarricadeIntact(markerName);
    }

    /// The GameObject holding this labelled marker's bound barricade, or null when unbound.
    public GameObject BoundBarricadeOf(string markerName)
        => barricadeBindings.TryGetValue(markerName, out GameObject barricade) ? barricade : null;

    private bool DistanceEnabled => minSpawnDistance > 0f && player != null;

    private void OnEnable()
    {
        PickupInteractable.AnyCollected += OnItemCollected;
        PickupInteractable.AnyReleased += OnItemReleased;
    }

    private void OnDisable()
    {
        PickupInteractable.AnyCollected -= OnItemCollected;
        PickupInteractable.AnyReleased -= OnItemReleased;
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerStats>();

        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            if (t.name.StartsWith(markerPrefix)) markers.Add(t);

        foreach (string name in barricadeMarkerNames)
            barricadeNames.Add(name);
        foreach (Transform m in markers)
            if (barricadeNames.Contains(m.name)) barricadedPool.Add(m);
            else ordinaryPool.Add(m);

        BindBarricades();
        if (logDetails)
            foreach (string label in barricadeNames)
                Log($"barricade binding: {label} -> " +
                    (barricadeBindings.TryGetValue(label, out GameObject bound) ? bound.name : "none (counts as intact)"));

        if (itemPrefabs != null)
        {
            foreach (GameObject prefab in itemPrefabs)
            {
                if (prefab == null) continue;
                PickupInteractable pickup = prefab.GetComponent<PickupInteractable>();
                if (pickup == null)
                {
                    Debug.LogWarning($"ItemSpawnDirector: prefab '{prefab.name}' has no PickupInteractable and is skipped.", this);
                    continue;
                }
                if (prefabByType.ContainsKey(pickup.Type))
                {
                    Debug.LogWarning($"ItemSpawnDirector: two prefabs provide '{pickup.Type}'; the first one wins.", this);
                    continue;
                }
                prefabByType[pickup.Type] = prefab;
                supplyTypes.Add(pickup.Type);
            }
        }

        if (markers.Count == 0 || prefabByType.Count == 0)
        {
            Debug.LogWarning($"ItemSpawnDirector: found {markers.Count} '{markerPrefix}' markers and " +
                             $"{prefabByType.Count} usable prefabs, so nothing spawns.", this);
            return;
        }

        if (minSpawnDistance > 0f && player == null)
            Debug.LogWarning($"ItemSpawnDirector: minSpawnDistance is {minSpawnDistance}m but no PlayerStats " +
                             "was found, so the distance rule is off.", this);

        int placed = useHardSplit ? PlaceHardSplit() : PlaceRandomFill();

        if (logDetails)
            Log($"start: placed {placed}/{markerSummary()} items ({CapSummary(true)}), " +
                $"{FreeMarkerCount} markers free, distance relaxations: {distanceRelaxations}" +
                (useHardSplit ? $", CSP: stage {solverStopStage}/5, {solverBacktracks} backtracks" : ""));
    }

    // Initial layout: the hard split solved as a constraint problem. Every planned item is a
    // variable, its domain is the free markers it may legally use (its pool, minus markers too
    // close to the player) and no two items may share a marker; SpawnCspSolver searches for a
    // full assignment. Stages widen the domains until one exists:
    //   1 pool + distance, 2 pool only, 3 barricaded items may spill onto ordinary markers
    //   (distance back on), 4 anywhere free, 5 per-item placement as before.
    private int PlaceHardSplit()
    {
        List<PickupType> barricadedPlan = BuildPlan(barricadedCounts);
        List<PickupType> ordinaryPlan = BuildPlan(ordinaryCounts);
        Shuffle(barricadedPlan);
        Shuffle(ordinaryPlan);

        var ordinaryPool = new List<Transform>();
        foreach (Transform m in markers)
            if (!barricadeNames.Contains(m.name)) ordinaryPool.Add(m);

        for (int stage = 1; stage <= 4; stage++)
        {
            bool relaxDistance = stage == 2 || stage == 4;
            bool barricadeSpill = stage == 3 || stage == 4;

            var slotTypes = new List<PickupType>();
            var variables = new List<SpawnCspSolver.Variable>();

            for (int i = 0; i < barricadedPlan.Count; i++)
            {
                List<Transform> domain = DomainFor(barricadeSpill ? markers : barricadedPool, relaxDistance);
                variables.Add(new SpawnCspSolver.Variable($"barricaded {i} ({barricadedPlan[i]})", domain));
                slotTypes.Add(barricadedPlan[i]);
            }
            for (int i = 0; i < ordinaryPlan.Count; i++)
            {
                List<Transform> domain = DomainFor(ordinaryPool, relaxDistance);
                variables.Add(new SpawnCspSolver.Variable($"ordinary {i} ({ordinaryPlan[i]})", domain));
                slotTypes.Add(ordinaryPlan[i]);
            }

            if (stage > 1)
                Log($"initial layout: relaxing to stage {stage} ({(barricadeSpill ? "barricaded items may spill, " : "")}" +
                    $"{(relaxDistance ? "distance relaxed" : "pools only")}).");

            if (!SpawnCspSolver.Solve(variables, out SpawnCspSolver.Stats stats))
            {
                solverBacktracks += stats.Backtracks;
                continue;
            }

            solverBacktracks += stats.Backtracks;
            solverStopStage = stage;
            int placedStage = 0;
            for (int i = 0; i < variables.Count; i++)
                if (variables[i].Assignment != null && Spawn(slotTypes[i], variables[i].Assignment))
                    placedStage++;
            if (stage > 1)
                Log($"initial layout: stage {stage} placed all {placedStage} items.");
            return placedStage;
        }

        // Stage 5: no relaxation gave the solver an assignment - the old per-item placement.
        solverStopStage = 5;
        Log("initial layout: no solver stage found an assignment - falling back to per-item placement.");
        int placed = 0;
        foreach (PickupType type in barricadedPlan)
            placed += TryPlace(type, barricadedPool) ? 1 : 0;
        foreach (PickupType type in ordinaryPlan)
            placed += TryPlace(type, markers) ? 1 : 0;
        return placed;
    }

    // Free markers of the pool that satisfy the distance rule (unless it is relaxed).
    private List<Transform> DomainFor(List<Transform> pool, bool ignoreDistance)
    {
        var domain = new List<Transform>();
        foreach (Transform m in pool)
            if (!occupied.ContainsValue(m) && (ignoreDistance || PassesDistance(m))) domain.Add(m);
        return domain;
    }

    // Fallback layout: random types on random free markers, up to Target Count and the per-type max.
    private int PlaceRandomFill()
    {
        int placed = 0;
        for (int i = 0; i < targetCount; i++)
        {
            List<PickupType> eligible = new List<PickupType>();
            foreach (PickupType t in supplyTypes)
                if (AliveCount(t) < RangeFor(t).normalCount) eligible.Add(t);

            if (eligible.Count == 0)
            {
                Log($"initial fill stopped at {placed}: every type is at its normal count.");
                break;
            }

            Transform marker = PickMarker(markers);
            if (marker == null)
            {
                Log($"initial fill stopped at {placed}: no free marker left.");
                break;
            }

            Spawn(eligible[Random.Range(0, eligible.Count)], marker);
            placed++;
        }
        return placed;
    }

    private List<PickupType> BuildPlan(SupplyCount counts)
    {
        var plan = new List<PickupType>();
        foreach (PickupType t in supplyTypes)
        {
            int n = CountFor(counts, t);
            for (int i = 0; i < n; i++) plan.Add(t);
        }
        return plan;
    }

    // Places one item of the given type, preferring the given pool; falls back to any free marker.
    private bool TryPlace(PickupType type, List<Transform> pool)
    {
        Transform marker = PickMarker(pool);
        if (marker == null)
        {
            marker = PickMarker(markers);
            if (marker == null)
            {
                Log($"cannot place {type}: no free marker anywhere.");
                return false;
            }
            Log($"no free marker left in its category for {type} - placed on a general marker instead.");
        }
        Spawn(type, marker);
        return true;
    }

    // Picks a free marker from the pool, honouring the distance rule, relaxing it once if needed.
    private Transform PickMarker(List<Transform> pool)
    {
        var candidates = new List<Transform>();
        foreach (Transform m in pool)
            if (!occupied.ContainsValue(m) && PassesDistance(m)) candidates.Add(m);

        if (candidates.Count > 0) return candidates[Random.Range(0, candidates.Count)];

        if (!DistanceEnabled) return null; // pool really is exhausted

        // Relax: closer-than-minDistance markers are better than no marker at all.
        foreach (Transform m in pool)
            if (!occupied.ContainsValue(m)) candidates.Add(m);

        if (candidates.Count == 0) return null;

        distanceRelaxations++;
        Log($"no marker is {minSpawnDistance}m away from the player - relaxing the distance rule for this spawn.");
        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool PassesDistance(Transform marker)
        => !DistanceEnabled || Vector3.Distance(marker.position, player.transform.position) >= minSpawnDistance;

    private bool Spawn(PickupType type, Transform marker)
    {
        GameObject prefab = prefabByType[type];
        GameObject item = Instantiate(prefab, marker.position, prefab.transform.rotation, transform);
        PickupInteractable pickup = item.GetComponent<PickupInteractable>();
        if (pickup == null)
        {
            Debug.LogWarning($"ItemSpawnDirector: prefab '{prefab.name}' lost its PickupInteractable.", this);
            Destroy(item);
            return false;
        }

        occupied[pickup] = marker;
        if (logDetails)
            Log($"spawned {type} at {marker.name}{DistanceText(marker)} - alive: {CapSummary()}");
        return true;
    }

    // Replacements

    // The spot is freed only here; AnyReleased (Thief dropped the item) deliberately does nothing.
    private void OnItemCollected(PickupInteractable pickup)
    {
        if (!occupied.Remove(pickup)) return; // an item this director did not place

        float delay = pickup.WasStolen ? stolenRespawnDelay : respawnDelay;
        if (logDetails)
            Log($"'{pickup.name}' {(pickup.WasStolen ? "stolen and stashed" : "collected")} - spot freed, " +
                $"replacement in {delay:0.#}s");
        StartCoroutine(RespawnAfter(delay));
    }

    private void OnItemReleased(PickupInteractable pickup)
    {
        if (occupied.ContainsKey(pickup) && logDetails)
            Log($"'{pickup.name}' dropped back into the world - its spot stays reserved, no replacement queued");
    }

    private IEnumerator RespawnAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        TryRespawn();
    }

    // One replacement: a random type that is under its current cap, on a marker from that type's
    // floor-aware pool. Type first, then floor, then marker - the floor depends on the chosen
    // type's cap, which is read here, at fire time.
    private void TryRespawn()
    {
        var eligible = new List<PickupType>();
        foreach (PickupType t in supplyTypes)
            if (AliveCount(t) < ComputeCap(t)) eligible.Add(t);

        if (eligible.Count == 0)
        {
            Log($"replacement skipped: all types at cap ({CapSummary()}) - the next collection retries.");
            return;
        }

        PickupType type = eligible[Random.Range(0, eligible.Count)];
        List<Transform> pool = ChooseRespawnPool(type);

        Transform marker = PickMarker(pool);
        if (marker == null && !ReferenceEquals(pool, markers))
        {
            marker = PickMarker(markers); // last resort: any free marker
            if (marker != null)
                Log($"respawn floor: no marker in the floor pool worked for {type} - falling back to every free marker.");
        }
        if (marker == null)
        {
            Log("replacement skipped: no free marker.");
            return;
        }

        Spawn(type, marker);
    }

    // Dynamic caps

    private int ComputeCap(PickupType type)
    {
        SupplyCap range = RangeFor(type);
        if (range == null) return 0;
        if (!useDynamicCaps || player == null) return range.normalCount;

        float frac;
        switch (type)
        {
            case PickupType.Health:
                frac = player.Health / Mathf.Max(1f, player.MaxHealth);
                break;
            case PickupType.Ammo:
                frac = player.ReserveAmmo / Mathf.Max(1f, player.MaxReserveAmmo);
                break;
            case PickupType.Sanity:
                frac = player.Sanity / Mathf.Max(1f, player.MaxSanity);
                break;
            default:
                return range.normalCount;
        }

        // Binary plateau: scarce (veryLowCount) only while the stat is below the threshold;
        // normal (normalCount) at or above it. Exactly the threshold counts as NOT very low.
        return frac < range.veryLowThreshold ? range.veryLowCount : range.normalCount;
    }

    private SupplyCap RangeFor(PickupType type)
    {
        switch (type)
        {
            case PickupType.Health: return healthCap;
            case PickupType.Ammo: return ammoCap;
            case PickupType.Sanity: return sanityCap;
            default: return null; // ExitKey is not this director's business
        }
    }

    private int CountFor(SupplyCount counts, PickupType type)
    {
        switch (type)
        {
            case PickupType.Health: return Mathf.Max(0, counts.health);
            case PickupType.Ammo: return Mathf.Max(0, counts.ammo);
            case PickupType.Sanity: return Mathf.Max(0, counts.sanity);
            default: return 0;
        }
    }

    // Helpers

    // The replacement's marker pool for this type. The reachability floor is max(1, cap-1): at
    // least cap-1 of the type's alive items must sit on reachable markers, and never fewer than
    // one - so at cap 1 the single slot is forced onto a reachable marker. While that floor is
    // broken, only reachable markers qualify. The cap is read now, at fire time - it is never
    // latched from an earlier moment. With the floor met (or impossible to meet), every free
    // marker is legal.
    private List<Transform> ChooseRespawnPool(PickupType type)
    {
        int floor = Mathf.Max(1, ComputeCap(type) - 1);
        int reachableAlive = AliveOnReachable(type);
        if (reachableAlive >= floor) return FreeMarkers(markers);

        var reachable = new List<Transform>();
        foreach (Transform m in markers)
            if (!occupied.ContainsValue(m) && IsReachableMarker(m.name)) reachable.Add(m);

        if (reachable.Count > 0)
        {
            Log($"respawn floor: only {reachableAlive} {type} on reachable markers (floor {floor}) - " +
                "respawning into the reachable pool only.");
            return reachable;
        }

        Log("respawn floor: no reachable marker is free - falling back to every free marker.");
        return FreeMarkers(markers);
    }

    // How many of this type's alive items sit on markers that are not behind an intact barricade.
    private int AliveOnReachable(PickupType type)
    {
        int count = 0;
        foreach (KeyValuePair<PickupInteractable, Transform> spot in occupied)
            if (spot.Key != null && spot.Key.Type == type && IsReachableMarker(spot.Value.name)) count++;
        return count;
    }

    private List<Transform> FreeMarkers(List<Transform> pool)
    {
        var free = new List<Transform>();
        foreach (Transform m in pool)
            if (!occupied.ContainsValue(m)) free.Add(m);
        return free;
    }

    // Binds every labelled barricaded marker to the nearest NavMeshObstacle GameObject that is not
    // part of a Door (doors carry obstacles too - WaypointNode excludes them the same way), within
    // the search radius. Unbound labels stay conservative: treated as intact until proven open.
    private void BindBarricades()
    {
        var obstacles = FindObjectsByType<UnityEngine.AI.NavMeshObstacle>(FindObjectsInactive.Exclude);
        foreach (Transform m in markers)
        {
            if (!barricadeNames.Contains(m.name)) continue;

            UnityEngine.AI.NavMeshObstacle best = null;
            float bestDistance = barricadeSearchRadius;
            foreach (UnityEngine.AI.NavMeshObstacle obstacle in obstacles)
            {
                if (obstacle.GetComponentInParent<Door>() != null) continue;
                float d = Vector3.Distance(m.position, obstacle.transform.position);
                if (d <= bestDistance) { bestDistance = d; best = obstacle; }
            }
            if (best != null) barricadeBindings[m.name] = best.gameObject;
        }
    }

    // The same intact rule as WaypointNode: exists, active, and still blocking the NavMesh. A
    // label with no observable barricade counts as intact - a room we cannot prove is open.
    private bool BarricadeIntact(string markerName)
    {
        if (!barricadeBindings.TryGetValue(markerName, out GameObject barricade) || barricade == null)
            return true;
        var obstacle = barricade.GetComponent<UnityEngine.AI.NavMeshObstacle>();
        return barricade.activeInHierarchy && obstacle != null && obstacle.enabled;
    }

    private string CapSummary(bool includeCap = false)
    {
        var parts = new List<string>();
        foreach (PickupType t in supplyTypes)
            parts.Add(includeCap
                ? $"{t} {AliveCount(t)}/{ComputeCap(t)}"
                : $"{t} {AliveCount(t)}");
        return string.Join(", ", parts);
    }

    private string markerSummary() => $"{occupied.Count} of {markers.Count}";

    private string DistanceText(Transform marker)
        => DistanceEnabled ? $" ({Vector3.Distance(marker.position, player.transform.position):0.0}m from player)" : string.Empty;

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void Log(string message) => Debug.Log($"[ItemSpawnDirector] {message}", this);

    private void OnValidate()
    {
        ClampCap(healthCap);
        ClampCap(ammoCap);
        ClampCap(sanityCap);
        barricadedCounts.health = Mathf.Max(0, barricadedCounts.health);
        barricadedCounts.ammo = Mathf.Max(0, barricadedCounts.ammo);
        barricadedCounts.sanity = Mathf.Max(0, barricadedCounts.sanity);
        ordinaryCounts.health = Mathf.Max(0, ordinaryCounts.health);
        ordinaryCounts.ammo = Mathf.Max(0, ordinaryCounts.ammo);
        ordinaryCounts.sanity = Mathf.Max(0, ordinaryCounts.sanity);
    }

    private static void ClampCap(SupplyCap cap)
    {
        if (cap == null) return;
        cap.normalCount = Mathf.Max(0, cap.normalCount);
        cap.veryLowCount = Mathf.Clamp(cap.veryLowCount, 0, cap.normalCount);
        cap.veryLowThreshold = Mathf.Clamp01(cap.veryLowThreshold);
    }

    // Right-click the component header in the Inspector during play mode.

    [ContextMenu("Debug: Dump State")]
    private void DebugDumpState()
    {
        Log($"markers: {markers.Count} total, {barricadedPool.Count} barricaded, {FreeMarkerCount} free | " +
            $"distance rule: {(DistanceEnabled ? $"min {minSpawnDistance}m" : "off")} | " +
            $"player: {(player != null ? player.name : "none")}");
        foreach (PickupType t in supplyTypes)
            Log($"  {t}: alive {AliveCount(t)}, cap {ComputeCap(t)}");
        foreach (KeyValuePair<PickupInteractable, Transform> kv in occupied)
            if (kv.Key != null)
                Log($"  reserved: {kv.Key.name} @ {kv.Value.name} " +
                    $"({(IsReachableMarker(kv.Value.name) ? "reachable" : "barricaded")})");
        foreach (string label in barricadeNames)
            Log($"  barricade: {label} -> " +
                (barricadeBindings.TryGetValue(label, out GameObject bound)
                    ? $"{bound.name} ({(BarricadeIntact(label) ? "intact" : "down")})"
                    : "unbound (intact by rule)"));
    }
}
