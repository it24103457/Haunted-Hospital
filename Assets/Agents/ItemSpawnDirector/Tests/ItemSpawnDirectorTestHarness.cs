using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Play-mode test suite for ItemSpawnDirector. Add it next to the director in your own scene copy,
 * then run tests from the component's context menu, or call the public Test_* methods from a tool.
 *
 * Every check logs "TEST PASS: ..." or "TEST FAIL: ..." so the console can be filtered by "TEST ".
 * The tests drive PickupInteractable's public API directly (TryClaim/MarkStolen/Remove/Release),
 * which is exactly the sequence the player's E pickup and the Thief use, so no Thief is required.
 *
 * Delays and the minimum distance are matched to the director's settings; adjust them if you tune
 * the director. Caps are the binary plateaus (1 while a stat is very low, 2 otherwise).
 * RunAllTests runs everything in a sensible order (a few minutes).
 */
public class ItemSpawnDirectorTestHarness : MonoBehaviour
{
    [Tooltip("The director under test. Found automatically when empty.")]
    [SerializeField] private ItemSpawnDirector director;

    [Tooltip("Matches the director's Respawn Delay.")]
    [SerializeField] private float expectedPlayerDelay = 10f;
    [Tooltip("Matches the director's Stolen Respawn Delay.")]
    [SerializeField] private float expectedStolenDelay = 20f;
    [Tooltip("Matches the director's Min Spawn Distance.")]
    [SerializeField] private float expectedMinDistance = 8f;
    [Tooltip("How many items the initial layout should have placed (0 = don't check the total).")]
    [SerializeField] private int expectedInitialTotal = 9;

    // Every check in order, readable from tools even when console log capture is unreliable.
    public readonly List<string> results = new List<string>();

    public int Passed => passed;
    public int Failed => failed;

    private int passed;
    private int failed;

    // Individual tests (also usable from the component's context menu)

    [ContextMenu("Test: Initial Placement")]
    public void Test_InitialPlacement() => Run(Test_InitialPlacementRoutine(), "InitialPlacement");

    [ContextMenu("Test: Player Collect Respawn")]
    public void Test_PlayerCollectRespawn() => Run(Test_PlayerCollectRespawnRoutine(), "PlayerCollectRespawn");

    [ContextMenu("Test: Stolen Respawn")]
    public void Test_StolenRespawn() => Run(Test_StolenRespawnRoutine(), "StolenRespawn");

    [ContextMenu("Test: Cap Enforcement")]
    public void Test_CapEnforcement() => Run(Test_CapEnforcementRoutine(), "CapEnforcement");

    [ContextMenu("Test: Dynamic Caps")]
    public void Test_DynamicCaps() => Run(Test_DynamicCapsRoutine(), "DynamicCaps");

    [ContextMenu("Test: Dropped Item Keeps Spot")]
    public void Test_DroppedItemKeepsSpot() => Run(Test_DroppedItemKeepsSpotRoutine(), "DroppedItemKeepsSpot");

    [ContextMenu("Test: Distance Window")]
    public void Test_DistanceWindow() => Run(Test_DistanceWindowRoutine(), "DistanceWindow");

    [ContextMenu("Test: Solver Unit")]
    public void Test_SolverUnit() => Run(Test_SolverUnitRoutine(), "SolverUnit");

    [ContextMenu("Test: Desperation Floor")]
    public void Test_DesperationFloor() => Run(Test_DesperationFloorRoutine(), "DesperationFloor");

    [ContextMenu("Test: Cap One Forces Reachable")]
    public void Test_CapOneForcesReachable() => Run(Test_CapOneForcesReachableRoutine(), "CapOneForcesReachable");

    [ContextMenu("Test: Cleared Room Becomes Reachable")]
    public void Test_ClearedRoomBecomesReachable() => Run(Test_ClearedRoomBecomesReachableRoutine(), "ClearedRoomBecomesReachable");

    [ContextMenu("Run All Tests")]
    public void RunAllTests() => Run(TestAllRoutine(), "AllTests");

    // The full suite, in an order that leaves the scene in a workable state
    private IEnumerator TestAllRoutine()
    {
        yield return Test_SolverUnitRoutine();
        yield return Test_InitialPlacementRoutine();
        yield return Test_DynamicCapsRoutine();
        yield return Test_CapEnforcementRoutine();
        yield return Test_DroppedItemKeepsSpotRoutine();
        yield return Test_PlayerCollectRespawnRoutine();
        yield return Test_StolenRespawnRoutine();
        yield return Test_DesperationFloorRoutine();
        yield return Test_CapOneForcesReachableRoutine();
        yield return Test_ClearedRoomBecomesReachableRoutine();
        yield return Test_DistanceWindowRoutine();
    }

    // The tests

