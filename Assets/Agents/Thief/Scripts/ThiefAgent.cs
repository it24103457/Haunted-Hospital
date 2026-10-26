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
    Dead,
    Inactive,
    Vanished
}

/*
 * Resource Thief agent: steals the supplies the player needs and delivers them to a stash.
 * It decides on a target, plans routes over the shared waypoint graph and publishes them through the
 * output contract below. It never moves itself: the Agent Controller (or the test mover in the Thief test
 * scene) turns CurrentPath into movement and applies SpeedMultiplier to the NavMeshAgent's speed.
 * Loop: Deciding -> GoToItem -> Stealing -> GoToStash -> Delivering -> Vanished -> (reappears) Deciding,
 * with WaitAtDoor during either trip, and Dead until it respawns at a random Thief spawn.
 * After each delivery it vanishes for a random vanishRespawnDelayMin-Max, keeping what it has learned;
 * only being killed wipes its memory.
 * At the start of play it is Inactive (not in the level yet) for a random firstSpawnDelayMin-Max.
 * Each time the player catches sight of it, the player loses sanityDrainPerSighting sanity.
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
    [Tooltip("How strongly nearer stashes are preferred: each reachable stash is weighted by e^(-route length / this). Smaller = almost always the nearest, larger = closer to an even pick.")]
    [SerializeField, Min(0.1f)] private float stashDistanceFalloff = 15f;
    [Tooltip("Never deliver to the same stash twice in a row while another stash is reachable.")]
    [SerializeField] private bool avoidLastStash = true;

    [Header("Travel")]
    [Tooltip("Horizontal distance to the last path point at which the Thief counts as arrived.")]
    [SerializeField] private float arriveDistance = 0.3f;
    [Tooltip("Seconds the Thief waits at a door after opening it, covering the door's swing.")]
    [SerializeField] private float doorOpenWait = 1f;
    [Tooltip("Minimum clearance from walls and furniture for the spot the Thief stands on beside an item.")]
    [SerializeField] private float standClearance = 0.4f;
    [Tooltip("Furthest an item's standing spot may be from the item, measured flat.")]
    [SerializeField] private float standSearchRadius = 1.5f;
    [Tooltip("Remembered barricades and locked doors within this distance are looked at again whenever the Thief stops.")]
    [SerializeField] private float barricadeRecheckDistance = 3f;

    [Header("Steal loop")]
    [Tooltip("Furthest an item may be from the Thief, measured flat, for it to be stolen.")]
    [SerializeField] private float stealRadius = 2f;
    [Tooltip("Seconds before looking again when there is nothing to steal or no route to anything.")]
    [SerializeField] private float retryDelay = 1f;
    [Tooltip("Seconds the Thief stays at the stash after delivering, before it vanishes.")]
    [SerializeField] private float stashPause = 3f;

    [Header("Health and respawn")]
    [SerializeField] private float maxHealth = 100f;
    [Tooltip("Shortest time, in seconds, between being killed and reappearing at a random Thief spawn.")]
    [SerializeField] private float deathRespawnDelayMin = 30f;
    [Tooltip("Longest time, in seconds, between being killed and reappearing at a random Thief spawn.")]
    [SerializeField] private float deathRespawnDelayMax = 45f;
    [Tooltip("Shortest time, in seconds, after play starts before the Thief first appears at a random Thief spawn.")]
    [SerializeField] private float firstSpawnDelayMin = 20f;
    [Tooltip("Longest time, in seconds, after play starts before the Thief first appears at a random Thief spawn.")]
    [SerializeField] private float firstSpawnDelayMax = 30f;
    [Tooltip("Shortest time, in seconds, between vanishing after a stash delivery and reappearing at a random Thief spawn.")]
    [SerializeField] private float vanishRespawnDelayMin = 24f;
    [Tooltip("Longest time, in seconds, between vanishing after a stash delivery and reappearing at a random Thief spawn.")]
    [SerializeField] private float vanishRespawnDelayMax = 45f;
    [Tooltip("Comes back at full health after vanishing. Off: damage carries over until the Thief is killed.")]
    [SerializeField] private bool restoreHealthAfterVanish = true;
    [Tooltip("Name prefix of the Thief spawn points the Thief starts and respawns at.")]
    [SerializeField] private string thiefSpawnPrefix = "ThiefSpawn";
    [Tooltip("Visual part hidden while dead. Found as a child named Model when left empty.")]
    [SerializeField] private Transform model;

    [Header("Sanity drain")]
    [Tooltip("Sanity the player loses each time they catch sight of the Thief. 0 turns the drain off.")]
    [SerializeField, Min(0f)] private float sanityDrainPerSighting = 5f;
    [Tooltip("Furthest the player can see the Thief from, in metres.")]
    [SerializeField, Min(0f)] private float sightDistance = 20f;
    [Tooltip("Seconds the Thief must stay out of the player's sight before seeing it again counts as a new sighting.")]
    [SerializeField, Min(0f)] private float sightingResetTime = 3f;
    [Tooltip("The camera the player looks through. Found on the player when left empty.")]
    [SerializeField] private Camera playerCamera;

    // Output contract

    // World positions the Thief wants to walk through, in order. Empty while it stands still.
    public IReadOnlyList<Vector3> CurrentPath => currentPath;

    // What the Thief is doing right now. Inactive until it first appears.
    public ThiefState State { get; private set; } = ThiefState.Inactive;

    // True unless the Thief is dead (it is alive while waiting to appear and while vanished).
    public bool IsAlive => State != ThiefState.Dead;

    // True while the Thief is in the level: not dead, not vanished and not yet waiting for its first spawn.
    public bool IsInPlay => IsAlive && State != ThiefState.Inactive && State != ThiefState.Vanished;

    // True while a stolen item is held.
    public bool IsCarrying => carriedItem != null;

    // Factor applied to the NavMeshAgent's base speed by whatever moves the Thief.
    public float SpeedMultiplier { get; private set; } = 1f;

    // Current health; the Thief dies at 0 and comes back at full health.
    public float Health { get; private set; }

    public float MaxHealth => maxHealth;

    // Raised whenever CurrentPath is replaced or cleared, with the new path.
    public event Action<IReadOnlyList<Vector3>> OnPathChanged;

    // Raised whenever State changes, with the new state.
    public event Action<ThiefState> OnStateChanged;

    // Raised when the Thief takes an item, with the item (its Type says what it was).
    public event Action<PickupInteractable> OnItemStolen;

    // Raised when the Thief hands an item over at a stash, just before the item is removed.
    public event Action<PickupInteractable> OnItemDelivered;

    // Raised when the Thief drops a carried item on dying, after the item is back in the world.
    public event Action<PickupInteractable> OnItemDropped;

    // Raised each time the Thief appears in the level: its first appearance and every respawn.
    public event Action OnSpawned;

    // Raised when the Thief is killed, after it has dropped any carried item.
    public event Action OnDied;

    // Raised when the Thief vanishes after a stash delivery.
    public event Action OnVanished;

    // True while the player can see the Thief (the current sighting, including the short grace after it leaves view).
    public bool IsSeenByPlayer => seenByPlayer;

    // Raised once per sighting, when the player catches sight of the Thief and loses sanity.
    public event Action OnSeenByPlayer;

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

    // A reachable stash during a stash choice: its index in stashes, the route to it and that route's length in metres.
    private struct StashOption
    {
        public int Index;
        public List<WaypointNode> Route;
        public float Length;
    }

    // The stash the current trip heads for, and the stash of the last delivery (-1 for none). Only dying forgets the last one.
    private int targetStashIndex = -1;
    private int lastStashIndex = -1;

    // Graph nodes at the Hunter spawn points, whose edges get the discount.
    private readonly HashSet<WaypointNode> hunterSpawnNodes = new HashSet<WaypointNode>();

    // Barricade nodes this Thief has found intact and door nodes it has found locked. Planning treats every other one as passable.
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

    // True when a locked door or an intact barricade turned up on the route, so the route needs replanning.
    private bool TripBlocked { get; set; }

    // The player as seen at the most recent decision.
    private PlayerSnapshot LastPlayerSample { get; set; }

    // The item the Thief is heading for, and when the next decision (or the respawn, while dead) may happen.
    private PickupInteractable targetItem;
    private float nextDecisionTime;

    private readonly List<Transform> thiefSpawns = new List<Transform>();
    private Collider hitbox;

    // The current sighting by the player, and when the player last had the Thief in view.
    private bool seenByPlayer;
    private float lastSeenTime;

    // True while the Thief is on a trip, including waiting at a door along the way.
    private bool IsTravelling =>
        State == ThiefState.GoToItem || State == ThiefState.GoToStash || State == ThiefState.WaitAtDoor;

    // Collects its own components and finds the waypoint graph, the player and the player's camera in the scene.
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        navFilter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = NavMesh.AllAreas };
        hitbox = GetComponent<Collider>();
        if (model == null) model = transform.Find("Model");
        Health = maxHealth;

        if (graph == null) graph = FindAnyObjectByType<WaypointGraph>();
        if (player == null) player = FindAnyObjectByType<PlayerStats>();
        if (playerCamera == null && player != null) playerCamera = player.GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;

        if (graph == null) Debug.LogError($"ThiefAgent '{name}' found no WaypointGraph in the scene.", this);
        if (player == null) Debug.LogError($"ThiefAgent '{name}' found no PlayerStats in the scene.", this);
    }

    // Start runs after every Awake, so the graph has collected its nodes by now.
    private void Start()
    {
        FindHunterSpawnNodes();
        FindStashes();
        FindThiefSpawns();

        // Hidden and out of play until the first spawn.
        SetVisible(false);
        nextDecisionTime = Time.time + RandomDelay(firstSpawnDelayMin, firstSpawnDelayMax);
    }

    /*
     * The steal loop: Deciding -> GoToItem -> Stealing -> GoToStash -> Delivering -> Deciding.
     * Deciding waits for nextDecisionTime, trips run TickTrip until they arrive or need a new decision,
     * Stealing takes the item, and Delivering is the pause at the stash.
     */
    private void Update()
    {
        // Out of the level (dead, vanished or not yet appeared) it can't be seen, so its next appearance is a new sighting.
        if (IsInPlay) UpdateSighting();
        else seenByPlayer = false;

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
                if (Time.time >= nextDecisionTime) Vanish();
                break;

            case ThiefState.Vanished:
                if (Time.time >= nextDecisionTime) Appear(restoreHealthAfterVanish);
                break;

            case ThiefState.Dead:
            case ThiefState.Inactive:
                if (Time.time >= nextDecisionTime) Appear(true);
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
        if (!IsInPlay || info.Amount <= 0f) return;

        Health = Mathf.Max(0f, Health - info.Amount);
        if (Health <= 0f) Die();
    }

    /*
     * Restores health up to MaxHealth; ignored while dead. Nothing in the game heals the Thief,
     * so this is only used by test tools.
     */
    public void Heal(float amount)
    {
        if (!IsInPlay || amount <= 0f) return;

        Health = Mathf.Min(maxHealth, Health + amount);
    }

    /*
     * Killed: drops any carried item on the floor where the Thief fell, resting on the floor (collectable again through
     * Release), forgets everything it learned (remembered barricades, locked doors and the last stash used), stops, disappears (model hidden, hitbox off) and comes
     * back after a random time between deathRespawnDelayMin and deathRespawnDelayMax. Raises OnDied.
     */
    private void Die()
    {
        if (carriedItem != null)
        {
            DropOnFloor(carriedItem);
            OnItemDropped?.Invoke(carriedItem);
        }
        carriedItem = null;
        targetItem = null;
        knownBlocked.Clear();
        lastStashIndex = -1;

        ClearPath();
        SetVisible(false);
        nextDecisionTime = Time.time + RandomDelay(deathRespawnDelayMin, deathRespawnDelayMax);
        SetState(ThiefState.Dead);
        OnDied?.Invoke();
    }

    /*
     * Brings the Thief into the level at a random Thief spawn: its first appearance and every return.
     * With restoreHealth it comes back at full health. It decides straight away and raises OnSpawned.
     * The memory of barricades and locked doors is untouched here; only dying clears it.
     */
    private void Appear(bool restoreHealth)
    {
        if (restoreHealth) Health = maxHealth;
        MoveToRandomSpawn();
        SetVisible(true);
        Redecide(0f);
        OnSpawned?.Invoke();
    }

    // A random time between the two ends of a delay range; works whichever end is set larger.
    private static float RandomDelay(float min, float max)
    {
        return UnityEngine.Random.Range(Mathf.Min(min, max), Mathf.Max(min, max));
    }

    /*
     * After a stash delivery: disappears (model hidden, hitbox off) for a random time between
     * vanishRespawnDelayMin and vanishRespawnDelayMax, then reappears at a random Thief spawn.
     * Unlike dying, it keeps its memory of barricades and locked doors. Raises OnVanished.
     */
    private void Vanish()
    {
        targetItem = null;
        ClearPath();
        SetVisible(false);
        nextDecisionTime = Time.time + RandomDelay(vanishRespawnDelayMin, vanishRespawnDelayMax);
        SetState(ThiefState.Vanished);
        OnVanished?.Invoke();
    }

    // Collects every scene object whose name starts with the Thief spawn prefix; one is picked at random on each appearance.
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

    /*
     * Releases a carried item where the Thief fell so it rests on the floor rather than sinking into it.
     * Release places the item's pivot, which sits in the middle of the item, so the item is then lifted until the bottom of
     * its model touches the floor. The floor's height comes from a ray straight down that skips the Thief and the item
     * themselves; without a hit, the NavMesh height is used.
     */
    private void DropOnFloor(PickupInteractable item)
    {
        // The highest surface within a quarter metre of the NavMesh height is the floor (the hits come back in no order).
        Vector3 spot = FloorBelow(transform.position);
        float floorY = spot.y;
        bool found = false;
        foreach (RaycastHit hit in Physics.RaycastAll(spot + Vector3.up * 0.5f, Vector3.down, 1.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(item.transform)) continue;
            if (Mathf.Abs(hit.point.y - spot.y) > 0.25f) continue;
            if (!found || hit.point.y > floorY) floorY = hit.point.y;
            found = true;
        }

        item.Release(new Vector3(spot.x, floorY, spot.z));

        Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        float bottom = renderers[0].bounds.min.y;
        foreach (Renderer r in renderers) bottom = Mathf.Min(bottom, r.bounds.min.y);
        item.transform.position += Vector3.up * (floorY - bottom);
    }

    // Shows or hides the Thief's model and turns its hitbox on or off with it, so a hidden Thief can't be shot or seen.
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
     * Plans the trip to a stash. Every stash with a route is a candidate, except the stash of the last delivery
     * while another one is reachable. One candidate is drawn at random by ChooseStash, so nearer stashes are
     * likelier without the choice being fixed. The trip ends on the stash's graph node. Returns false when no
     * stash is reachable.
     */
    private bool GoToStash()
    {
        WaypointNode start = graph.GetNearestNode(transform.position);

        var candidates = new List<StashOption>();
        for (int i = 0; i < stashes.Count; i++)
        {
            List<WaypointNode> stashRoute = PlanRoute(start, stashes[i].Node);
            if (stashRoute.Count == 0) continue;

            candidates.Add(new StashOption { Index = i, Route = stashRoute, Length = RouteLength(stashRoute) });
        }

        if (candidates.Count == 0) return false;

        if (avoidLastStash && candidates.Count > 1) candidates.RemoveAll(c => c.Index == lastStashIndex);

        StashOption chosen = ChooseStash(candidates);
        targetStashIndex = chosen.Index;
        StartTrip(chosen.Route, ThiefState.GoToStash);
        return true;
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
        OnItemStolen?.Invoke(item);

        if (!GoToStash()) Redecide(retryDelay);
    }

    /*
     * Hands the carried item over at the stash (it counts as collected), remembers that stash as the last one used
     * and pauses there before vanishing.
     */
    private void Deliver()
    {
        if (carriedItem != null)
        {
            OnItemDelivered?.Invoke(carriedItem);
            carriedItem.Remove();
        }
        carriedItem = null;
        lastStashIndex = targetStashIndex;

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

    // Total walking length of a NavMesh path, summed corner to corner, so a spot behind a wall shows up as a long detour.
    private static float PathLength(NavMeshPath path)
    {
        float length = 0f;
        for (int i = 1; i < path.corners.Length; i++)
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        return length;
    }

    /*
     * Runs every frame during a trip. On reaching the end of a segment it either finishes the trip,
     * opens and waits at a door, or carries on past a barricade. After the door wait it resumes the trip;
     * a door closed during the wait is opened again, or ends the trip as blocked if it is now locked.
     * A door that has already finished swinging open is walked straight through without stopping.
     */
    private void TickTrip()
    {
        if (State == ThiefState.WaitAtDoor)
        {
            // A door shut again during the wait (by the player) is opened again and the wait starts over.
            Door door = checkpoint.Door;
            if (door != null && !door.IsOpen)
            {
                if (door.IsLocked)
                {
                    knownBlocked.Add(checkpoint);
                    TripBlocked = true;
                }
                else BeginOpening(door);
                return;
            }

            if (!DoorReady(door)) return;

            SetState(tripState);
            PublishNextSegment();
            return;
        }

        if (!ArrivedAtPathEnd()) return;

        RecheckNearbyBlocked();

        if (checkpoint == null)
        {
            TripArrived = true;
            return;
        }

        if (checkpoint.Type == WaypointType.Door && checkpoint.Door != null)
        {
            if (checkpoint.Door.IsLocked)
            {
                knownBlocked.Add(checkpoint);
                TripBlocked = true;
                return;
            }

            if (checkpoint.Door.IsFullyOpen)
            {
                PublishNextSegment();
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

    // Sanity drain

    /*
     * Drains the player's sanity once per sighting. A sighting starts the moment the player sees the Thief and lasts
     * until the Thief has been out of view for sightingResetTime, so a brief glimpse behind a pillar doesn't count twice.
     * Raises OnSeenByPlayer when a sighting starts.
     */
    private void UpdateSighting()
    {
        if (!PlayerCanSee())
        {
            if (seenByPlayer && Time.time - lastSeenTime >= sightingResetTime) seenByPlayer = false;
            return;
        }

        lastSeenTime = Time.time;
        if (seenByPlayer) return;

        seenByPlayer = true;
        player.DrainSanity(sanityDrainPerSighting);
        OnSeenByPlayer?.Invoke();
    }

    /*
     * True when the player can see the Thief: the player is alive, the middle of the Thief's hitbox is within
     * sightDistance of the player's camera and inside its view, and nothing solid stands between the two.
     */
    private bool PlayerCanSee()
    {
        if (player == null || player.IsDead || playerCamera == null || hitbox == null || !hitbox.enabled) return false;

        Vector3 eye = playerCamera.transform.position;
        Vector3 target = hitbox.bounds.center;
        if ((target - eye).sqrMagnitude > sightDistance * sightDistance) return false;

        Vector3 view = playerCamera.WorldToViewportPoint(target);
        if (view.z <= 0f || view.x < 0f || view.x > 1f || view.y < 0f || view.y > 1f) return false;

        // The first solid thing on the line from the camera must be the Thief itself.
        if (Physics.Linecast(eye, target, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return hit.collider == hitbox || hit.transform.IsChildOf(transform);

        return true;
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

    // How much the player lacks a stat, from 0 (full) to 1 (empty), so stats with different maximums compare on one scale.
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

    // Distance between two points ignoring height, so an item on a shelf or table isn't counted as further away than it is.
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
     * Draws one stash from the candidates (roulette-wheel selection), each with a chance proportional to its weight
     *   e^(-L / stashDistanceFalloff)
     * where L = the length of the route to it in metres. A nearer stash is likelier but never certain.
     */
    private StashOption ChooseStash(List<StashOption> candidates)
    {
        var weights = new float[candidates.Count];
        float total = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            weights[i] = Mathf.Exp(-candidates[i].Length / stashDistanceFalloff);
            total += weights[i];
        }

        // Weights too small to compare (very long routes) leave every candidate equally likely.
        if (total <= 0f) return candidates[UnityEngine.Random.Range(0, candidates.Count)];

        float pick = UnityEngine.Random.value * total;
        for (int i = 0; i < candidates.Count; i++)
        {
            pick -= weights[i];
            if (pick <= 0f) return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }

    // Length of a node route in metres, along its edges.
    private static float RouteLength(List<WaypointNode> nodes)
    {
        float length = 0f;
        for (int i = 1; i < nodes.Count; i++)
            length += Vector3.Distance(nodes[i - 1].Position, nodes[i].Position);

        return length;
    }

    // Barricade and locked door memory

    /*
     * Looks again at remembered barricades and locked doors close to where the Thief has stopped, and forgets any
     * that are no longer in the way (a barricade broken, a door unlocked), so routes through them open up again.
     * Ones it can't see stay remembered.
     */
    private void RecheckNearbyBlocked()
    {
        if (knownBlocked.Count == 0) return;

        knownBlocked.RemoveWhere(node =>
            node == null ||
            (!StillInTheWay(node) &&
             Vector3.Distance(node.Position, transform.position) <= barricadeRecheckDistance));
    }

    // True while a remembered node still blocks the way: a door still locked or a barricade still intact.
    private static bool StillInTheWay(WaypointNode node)
    {
        if (node.Type == WaypointType.Door) return node.Door != null && node.Door.IsLocked;
        return node.IsBarricadeIntact;
    }

    // Opens the door and waits doorOpenWait seconds, also when it is already opening, so it has finished swinging.
    private void BeginOpening(Door door)
    {
        door.Open();
        doorReadyTime = Time.time + doorOpenWait;
        SetState(ThiefState.WaitAtDoor);
    }

    /*
     * True once the wait has passed and the door has finished swinging open. The only place that decides when to walk on.
     * A door being swung shut still counts as open until it is fully closed, so it is never walked into while closing.
     */
    private bool DoorReady(Door door)
    {
        if (door == null) return true;
        return door.IsFullyOpen && Time.time >= doorReadyTime;
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
     * A doorway or barricade is blocked only once this Thief has found it in the way (the door locked, the barricade
     * intact); until then it plans as if the way is open. Nothing about doors or barricades is known in advance.
     */
    private bool IsBlocked(WaypointNode node)
    {
        switch (node.Type)
        {
            case WaypointType.Door:
            case WaypointType.Barricade:
                return knownBlocked.Contains(node);
            default:
                return false;
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

    // Collects the graph node nearest to each Hunter spawn point, whose edges get the Hunter spawn discount.
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
