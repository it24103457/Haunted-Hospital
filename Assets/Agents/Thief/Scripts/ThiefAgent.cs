using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * What the Thief knows about the player at one decision: where the player stood and how much the player
 * needs each supply, from 0 (stat full) to 1 (stat empty). Taken once per decision, never tracked continuously.
 */
public struct PlayerSnapshot
{
    public Vector3 Position;
    public float HealthNeed;
    public float SanityNeed;
    public float AmmoNeed;
}

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
public class ThiefAgent : MonoBehaviour, IDamageable
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

    [Header("Item choice")]
    [Tooltip("Weight of the item being far from the player.")]
    [Range(0f, 1f)]
    [SerializeField] private float playerDistanceWeight = 0.5f;
    [Tooltip("Weight of the player needing the item's supply (low health, sanity or ammo).")]
    [Range(0f, 1f)]
    [SerializeField] private float needWeight = 0.3f;
    [Tooltip("Weight of the item being close to the Thief.")]
    [Range(0f, 1f)]
    [SerializeField] private float thiefDistanceWeight = 0.2f;

    [Header("Stashes")]
    [Tooltip("Name of the drop point objects inside each Thief stash; each is matched to its nearest graph node.")]
    [SerializeField] private string dropPointName = "DropPoint";

    [Header("Travel")]
    [Tooltip("Horizontal distance to the last path point at which the Thief counts as arrived.")]
    [SerializeField] private float arriveDistance = 0.3f;
    [Tooltip("Seconds the Thief waits at a door after opening it, covering the door's swing.")]
    [SerializeField] private float doorOpenWait = 1f;
    [Tooltip("Minimum clearance from walls and furniture for the spot the Thief stands on beside an item.")]
    [SerializeField] private float standClearance = 0.4f;
    [Tooltip("Furthest an item's standing spot may be from the item, measured flat.")]
    [SerializeField] private float standSearchRadius = 1.5f;
    [Tooltip("Remembered barricades within this distance are looked at again whenever the Thief stops.")]
    [SerializeField] private float barricadeRecheckDistance = 3f;

    [Header("Steal loop")]
    [Tooltip("Furthest an item may be from the Thief, measured flat, for it to be stolen.")]
    [SerializeField] private float stealRadius = 2f;
    [Tooltip("Seconds before looking again when there is nothing to steal or no route to anything.")]
    [SerializeField] private float retryDelay = 1f;
    [Tooltip("Seconds the Thief stays at the stash after delivering, before deciding again.")]
    [SerializeField] private float stashPause = 3f;

    [Header("Health and respawn")]
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("Seconds between dying and reappearing at a random Thief spawn.")]
    [SerializeField] private float respawnDelay = 5f;
    [Tooltip("Name prefix of the Thief spawn points the Thief starts and respawns at.")]
    [SerializeField] private string thiefSpawnPrefix = "ThiefSpawn";
    [Tooltip("Visual part hidden while dead. Found as a child named Model when left empty.")]
    [SerializeField] private Transform model;

    // Output contract

    // World positions the Thief wants to walk through, in order. Empty while it stands still.
    public IReadOnlyList<Vector3> CurrentPath => currentPath;

    // What the Thief is doing right now.
    public ThiefState State { get; private set; } = ThiefState.Deciding;

    // True while a stolen item is held.
    public bool IsCarrying => carriedItem != null;

    // Factor applied to the NavMeshAgent's base speed by whatever moves the Thief.
    public float SpeedMultiplier { get; private set; } = 1f;

    // Current health; the Thief dies at 0 and comes back at full health.
    public float Health { get; private set; }

    // Raised whenever CurrentPath is replaced or cleared, with the new path.
    public event Action<IReadOnlyList<Vector3>> OnPathChanged;

    // Raised whenever State changes, with the new state.
    public event Action<ThiefState> OnStateChanged;

    private readonly List<Vector3> currentPath = new List<Vector3>();
    private PickupInteractable carriedItem = null;
    private NavMeshAgent agent;

    // NavMesh queries use the Thief's own agent type, the one the hospital NavMesh is baked for.
    private NavMeshQueryFilter navFilter;

    // A stash the Thief can deliver to: the drop point in the scene and the graph node standing on it.
    private struct Stash
    {
        public Transform DropPoint;
        public WaypointNode Node;
    }

    private readonly List<Stash> stashes = new List<Stash>();

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

    // The player as seen at the most recent decision.
    private PlayerSnapshot LastPlayerSample { get; set; }

    // The item the Thief is heading for, and when the next decision (or the respawn, while dead) may happen.
    private PickupInteractable targetItem;
    private float nextDecisionTime;

    private readonly List<Transform> thiefSpawns = new List<Transform>();
    private Collider hitbox;

    // True while the Thief is on a trip, including waiting at a door along the way.
    private bool IsTravelling =>
        State == ThiefState.GoToItem || State == ThiefState.GoToStash || State == ThiefState.WaitAtDoor;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        navFilter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = NavMesh.AllAreas };
        hitbox = GetComponent<Collider>();
        if (model == null) model = transform.Find("Model");
        Health = maxHealth;

        if (graph == null) graph = FindAnyObjectByType<WaypointGraph>();
        if (player == null) player = FindAnyObjectByType<PlayerStats>();

        if (graph == null) Debug.LogError($"ThiefAgent '{name}' found no WaypointGraph in the scene.", this);
        if (player == null) Debug.LogError($"ThiefAgent '{name}' found no PlayerStats in the scene.", this);
    }

    // Start runs after every Awake, so the graph has collected its nodes by now.
    private void Start()
    {
        FindHunterSpawnNodes();
        FindStashes();
        FindThiefSpawns();
        MoveToRandomSpawn();
    }

    /*
     * The steal loop: Deciding -> GoToItem -> Stealing -> GoToStash -> Delivering -> Deciding.
     * Deciding waits for nextDecisionTime, trips run TickTrip until they arrive or need a new decision,
     * Stealing takes the item, and Delivering is the pause at the stash.
     */
    private void Update()
    {
        switch (State)
        {
            case ThiefState.Deciding:
                if (Time.time >= nextDecisionTime) Decide();
                break;

            case ThiefState.GoToItem:
            case ThiefState.GoToStash:
            case ThiefState.WaitAtDoor:
                Travel();
                break;

            case ThiefState.Stealing:
                Steal();
                break;

            case ThiefState.Delivering:
                if (Time.time >= nextDecisionTime) Redecide(0f);
                break;

            case ThiefState.Dead:
                if (Time.time >= nextDecisionTime) Respawn();
                break;
        }
    }

    // Health, death and respawn

    /*
     * Damage from any source (player weapon, Hunter) through IDamageable. At 0 health the Thief dies.
     * Damage while already dead is ignored.
     */
    public void TakeDamage(DamageInfo info)
    {
        if (State == ThiefState.Dead || info.Amount <= 0f) return;

        Health = Mathf.Max(0f, Health - info.Amount);
        if (Health <= 0f) Die();
    }

    /*
     * Drops any carried item on the floor where the Thief fell (collectable again through Release),
     * stops, disappears (model hidden, hitbox off) and schedules the respawn.
     */
    private void Die()
    {
        if (carriedItem != null) carriedItem.Release(FloorBelow(transform.position));
        carriedItem = null;
        targetItem = null;

        ClearPath();
        SetVisible(false);
        nextDecisionTime = Time.time + respawnDelay;
        SetState(ThiefState.Dead);
    }

    /*
     * Reappears at full health at a random Thief spawn as a fresh Thief: barricade memory is cleared,
     * then it decides straight away.
     */
    private void Respawn()
    {
        Health = maxHealth;
        knownBlocked.Clear();
        MoveToRandomSpawn();
        SetVisible(true);
        Redecide(0f);
    }

    private void FindThiefSpawns()
    {
        thiefSpawns.Clear();
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            if (t.name.StartsWith(thiefSpawnPrefix)) thiefSpawns.Add(t);

        if (thiefSpawns.Count == 0) Debug.LogWarning($"ThiefAgent '{name}' found no '{thiefSpawnPrefix}' spawn points.", this);
    }

    // Places the Thief on a random Thief spawn, used at the start of play and on every respawn.
    private void MoveToRandomSpawn()
    {
        if (thiefSpawns.Count == 0) return;

        Vector3 spawn = thiefSpawns[UnityEngine.Random.Range(0, thiefSpawns.Count)].position;
        Vector3 point = FloorBelow(spawn);

        if (agent.isOnNavMesh || NavMesh.SamplePosition(point, out _, 1f, navFilter)) agent.Warp(point);
        else transform.position = point;
    }

    // The NavMesh point under a position, or the position itself when there is no NavMesh nearby.
    private Vector3 FloorBelow(Vector3 position)
    {
        return NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, navFilter) ? hit.position : position;
    }

    private void SetVisible(bool visible)
    {
        if (model != null) model.gameObject.SetActive(visible);
        if (hitbox != null) hitbox.enabled = visible;
    }

    // Steal loop

    /*
     * Chooses what to do next. Carrying an item, it heads for a stash; otherwise it samples the player once,
     * picks the best stealable item it can actually reach, and heads for it. With nothing reachable it
     * looks again after retryDelay.
     */
    private void Decide()
    {
        if (IsCarrying)
        {
            if (!GoToStash()) Redecide(retryDelay);
            return;
        }

        PlayerSnapshot sample = SamplePlayer();
        List<PickupInteractable> items = FindStealableItems();

        // Unreachable items are dropped from this decision's list and the next best is tried.
        while (items.Count > 0)
        {
            PickupInteractable item = ChooseItem(items, sample);
            if (GoToItem(item)) return;

            items.Remove(item);
        }

        Redecide(retryDelay);
    }

    /*
     * Plans the trip to an item: A* from the node nearest the Thief to the node nearest the item's standing
     * spot, then the final stretch to that spot. Returns false when either part has no route.
     */
    private bool GoToItem(PickupInteractable item)
    {
        Vector3 stand = FindStandPoint(item.transform.position);

        List<WaypointNode> itemRoute = PlanRoute(graph.GetNearestNode(transform.position), graph.GetNearestNode(stand));
        if (itemRoute.Count == 0) return false;

        List<Vector3> stretch = FinalStretchTo(itemRoute[itemRoute.Count - 1], stand);
        if (stretch == null) return false;

        targetItem = item;
        StartTrip(itemRoute, ThiefState.GoToItem, stretch);
        return true;
    }

    /*
     * Plans the trip to a stash: the nearest stash in a straight line first, then the others by distance if
     * the nearer ones have no route. The trip ends on the stash's graph node. Returns false when none is reachable.
     */
    private bool GoToStash()
    {
        if (!ChooseStash(out Stash nearest)) return false;

        var candidates = new List<Stash>(stashes);
        candidates.Remove(nearest);
        candidates.Sort((a, b) => FlatDistance(transform.position, a.DropPoint.position)
                                  .CompareTo(FlatDistance(transform.position, b.DropPoint.position)));
        candidates.Insert(0, nearest);

        WaypointNode start = graph.GetNearestNode(transform.position);
        foreach (Stash stash in candidates)
        {
            List<WaypointNode> stashRoute = PlanRoute(start, stash.Node);
            if (stashRoute.Count == 0) continue;

            StartTrip(stashRoute, ThiefState.GoToStash);
            return true;
        }

        return false;
    }

    /*
     * Runs a trip and watches for reasons to decide again: the target item being taken by someone else,
     * or the route being blocked (a locked door or an intact barricade). On arrival it steals or delivers.
     */
    private void Travel()
    {
        if (tripState == ThiefState.GoToItem && !IsStealable(targetItem))
        {
            Redecide(0f);
            return;
        }

        TickTrip();

        if (TripBlocked)
        {
            Redecide(0f);
            return;
        }

        if (!TripArrived) return;

        if (tripState == ThiefState.GoToItem) SetState(ThiefState.Stealing);
        else Deliver();
    }

    /*
     * Takes the target item if it is still free and within stealRadius (measured flat): claims it, marks it
     * stolen and holds it, then heads for a stash. Anything else sends the Thief back to deciding.
     */
    private void Steal()
    {
        PickupInteractable item = targetItem;
        targetItem = null;

        bool inReach = IsStealable(item) && FlatDistance(transform.position, item.transform.position) <= stealRadius;
        if (!inReach || !item.TryClaim())
        {
            Redecide(0f);
            return;
        }

        item.MarkStolen();
        carriedItem = item;

        if (!GoToStash()) Redecide(retryDelay);
    }

    // Hands the carried item over at the stash (it counts as collected) and pauses before deciding again.
    private void Deliver()
    {
        if (carriedItem != null) carriedItem.Remove();
        carriedItem = null;

        ClearPath();
        nextDecisionTime = Time.time + stashPause;
        SetState(ThiefState.Delivering);
    }

    // Stops where it is and returns to deciding after the given delay.
    private void Redecide(float delay)
    {
        targetItem = null;
        ClearPath();
        nextDecisionTime = Time.time + delay;
        SetState(ThiefState.Deciding);
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

        RecheckNearbyBarricades();

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

        if (checkpoint.Type == WaypointType.Barricade && checkpoint.IsBarricadeIntact)
        {
            knownBlocked.Add(checkpoint);
            TripBlocked = true;
            return;
        }

        // A broken barricade is walked straight through, and forgotten if it was remembered as intact.
        knownBlocked.Remove(checkpoint);
        PublishNextSegment();
    }

    // Player stat tracking

    /*
     * Reads the player's position and stats once and turns each stat into a need:
     * need = 1 - current / max, so an empty stat is a need of 1 and a full one a need of 0.
     */
    private PlayerSnapshot SamplePlayer()
    {
        var sample = new PlayerSnapshot();
        if (player == null) return sample;

        sample.Position = player.transform.position;
        sample.HealthNeed = Need(player.Health, player.MaxHealth);
        sample.SanityNeed = Need(player.Sanity, player.MaxSanity);
        sample.AmmoNeed = Need(player.ReserveAmmo, player.MaxReserveAmmo);

        LastPlayerSample = sample;
        return sample;
    }

    private static float Need(float current, float max)
    {
        if (max <= 0f) return 0f;
        return Mathf.Clamp01(1f - current / max);
    }

    // The player's need for the supply an item gives. Items the Thief doesn't steal (such as keys) have no need.
    private static float NeedFor(PickupType type, PlayerSnapshot sample)
    {
        switch (type)
        {
            case PickupType.Health: return sample.HealthNeed;
            case PickupType.Sanity: return sample.SanityNeed;
            case PickupType.Ammo: return sample.AmmoNeed;
            default: return 0f;
        }
    }

    // Live item tracking

    /*
     * Every item the Thief could steal right now: an active supply pickup (health, sanity or ammo) that nobody
     * has claimed. The Thief finds them itself at each decision, so it works with whatever spawner is in the scene.
     */
    private List<PickupInteractable> FindStealableItems()
    {
        var items = new List<PickupInteractable>();

        foreach (PickupInteractable item in FindObjectsByType<PickupInteractable>(FindObjectsInactive.Exclude))
            if (IsStealable(item)) items.Add(item);

        return items;
    }

    // True while an item still exists, is in the world, is unclaimed and is a supply the Thief steals.
    private static bool IsStealable(PickupInteractable item)
    {
        if (item == null || !item.gameObject.activeInHierarchy || item.IsCollected) return false;

        return item.Type == PickupType.Health || item.Type == PickupType.Sanity || item.Type == PickupType.Ammo;
    }

    // Weighted item choice

    /*
     * The best item to steal for this decision, or null when there are none. Each item scores
     *   playerDistanceWeight * P + needWeight * N + thiefDistanceWeight * T
     * where P = its distance from the player / the largest such distance among the items (far = 1),
     *       N = the player's need for its supply (empty stat = 1),
     *       T = 1 - its distance from the Thief / the largest such distance among the items (near = 1).
     * Distances are measured flat, since items sit on furniture at different heights. The highest score wins;
     * on a tie the first item found keeps it.
     */
    private PickupInteractable ChooseItem(List<PickupInteractable> items, PlayerSnapshot sample)
    {
        if (items.Count == 0) return null;

        float maxPlayerDistance = 0f;
        float maxThiefDistance = 0f;
        foreach (PickupInteractable item in items)
        {
            maxPlayerDistance = Mathf.Max(maxPlayerDistance, FlatDistance(sample.Position, item.transform.position));
            maxThiefDistance = Mathf.Max(maxThiefDistance, FlatDistance(transform.position, item.transform.position));
        }

        PickupInteractable best = null;
        float bestScore = float.NegativeInfinity;

        foreach (PickupInteractable item in items)
        {
            float playerDistance = FlatDistance(sample.Position, item.transform.position);
            float thiefDistance = FlatDistance(transform.position, item.transform.position);

            float farFromPlayer = maxPlayerDistance > 0f ? playerDistance / maxPlayerDistance : 1f;
            float closeToThief = maxThiefDistance > 0f ? 1f - thiefDistance / maxThiefDistance : 1f;
            float need = NeedFor(item.Type, sample);

            float score = playerDistanceWeight * farFromPlayer + needWeight * need + thiefDistanceWeight * closeToThief;
            if (score <= bestScore) continue;

            bestScore = score;
            best = item;
        }

        return best;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // Stash choice

    // Matches every stash drop point in the scene to its nearest graph node (the WP_Target_Stash node beside it).
    private void FindStashes()
    {
        stashes.Clear();
        if (graph == null) return;

        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (t.name != dropPointName) continue;

            WaypointNode node = graph.GetNearestNode(t.position);
            if (node != null) stashes.Add(new Stash { DropPoint = t, Node = node });
        }

        if (stashes.Count == 0) Debug.LogWarning($"ThiefAgent '{name}' found no '{dropPointName}' stash drop points.", this);
    }

    /*
     * The stash whose drop point is nearest the Thief in a straight line, measured flat. Distance from the
     * player and the player's stats play no part here. Returns false when the scene has no stashes.
     */
    private bool ChooseStash(out Stash nearest)
    {
        nearest = default;
        float bestDistance = float.MaxValue;

        foreach (Stash stash in stashes)
        {
            float distance = FlatDistance(transform.position, stash.DropPoint.position);
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            nearest = stash;
        }

        return stashes.Count > 0;
    }

    // Barricade memory

    /*
     * Looks again at remembered barricades close to where the Thief has stopped, and forgets any that are
     * no longer intact, so routes through them open up again. Barricades it can't see stay remembered.
     */
    private void RecheckNearbyBarricades()
    {
        if (knownBlocked.Count == 0) return;

        knownBlocked.RemoveWhere(node =>
            node == null ||
            (!node.IsBarricadeIntact &&
             Vector3.Distance(node.Position, transform.position) <= barricadeRecheckDistance));
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