    // SpawnCspSolver in isolation on synthetic markers (no scene state): finds an assignment
    // when one exists, respects domains and distinctness, and reports failure when the
    // constraints cannot be satisfied.
    private IEnumerator Test_SolverUnitRoutine()
    {
        Transform a = NewSyntheticMarker("cspA");
        Transform b = NewSyntheticMarker("cspB");
        Transform c = NewSyntheticMarker("cspC");

        // Solvable: X{A,B} Y{A,C} Z{A,B,C} - e.g. Z=A, X=B, Y=C.
        var solvable = new List<SpawnCspSolver.Variable>
        {
            new SpawnCspSolver.Variable("X", a, b),
            new SpawnCspSolver.Variable("Y", a, c),
            new SpawnCspSolver.Variable("Z", a, b, c),
        };

        if (Require(SpawnCspSolver.Solve(solvable, out SpawnCspSolver.Stats stats),
            "solver finds an assignment when one exists"))
        {
            var used = new HashSet<Transform>();
            foreach (SpawnCspSolver.Variable v in solvable)
            {
                Check(v.Assignment != null && v.Domain.Contains(v.Assignment),
                    $"variable {v.Id} got a value from its own domain");
                if (v.Assignment != null)
                    Check(used.Add(v.Assignment), $"variable {v.Id} got a distinct marker");
            }
            Log($"TEST INFO: solver used {stats.Backtracks} backtracks on the solvable case");
        }

        // Unsolvable: two variables sharing one marker while distinctness is required.
        var unsolvable = new List<SpawnCspSolver.Variable>
        {
            new SpawnCspSolver.Variable("P", a),
            new SpawnCspSolver.Variable("Q", a),
        };
        Check(!SpawnCspSolver.Solve(unsolvable, out _),
            "solver reports failure when the constraints cannot be satisfied");

        Destroy(a.gameObject);
        Destroy(b.gameObject);
        Destroy(c.gameObject);
        yield break;
    }

    // The director placed the agreed initial layout: the right total, an even per-type split,
    // and every placed item reserved on a marker.
    private IEnumerator Test_InitialPlacementRoutine()
    {
        if (!Guard()) yield break;

        int health = director.AliveCount(PickupType.Health);
        int ammo = director.AliveCount(PickupType.Ammo);
        int sanity = director.AliveCount(PickupType.Sanity);
        int total = health + ammo + sanity;

        Check(total > 0, $"initial layout placed {total} items");
        if (expectedInitialTotal > 0)
            Check(total == expectedInitialTotal, $"item total is {total}, expected {expectedInitialTotal}");
        Check(health == ammo && ammo == sanity,
            $"per-type split is even (Health {health}, Ammo {ammo}, Sanity {sanity})");
        Check(director.FreeMarkerCount > 0, $"{director.FreeMarkerCount} markers are still free");

        int reserved = 0;
        foreach (PickupInteractable p in AllItems())
            if (p != null && director.IsReserved(p)) reserved++;
        Check(reserved == total, $"all {total} items hold a reserved spot ({reserved} reserved)");

        // The hard split is exact: exactly 3 items sit on barricaded-labeled markers and the
        // rest on ordinary ones. Labels mirror the director's default Inspector list - if a
        // designer retunes that list, update these together with it.
        string[] barricadeLabels = { "SP_S04", "SP_S08", "SP_S09", "SP_S10", "SP_S12" };
        int onBarricade = 0, onOrdinary = 0, offMarker = 0;
        foreach (PickupInteractable p in AllItems())
        {
            if (p == null || !director.IsReserved(p)) continue;
            Transform m = NearestMarker(p.transform.position);
            if (m == null) { offMarker++; continue; }
            if (Array.IndexOf(barricadeLabels, m.name) >= 0) onBarricade++;
            else onOrdinary++;
        }
        Check(offMarker == 0, $"every reserved item sits on a marker ({offMarker} off)");
        Check(onBarricade == 3, $"exactly 3 items on barricaded markers (found {onBarricade})");
        Check(onOrdinary == total - 3, $"remaining items sit on ordinary markers (found {onOrdinary}, expected {total - 3})");
    }

