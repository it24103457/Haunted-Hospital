using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * The real Item Spawn Director: keeps a changing supply of Medkits, Ammo Boxes and Sanity Bottles
 * on the SP_S markers of the hospital, and replaces items when they are taken.
 *
 * How it fits together:
 *  Start: places the agreed initial layout - the handoff's even split (a minSpawnDistance away from
 *         the player), serving barricaded markers first, then ordinary ones. With Hard Split off it
 *         falls back to a random fill up to Target Count.
 *  PickupInteractable.AnyCollected: frees that spot and queues a replacement; a stolen delivery waits
 *         Stolen Respawn Delay, a player pickup waits Respawn Delay.
 *  PickupInteractable.AnyReleased (Thief shot, item dropped): the spot is NOT freed - the dropped
 *         item still occupies it and counts toward its type's cap, so nothing duplicates.
 *  Every replacement respects the dynamic cap: cap(type) = Lerp(min, max, need), where need is how
 *         far the player is from full health / sanity / reserve ammo. Existing items are never
 *         despawned when a cap shrinks; caps only gate future spawns.
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

    // Per-type dynamic cap range (min at full stat, max at empty stat).
    [System.Serializable]
    private class SupplyCap
    {
        [Tooltip("How many of this type may exist even when the player is at full.")]
        public int min = 1;
        [Tooltip("How many of this type may exist when the player needs it most.")]
        public int max = 3;
    }

    [Header("Items")]
    [Tooltip("Item prefabs to spawn. Each needs a PickupInteractable; its Type decides what spawns.")]
    [SerializeField] private GameObject[] itemPrefabs;

    [Header("Markers")]
    [Tooltip("Name prefix of the spawn markers searched for in the scene.")]
    [SerializeField] private string markerPrefix = "SP_S";
    [Tooltip("Markers that share a room with a barricade. Served first at start.")]
    [SerializeField] private string[] barricadeMarkerNames = { "SP_S04", "SP_S08", "SP_S09", "SP_S10", "SP_S12" };
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
    [Tooltip("Cap replacements by how much the player needs each stat. Off = always the per-type max. The initial layout is never capped.")]
    [SerializeField] private bool useDynamicCaps = true;
    [SerializeField] private SupplyCap healthCap = new SupplyCap { min = 1, max = 3 };
    [SerializeField] private SupplyCap ammoCap = new SupplyCap { min = 1, max = 3 };
    [SerializeField] private SupplyCap sanityCap = new SupplyCap { min = 1, max = 3 };

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
                if (AliveCount(t) < RangeFor(t).max) eligible.Add(t);

            if (eligible.Count == 0)
            {
                Log($"initial fill stopped at {placed}: every type already at its max.");
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

    // One replacement: a random type that is under its current cap, on a random valid marker.
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

        Transform marker = PickMarker(markers);
        if (marker == null)
        {
            Log("replacement skipped: no free marker.");
            return;
        }

        Spawn(eligible[Random.Range(0, eligible.Count)], marker);
    }

    // Dynamic caps

    private int ComputeCap(PickupType type)
    {
        SupplyCap range = RangeFor(type);
        if (range == null) return 0;
        if (!useDynamicCaps || player == null) return range.max;

        float need;
        switch (type)
        {
            case PickupType.Health:
                need = 1f - player.Health / Mathf.Max(1f, player.MaxHealth);
                break;
            case PickupType.Ammo:
                need = 1f - player.ReserveAmmo / Mathf.Max(1f, player.MaxReserveAmmo);
                break;
            case PickupType.Sanity:
                need = 1f - player.Sanity / Mathf.Max(1f, player.MaxSanity);
                break;
            default:
                return range.max;
        }

        int cap = Mathf.RoundToInt(Mathf.Lerp(range.min, range.max, Mathf.Clamp01(need)));
        return Mathf.Clamp(cap, range.min, range.max);
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
        ClampRange(healthCap);
        ClampRange(ammoCap);
        ClampRange(sanityCap);
        barricadedCounts.health = Mathf.Max(0, barricadedCounts.health);
        barricadedCounts.ammo = Mathf.Max(0, barricadedCounts.ammo);
        barricadedCounts.sanity = Mathf.Max(0, barricadedCounts.sanity);
        ordinaryCounts.health = Mathf.Max(0, ordinaryCounts.health);
        ordinaryCounts.ammo = Mathf.Max(0, ordinaryCounts.ammo);
        ordinaryCounts.sanity = Mathf.Max(0, ordinaryCounts.sanity);
    }

    private static void ClampRange(SupplyCap range)
    {
        if (range == null) return;
        range.max = Mathf.Max(0, range.max);
        range.min = Mathf.Clamp(range.min, 0, range.max);
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
                Log($"  reserved: {kv.Key.name} @ {kv.Value.name}");
    }
}