    // A player pickup (TryClaim + Remove) frees the spot and brings a replacement after ~10s.
    // Premise under the binary caps: stats sit above the very-low threshold (every cap = the
    // normal count, 2) and the collected type is first settled at its cap, so the timed
    // collection drops it below the cap and exactly one replacement is allowed through. The
    // original stats are restored afterwards.
    private IEnumerator Test_PlayerCollectRespawnRoutine()
    {
        if (!Guard()) yield break;
        PlayerStats player = FindPlayer();
        if (!Require(player != null, "a PlayerStats exists")) yield break;

        PickupInteractable seed = FindAnyItem();
        if (!Require(seed != null, "an item exists to collect")) yield break;
        PickupType type = seed.Type;

        PlayerSnapshot snapshot = SnapshotPlayer(player);
        SetAllFracsNormal(player);

        // Settle the type at its cap first: collect everything, let every queued replacement fire
        // in one burst, then take the timed collection. We only need the seed type to land in the
        // usable range 1..cap (over cap would block a respawn; 0 would leave nothing to collect).
        int cap = director.CurrentCap(type);
        CollectAll(type, false);
        yield return new WaitForSeconds(expectedPlayerDelay + 2f);
        int settled = director.AliveCount(type);
        if (!Require(settled >= 1 && settled <= cap,
            $"{type} settled at its cap ({settled} of cap {cap}) with an item left to collect"))
        {
            RestorePlayer(player, snapshot);
            yield break;
        }

        int before = director.AliveCount(type);
        int totalBefore = TotalAlive();
        PickupInteractable item = FindItemOfType(type);
        if (!Require(item != null, $"a {type} item exists to collect"))
        {
            RestorePlayer(player, snapshot);
            yield break;
        }
        float t0 = Time.time;

        if (!Require(SimulatePlayerCollect(item), "the item accepted the player collection"))
        {
            RestorePlayer(player, snapshot);
            yield break;
        }
        Check(director.AliveCount(type) == before - 1,
            $"{type} count dropped to {director.AliveCount(type)} immediately after collection");

        bool arrived = false;
        yield return WaitUntil(() => TotalAlive() >= totalBefore, expectedPlayerDelay + 5f, v => arrived = v);
        float elapsed = Time.time - t0;
        RestorePlayer(player, snapshot);

        Check(arrived, $"a replacement appeared after {elapsed:0.0}s (total supply back to {totalBefore})");
        if (arrived)
            Check(elapsed >= expectedPlayerDelay - 0.5f && elapsed <= expectedPlayerDelay + 3f,
                $"player-collect respawn took {elapsed:0.0}s (expected ~{expectedPlayerDelay}s)");
    }

    // A stolen delivery (TryClaim + MarkStolen + Remove) brings the replacement much later (~20s).
    // Same premise as the player test: normal plateaus and the type settled at its cap first
    // (via player collections, which settle at the 10s delay), then the timed steal.
    private IEnumerator Test_StolenRespawnRoutine()
    {
        if (!Guard()) yield break;
        PlayerStats player = FindPlayer();
        if (!Require(player != null, "a PlayerStats exists")) yield break;

        PickupInteractable seed = FindAnyItem();
        if (!Require(seed != null, "an item exists to be stolen")) yield break;
        PickupType type = seed.Type;

        PlayerSnapshot snapshot = SnapshotPlayer(player);
        SetAllFracsNormal(player);

        // Same usable range as the player test: settle the seed type into 1..cap so the timed
        // steal drops it below the cap and a replacement is guaranteed. Player collections settle
        // at the 10s delay, so one wait covers the whole burst.
        int cap = director.CurrentCap(type);
        CollectAll(type, false);
        yield return new WaitForSeconds(expectedPlayerDelay + 2f);
        int settled = director.AliveCount(type);
        if (!Require(settled >= 1 && settled <= cap,
            $"{type} settled at its cap ({settled} of cap {cap}) with an item left to steal"))
        {
            RestorePlayer(player, snapshot);
            yield break;
        }

        int before = director.AliveCount(type);
        int totalBefore = TotalAlive();
        PickupInteractable item = FindItemOfType(type);
        if (!Require(item != null, $"a {type} item exists to be stolen"))
        {
            RestorePlayer(player, snapshot);
            yield break;
        }
        float t0 = Time.time;

        if (!Require(SimulateStolenCollect(item), "the item accepted the steal"))
        {
            RestorePlayer(player, snapshot);
            yield break;
        }
        Check(director.AliveCount(type) == before - 1,
            $"{type} count dropped to {director.AliveCount(type)} immediately after the steal");

        bool arrived = false;
        yield return WaitUntil(() => TotalAlive() >= totalBefore, expectedStolenDelay + 5f, v => arrived = v);
        float elapsed = Time.time - t0;
        RestorePlayer(player, snapshot);

        Check(arrived, $"a replacement appeared after {elapsed:0.0}s (total supply back to {totalBefore})");
        if (arrived)
            Check(elapsed >= expectedStolenDelay - 0.5f && elapsed <= expectedStolenDelay + 3f,
                $"stolen respawn took {elapsed:0.0}s (expected ~{expectedStolenDelay}s)");
        Check(elapsed >= expectedPlayerDelay + 5f,
            $"stolen respawn ({elapsed:0.0}s) is clearly longer than the player delay ({expectedPlayerDelay}s)");
    }

    // With the player healed to full the cap sits at its normal count (2); collecting everything
    // then lets exactly cap-many replacements through - never the full count back.
    private IEnumerator Test_CapEnforcementRoutine()
    {
        if (!Guard()) yield break;
        PlayerStats player = FindPlayer();
        if (!Require(player != null, "a PlayerStats exists")) yield break;

        player.Heal(player.MaxHealth);
        int capAtFull = director.CurrentCap(PickupType.Health);
        Check(capAtFull == 2, $"at full health the health cap is the normal count ({capAtFull})");

        var collected = new List<PickupInteractable>();
        foreach (PickupInteractable p in AllItems())
            if (p != null && p.Type == PickupType.Health && !p.IsCollected) collected.Add(p);

        if (!Require(collected.Count > 0, "there are health items to collect")) yield break;

        foreach (PickupInteractable p in collected)
            SimulatePlayerCollect(p);

        Check(director.AliveCount(PickupType.Health) == 0, "all health items are gone after collecting them");

        bool arrived = false;
        yield return WaitUntil(() => director.AliveCount(PickupType.Health) >= capAtFull,
            expectedPlayerDelay + 5f, v => arrived = v);

        Check(arrived, $"health respawned up to the cap ({capAtFull})");
        Check(director.AliveCount(PickupType.Health) == capAtFull,
            $"health supply settled exactly on the cap: {director.AliveCount(PickupType.Health)} alive, cap {capAtFull}");
        if (capAtFull >= collected.Count)
            Log("TEST INFO: the cap equals the collected count, so this run cannot exceed it anyway - " +
                "lower the very-low count to make the check bite harder");
    }

    // The cap is a binary plateau: the normal count while the stat is at/above the very-low
    // threshold (30% by default), the very-low count below it - read fresh for every stat type.
    // Exactly 30% counts as NOT very low (cap 2).
    private IEnumerator Test_DynamicCapsRoutine()
    {
        if (!Guard()) yield break;
        PlayerStats player = FindPlayer();
        if (!Require(player != null, "a PlayerStats exists")) yield break;

        PlayerSnapshot snapshot = SnapshotPlayer(player);

        // Health: full and just above the threshold -> normal count; just below -> very-low count.
        SetStatFrac(player, PickupType.Health, 1f);
        Check(director.CurrentCap(PickupType.Health) == 2,
            $"health cap is the normal count at full health ({director.CurrentCap(PickupType.Health)})");

        SetStatFrac(player, PickupType.Health, 0.31f);
        Check(director.CurrentCap(PickupType.Health) == 2,
            $"health cap is the normal count at 31% health ({director.CurrentCap(PickupType.Health)})");

        SetStatFrac(player, PickupType.Health, 0.29f);
        Check(director.CurrentCap(PickupType.Health) == 1,
            $"health cap is the very-low count at 29% health ({director.CurrentCap(PickupType.Health)})");

        SetStatFrac(player, PickupType.Health, 0.10f);
        Check(director.CurrentCap(PickupType.Health) == 1,
            $"health cap is the very-low count at 10% health ({director.CurrentCap(PickupType.Health)})");

        // Sanity: same plateaus.
        SetStatFrac(player, PickupType.Sanity, 1f);
        Check(director.CurrentCap(PickupType.Sanity) == 2,
            $"sanity cap is the normal count at full sanity ({director.CurrentCap(PickupType.Sanity)})");

        SetStatFrac(player, PickupType.Sanity, 0.25f);
        Check(director.CurrentCap(PickupType.Sanity) == 1,
            $"sanity cap is the very-low count at 25% sanity ({director.CurrentCap(PickupType.Sanity)})");

        // Ammo: plateaus plus the exact threshold - 30% of the reserve is integer-exact here,
        // so equality can be checked without float luck (guarded, in case a scene's max differs).
        SetStatFrac(player, PickupType.Ammo, 1f);
        Check(director.CurrentCap(PickupType.Ammo) == 2,
            $"ammo cap is the normal count at a full reserve ({director.CurrentCap(PickupType.Ammo)})");

        SetStatFrac(player, PickupType.Ammo, 0.30f);
        float atThreshold = player.ReserveAmmo / Mathf.Max(1f, player.MaxReserveAmmo);
        if (Mathf.Abs(atThreshold - 0.3f) < 1e-4f)
            Check(director.CurrentCap(PickupType.Ammo) == 2,
                $"exactly 30% reserve counts as NOT very low (cap {director.CurrentCap(PickupType.Ammo)}, {player.ReserveAmmo}/{player.MaxReserveAmmo})");
        else
            Log($"TEST INFO: reserve granularity misses 30% exactly ({player.ReserveAmmo}/{player.MaxReserveAmmo} = {atThreshold:0.0000}) - threshold equality check skipped");

        SetStatFrac(player, PickupType.Ammo, 0.25f);
        Check(director.CurrentCap(PickupType.Ammo) == 1,
            $"ammo cap is the very-low count at 25% reserve ({director.CurrentCap(PickupType.Ammo)})");

        SetStatFrac(player, PickupType.Ammo, 0f);
        Check(director.CurrentCap(PickupType.Ammo) == 1,
            $"ammo cap is the very-low count at an empty reserve ({director.CurrentCap(PickupType.Ammo)})");

        RestorePlayer(player, snapshot);
    }

    // A stolen item that is dropped (TryClaim + MarkStolen + Release) keeps its spot and never
    // duplicates; only a real collection frees it again.
    private IEnumerator Test_DroppedItemKeepsSpotRoutine()
    {
        if (!Guard()) yield break;

        PickupInteractable item = FindAnyItem();
        if (!Require(item != null, "an item exists for the drop test")) yield break;

        PickupType type = item.Type;
        int before = director.AliveCount(type);

        if (!Require(SimulateStolenDrop(item), "the item accepted the steal")) yield break;

        Check(item != null && !item.IsCollected, "the dropped item is back in the world and collectable");
        Check(director.IsReserved(item), "the dropped item still holds its reserved spot");
        Check(director.AliveCount(type) == before,
            $"no replacement was queued (still {director.AliveCount(type)} {type} alive)");

        yield return new WaitForSeconds(expectedPlayerDelay + 2f);

        Check(item != null && director.IsReserved(item), "after the player delay the spot is still reserved");
        Check(director.AliveCount(type) == before, $"still no duplicate ({director.AliveCount(type)} {type} alive)");

        // Cleanup: collect it for real now, which frees the spot like any normal pickup.
        if (item != null) SimulatePlayerCollect(item);
    }

    // After a collection, the replacement spawns at least expectedMinDistance away from the player,
    // even when the player stands right on a marker. The whole type is collected (alive 0 sits
    // under any cap, so a replacement is guaranteed no matter where the stat plateaus are), the
    // player is parked first, and the first new item in the world is checked.
    private IEnumerator Test_DistanceWindowRoutine()
    {
        if (!Guard()) yield break;
        PlayerStats player = FindPlayer();
        if (!Require(player != null, "a PlayerStats exists")) yield break;

        var items = new HashSet<PickupInteractable>();
        foreach (PickupInteractable p in AllItems())
            if (p != null) items.Add(p);
        if (!Require(items.Count > 0, "there is an item to collect")) yield break;

        PickupInteractable seed = FindAnyItem();
        if (!Require(seed != null, "a director-owned item exists to collect")) yield break;
        PickupType type = seed.Type;

        // Park the player on some marker, so nearby spots are obviously too close.
        Transform[] markers = FindMarkers();
        if (!Require(markers.Length > 0, "there are markers in the scene")) yield break;
        Transform target = markers[UnityEngine.Random.Range(0, markers.Length)];

        Vector3 originalPosition = player.transform.position;
        CharacterController controller = player.GetComponentInChildren<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = target.position;
        if (controller != null) controller.enabled = true;

        CollectAll(type, false);

        bool arrived = false;
        yield return WaitUntil(() => AnyNewItem(items) != null, expectedPlayerDelay + 5f, v => arrived = v);
        Check(arrived, $"a replacement appeared while the player was parked on a marker");

        if (arrived)
        {
            PickupInteractable replacement = AnyNewItem(items);
            if (Require(replacement != null, "the replacement was found in the world"))
            {
                float distance = Vector3.Distance(replacement.transform.position, player.transform.position);
                Check(distance >= expectedMinDistance - 0.5f,
                    $"replacement spawned {distance:0.0}m from the player (minimum {expectedMinDistance}m)");
            }
        }

        // Put the player back.
        if (controller != null) controller.enabled = false;
        player.transform.position = originalPosition;
        if (controller != null) controller.enabled = true;
    }

    // The cap plateaus and the reachability floor together, in both directions. Normal plateau:
    // stats at/above the threshold -> cap 2 -> up to two replacements of the collected type, at
    // least one of them on a reachable marker (floor = max(1, cap-1) = 1). Very-low plateau: stat
    // below the threshold -> cap 1 ("scarce when desperate") -> exactly one replacement, also on
    // a reachable marker.
    private IEnumerator Test_DesperationFloorRoutine()
    {
        if (!Guard()) yield break;
        PlayerStats player = FindPlayer();
        if (!Require(player != null, "a PlayerStats exists")) yield break;

        PlayerSnapshot before = SnapshotPlayer(player);

        // Normalize first so nothing is under cap diverting the replacements we are about to
        // queue; then pick the type with the most items alive as the subject.
        yield return NormalizeSupply(player);

        PickupType type = PickupType.Health;
        int aliveNow = -1;
        foreach (PickupType t in new[] { PickupType.Health, PickupType.Ammo, PickupType.Sanity })
        {
            if (director.AliveCount(t) > aliveNow) { aliveNow = director.AliveCount(t); type = t; }
        }
        if (!Require(aliveNow >= 1, "at least one item is alive to collect"))
        {
            RestorePlayer(player, before);
            yield break;
        }

        // Plateau 1 - normal: cap 2, floor 1.
        int capNormal = director.CurrentCap(type);
        Check(capNormal == 2, $"normal plateau: cap is the normal count ({capNormal})");

        int initial = director.AliveCount(type);
        int expectNormal = Mathf.Min(initial, capNormal);
        CollectAll(type, false);

        bool arrived = false;
        yield return WaitUntil(() => director.AliveCount(type) >= expectNormal,
            expectedPlayerDelay + 5f, v => arrived = v);
        Check(arrived, $"supply restored under the normal cap ({director.AliveCount(type)} of {expectNormal} expected)");

        if (arrived)
        {
            Check(director.AliveCount(type) == expectNormal,
                $"supply settled exactly on the normal cap: {director.AliveCount(type)} alive, expected {expectNormal}");
            Check(CountReachable(type) >= 1,
                $"{type} on reachable markers: {CountReachable(type)} of {director.AliveCount(type)} " +
                $"(floor {Mathf.Max(1, director.CurrentCap(type) - 1)} at cap {director.CurrentCap(type)})");
        }

        // Plateau 2 - very low: cap 1, floor 1. Exactly one box comes back, and it must be reachable.
        SetStatFrac(player, type, 0.05f);
        int capLow = director.CurrentCap(type);
        Check(capLow == 1, $"very-low plateau: cap drops to the very-low count ({capLow})");

        CollectAll(type, false);
        arrived = false;
        yield return WaitUntil(() => director.AliveCount(type) >= 1, expectedPlayerDelay + 5f, v => arrived = v);
        Check(arrived, "one replacement came back under the very-low cap");
        if (arrived)
        {
            Check(director.AliveCount(type) == 1,
                $"supply settled on the very-low cap: {director.AliveCount(type)} alive, cap {director.CurrentCap(type)}");
            Check(CountReachable(type) >= 1,
                $"the single {type} sits on a reachable marker " +
                $"(floor {Mathf.Max(1, director.CurrentCap(type) - 1)} at cap {director.CurrentCap(type)})");
        }

        RestorePlayer(player, before);
    }

    // Premise inversion for the binary caps: cap 1 now means the stat is BELOW the threshold
    // (drive it low - scarce when desperate), and the floor at cap 1 is max(1, cap-1) = 1 - so
    // the single restored slot must land on a reachable marker (the old "any marker is legal"
    // behaviour is gone). The repeat cycles guard against over-tightening: the slot always comes
    // back, exactly once, never blocked and never duplicated.
    private IEnumerator Test_CapOneForcesReachableRoutine()
    {
        if (!Guard()) yield break;
        PlayerStats player = FindPlayer();
        if (!Require(player != null, "a PlayerStats exists")) yield break;

        PlayerSnapshot before = SnapshotPlayer(player);

        // Normalize so nothing is under cap diverting replacements; then pick the type with the
        // most items alive and push that type's stat below the threshold.
        yield return NormalizeSupply(player);

        PickupType type = PickupType.Health;
        int aliveNow = -1;
        foreach (PickupType t in new[] { PickupType.Health, PickupType.Ammo, PickupType.Sanity })
        {
            if (director.AliveCount(t) > aliveNow) { aliveNow = director.AliveCount(t); type = t; }
        }
        if (!Require(aliveNow >= 1, "an item exists for the cap-one cycles")) yield break;

        SetStatFrac(player, type, 0.05f);
        Check(director.CurrentCap(type) == 1,
            $"{type} cap sits at the very-low count with the stat at 5% ({director.CurrentCap(type)})");

        for (int cycle = 1; cycle <= 3; cycle++)
        {
            CollectAll(type, false);
            Check(director.AliveCount(type) == 0, $"cycle {cycle}: every {type} item is collected");

            bool arrived = false;
            yield return WaitUntil(() => director.AliveCount(type) >= 1, 15f, v => arrived = v);
            Check(arrived, $"cycle {cycle}: the single slot was restored under cap 1");
            if (arrived)
            {
                Check(director.AliveCount(type) == 1,
                    $"cycle {cycle}: exactly one {type} alive, never more (cap 1)");
                Check(CountReachable(type) >= 1,
                    $"cycle {cycle}: the single {type} sits on a reachable marker (floor 1 forces reachable-only)");
            }
        }

        RestorePlayer(player, before);
    }

    // A labeled marker whose barricade is bound flips reachable once the barricade is cleared
    // (SetActive(false), exactly what the breakables do) and back when it is restored. Scenes
    // without breakable barricades pass with a skip note - that is the Scene A case.
    private IEnumerator Test_ClearedRoomBecomesReachableRoutine()
    {
        if (!Guard()) yield break;

        string[] barricadeLabels = { "SP_S04", "SP_S08", "SP_S09", "SP_S10", "SP_S12" };
        GameObject bound = null;
        string boundLabel = null;
        foreach (string label in barricadeLabels)
        {
            GameObject b = director.BoundBarricadeOf(label);
            if (b != null) { bound = b; boundLabel = label; break; }
        }
        if (bound == null)
        {
            Check(true, "no labeled marker has a bound barricade here - clearing test skipped");
            yield break;
        }

        Check(!director.IsReachableMarker(boundLabel),
            $"'{boundLabel}' is unreachable while its barricade is intact");

        bool wasActive = bound.activeSelf;
        bound.SetActive(false);
        Check(director.IsReachableMarker(boundLabel),
            $"'{boundLabel}' became reachable after its barricade was cleared");

        bound.SetActive(wasActive);
        Check(!director.IsReachableMarker(boundLabel),
            $"'{boundLabel}' is unreachable again after the barricade is restored");
    }

    // Plumbing

    private bool Guard()
    {
        if (director == null) director = FindFirstObjectByType<ItemSpawnDirector>();
        if (director == null)
        {
            Check(false, "an ItemSpawnDirector exists in the scene");
            return false;
        }
        return true;
    }

    private void Run(IEnumerator suite, string name)
    {
        results.Clear();
        passed = 0;
        failed = 0;
        Log($"TEST --- {name} started");
        StartCoroutine(RunRoutine(suite, name));
    }

    private IEnumerator RunRoutine(IEnumerator suite, string name)
    {
        while (suite.MoveNext()) yield return suite.Current;
        results.Add($"SUITE {name} done: {passed} passed, {failed} failed");
        Log($"TEST --- {name} done: {passed} passed, {failed} failed");
    }

    private void Check(bool condition, string what)
    {
        if (condition)
        {
            passed++;
            Log($"TEST PASS: {what}");
        }
        else
        {
            failed++;
            Log($"TEST FAIL: {what}");
        }
        results.Add($"{(condition ? "PASS" : "FAIL")}: {what}");
    }

    private bool Require(bool condition, string what)
    {
        Check(condition, what);
        return condition;
    }

    // Waits until the condition is true, at most 'timeout' seconds; reports whether it became true.
    private IEnumerator WaitUntil(Func<bool> condition, float timeout, Action<bool> onResult)
    {
        float deadline = Time.time + timeout;
        while (!condition() && Time.time < deadline) yield return null;
        onResult?.Invoke(condition());
    }

    // Simulates exactly what the real systems do, in PickupInteractable's own order.

    // The player's stats as they were, so a test can put them back afterwards.
    private struct PlayerSnapshot
    {
        public float Health;
        public float Sanity;
        public int Ammo;
    }

    private static PlayerSnapshot SnapshotPlayer(PlayerStats p)
        => new PlayerSnapshot { Health = p.Health, Sanity = p.Sanity, Ammo = p.ReserveAmmo };

    // Puts the stats back exactly - up when the test drained them, down when it raised them.
    private void RestorePlayer(PlayerStats p, PlayerSnapshot s)
    {
        if (p.Health > s.Health) p.TakeDamage(new DamageInfo(p.Health - s.Health, gameObject, p.transform.position));
        else if (p.Health < s.Health) p.Heal(s.Health - p.Health);
        if (p.Sanity > s.Sanity) p.DrainSanity(p.Sanity - s.Sanity);
        else if (p.Sanity < s.Sanity) p.AddSanity(s.Sanity - p.Sanity);
        if (p.ReserveAmmo > s.Ammo) p.TryUseAmmo(p.ReserveAmmo - s.Ammo);
        else if (p.ReserveAmmo < s.Ammo) p.AddAmmo(s.Ammo - p.ReserveAmmo);
    }

    private static bool SimulatePlayerCollect(PickupInteractable item)
    {
        if (item == null || !item.TryClaim()) return false;
        item.Remove();
        return true;
    }

    private static bool SimulateStolenCollect(PickupInteractable item)
    {
        if (item == null || !item.TryClaim()) return false;
        item.MarkStolen();
        item.Remove();
        return true;
    }

    private static bool SimulateStolenDrop(PickupInteractable item)
    {
        if (item == null || !item.TryClaim()) return false;
        item.MarkStolen();
        item.Release(item.transform.position);
        return true;
    }

    private static PickupInteractable[] AllItems()
        => FindObjectsByType<PickupInteractable>(FindObjectsInactive.Exclude);

    // Items alive across all supply types (a replacement of any type restores the total).
    private int TotalAlive()
        => director.AliveCount(PickupType.Health)
         + director.AliveCount(PickupType.Ammo)
         + director.AliveCount(PickupType.Sanity);

    // Only director-owned items count; scenes also contain foreign pickups (the ExitKey in the
    // KeyBox, for example) that this director never reserved.
    private PickupInteractable FindAnyItem()
    {
        foreach (PickupInteractable p in AllItems())
            if (p != null && !p.IsCollected && director != null && director.IsReserved(p)) return p;
        return null;
    }

    // The first live, reserved item of that type.
    private PickupInteractable FindItemOfType(PickupType type)
    {
        foreach (PickupInteractable p in AllItems())
            if (p != null && !p.IsCollected && director != null && director.IsReserved(p) && p.Type == type) return p;
        return null;
    }

    // Collects every live item of the type through the public pickup API; returns how many.
    private int CollectAll(PickupType type, bool stolen)
    {
        int taken = 0;
        foreach (PickupInteractable p in AllItems())
        {
            if (p == null || p.IsCollected || p.Type != type || director == null || !director.IsReserved(p)) continue;
            if (stolen ? SimulateStolenCollect(p) : SimulatePlayerCollect(p)) taken++;
        }
        return taken;
    }

    // How many alive, reserved items of the type sit on reachable (not sealed) markers.
    private int CountReachable(PickupType type)
    {
        int reachable = 0;
        foreach (PickupInteractable p in AllItems())
        {
            if (p == null || p.Type != type || director == null || !director.IsReserved(p)) continue;
            Transform m = NearestMarker(p.transform.position);
            if (m != null && director.IsReachableMarker(m.name)) reachable++;
        }
        return reachable;
    }

    // The first item that was not in the given snapshot - a director replacement.
    private static PickupInteractable AnyNewItem(HashSet<PickupInteractable> known)
    {
        foreach (PickupInteractable p in AllItems())
            if (p != null && !known.Contains(p)) return p;
        return null;
    }

    // Makes the world deterministic before a test that asserts exact counts: put stats on the
    // normal plateau, collect everything, and let one full burst of replacements fire. Every
    // type that had enough collected items tops back up to its cap, so nothing is left under
    // cap diverting the next test's replacements.
    private IEnumerator NormalizeSupply(PlayerStats player)
    {
        SetAllFracsNormal(player);
        foreach (PickupType t in new[] { PickupType.Health, PickupType.Ammo, PickupType.Sanity })
            CollectAll(t, false);
        yield return new WaitForSeconds(expectedPlayerDelay + 2f);
    }

    // Drives one stat to a fraction of its maximum (up or down), so a cap plateau is exact.
    private void SetStatFrac(PlayerStats p, PickupType type, float frac)
    {
        switch (type)
        {
            case PickupType.Health:
            {
                float target = p.MaxHealth * frac;
                if (p.Health > target) p.TakeDamage(new DamageInfo(p.Health - target, gameObject, p.transform.position));
                else if (p.Health < target) p.Heal(target - p.Health);
                break;
            }
            case PickupType.Sanity:
            {
                float target = p.MaxSanity * frac;
                if (p.Sanity > target) p.DrainSanity(p.Sanity - target);
                else if (p.Sanity < target) p.AddSanity(target - p.Sanity);
                break;
            }
            case PickupType.Ammo:
            {
                int target = Mathf.RoundToInt(p.MaxReserveAmmo * frac);
                if (p.ReserveAmmo > target) p.TryUseAmmo(p.ReserveAmmo - target);
                else if (p.ReserveAmmo < target) p.AddAmmo(target - p.ReserveAmmo);
                break;
            }
        }
    }

    // Every stat comfortably above the very-low threshold: all caps sit at the normal count (2).
    private void SetAllFracsNormal(PlayerStats p)
    {
        SetStatFrac(p, PickupType.Health, 0.6f);
        SetStatFrac(p, PickupType.Sanity, 0.6f);
        SetStatFrac(p, PickupType.Ammo, 0.6f);
    }

    private static Transform NewSyntheticMarker(string name)
    {
        // Synthetic markers never start with "SP_S", so no marker scan in the suite can pick them up.
        var go = new GameObject(name);
        go.transform.position = new Vector3(9999f, 0f, 9999f);
        return go.transform;
    }

    // The reserved marker for an item position: the nearest SP_S marker within 1.5m.
    private static Transform NearestMarker(Vector3 pos, float maxDistance = 1.5f)
    {
        Transform best = null;
        float bestDistance = maxDistance;
        foreach (Transform t in FindMarkers())
        {
            float d = Vector3.Distance(t.position, pos);
            if (d < bestDistance) { bestDistance = d; best = t; }
        }
        return best;
    }

    private static PlayerStats FindPlayer() => FindFirstObjectByType<PlayerStats>();

    private static Transform[] FindMarkers()
    {
        var list = new List<Transform>();
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            if (t.name.StartsWith("SP_S")) list.Add(t);
        return list.ToArray();
    }

    private void Log(string message) => Debug.Log($"[ItemSpawnDirectorTest] {message}", this);
}
