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
 * the director. RunAllTests runs everything in a sensible order (about 1.5 minutes).
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

    [ContextMenu("Run All Tests")]
    public void RunAllTests() => Run(TestAllRoutine(), "AllTests");

    // The full suite, in an order that leaves the scene in a workable state
    private IEnumerator TestAllRoutine()
    {
        yield return Test_InitialPlacementRoutine();
        yield return Test_DynamicCapsRoutine();
        yield return Test_CapEnforcementRoutine();
        yield return Test_DroppedItemKeepsSpotRoutine();
        yield return Test_PlayerCollectRespawnRoutine();
        yield return Test_StolenRespawnRoutine();
        yield return Test_DistanceWindowRoutine();
    }

    // The tests

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
    }

    // A player pickup (TryClaim + Remove) frees the spot and brings a replacement after ~10s.
    // The player is pushed to low stats first so every cap sits at its maximum and the respawn
    // is guaranteed; the original stats are restored afterwards. Arrival is tracked on the total
    // supply, because a replacement may be any type that is under its cap.
    private IEnumerator Test_PlayerCollectRespawnRoutine()
    {
        if (!Guard()) yield break;
        PlayerStats player = FindPlayer();
        if (!Require(player != null, "a PlayerStats exists")) yield break;

        PickupInteractable item = FindAnyItem();
        if (!Require(item != null, "an item exists to collect")) yield break;

        PickupType type = item.Type;
        int before = director.AliveCount(type);
        int totalBefore = TotalAlive();
        PlayerSnapshot snapshot = SnapshotPlayer(player);
        PushPlayerToLowStats(player);
        float t0 = Time.time;

        if (!Require(SimulatePlayerCollect(item), "the item accepted the player collection")) yield break;
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
    // Stats are pushed low first for the same reason as above.
    private IEnumerator Test_StolenRespawnRoutine()
    {
        if (!Guard()) yield break;
        PlayerStats player = FindPlayer();
        if (!Require(player != null, "a PlayerStats exists")) yield break;

        PickupInteractable item = FindAnyItem();
        if (!Require(item != null, "an item exists to be stolen")) yield break;

        PickupType type = item.Type;
        int before = director.AliveCount(type);
        int totalBefore = TotalAlive();
        PlayerSnapshot snapshot = SnapshotPlayer(player);
        PushPlayerToLowStats(player);
        float t0 = Time.time;

        if (!Require(SimulateStolenCollect(item), "the item accepted the steal")) yield break;
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

    // With the player healed to full (so the cap sits at its minimum), draining a type and
    // collecting everything lets only cap-many replacements through - never the full count back.
    private IEnumerator Test_CapEnforcementRoutine()
    {
        if (!Guard()) yield break;
        PlayerStats player = FindPlayer();
        if (!Require(player != null, "a PlayerStats exists")) yield break;

        player.Heal(player.MaxHealth);
        int capAtFull = director.CurrentCap(PickupType.Health);

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
                "lower healthCap.min to make the check bite harder");
    }

    // CurrentCap follows the player's health: minimum at full health, higher when badly hurt.
    private IEnumerator Test_DynamicCapsRoutine()
    {
        if (!Guard()) yield break;
        PlayerStats player = FindPlayer();
        if (!Require(player != null, "a PlayerStats exists")) yield break;

        float originalHealth = player.Health;

        player.Heal(player.MaxHealth);
        int capFull = director.CurrentCap(PickupType.Health);

        player.TakeDamage(new DamageInfo(player.MaxHealth * 0.9f, gameObject, player.transform.position));
        int capHurt = director.CurrentCap(PickupType.Health);

        Check(capHurt >= capFull,
            $"health cap tracks need: {capFull} at full health, {capHurt} at 10% health");

        // Put the health back so later tests see the original state.
        if (player.Health < originalHealth) player.Heal(originalHealth - player.Health);
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
    // even when the player stands right on a marker. Stats are pushed low first so the respawn is
    // guaranteed (at full stats the caps can correctly block it).
    private IEnumerator Test_DistanceWindowRoutine()
    {
        if (!Guard()) yield break;
        PlayerStats player = FindPlayer();
        if (!Require(player != null, "a PlayerStats exists")) yield break;

        var items = new HashSet<PickupInteractable>();
        foreach (PickupInteractable p in AllItems())
            if (p != null) items.Add(p);
        if (!Require(items.Count > 0, "there is an item to collect")) yield break;

        // Park the player on some marker, so nearby spots are obviously too close.
        Transform[] markers = FindMarkers();
        if (!Require(markers.Length > 0, "there are markers in the scene")) yield break;
        Transform target = markers[UnityEngine.Random.Range(0, markers.Length)];

        PlayerSnapshot snapshot = SnapshotPlayer(player);
        PushPlayerToLowStats(player);

        Vector3 originalPosition = player.transform.position;
        CharacterController controller = player.GetComponentInChildren<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.position = target.position;
        if (controller != null) controller.enabled = true;

        PickupInteractable collected = FindAnyItem();
        int totalBefore = TotalAlive();
        SimulatePlayerCollect(collected);

        bool arrived = false;
        yield return WaitUntil(() => TotalAlive() >= totalBefore, expectedPlayerDelay + 5f, v => arrived = v);
        Check(arrived, $"a replacement appeared while the player was parked on a marker");

        if (arrived)
        {
            PickupInteractable replacement = null;
            foreach (PickupInteractable p in AllItems())
                if (p != null && !items.Contains(p)) { replacement = p; break; }

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
        RestorePlayer(player, snapshot);
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

    // Drives the player to near-empty stats so every dynamic cap sits at its maximum and a
    // replacement is always allowed; the original values can be restored afterwards.
    private struct PlayerSnapshot
    {
        public float Health;
        public float Sanity;
        public int Ammo;
    }

    private static PlayerSnapshot SnapshotPlayer(PlayerStats p)
        => new PlayerSnapshot { Health = p.Health, Sanity = p.Sanity, Ammo = p.ReserveAmmo };

    private void PushPlayerToLowStats(PlayerStats p)
    {
        if (p.Health > 1f) p.TakeDamage(new DamageInfo(p.Health - 1f, gameObject, p.transform.position));
        if (p.Sanity > 0f) p.DrainSanity(p.Sanity);
        if (p.ReserveAmmo > 0) p.TryUseAmmo(p.ReserveAmmo);
    }

    private static void RestorePlayer(PlayerStats p, PlayerSnapshot s)
    {
        if (p.Health < s.Health) p.Heal(s.Health - p.Health);
        if (p.Sanity < s.Sanity) p.AddSanity(s.Sanity - p.Sanity);
        if (p.ReserveAmmo < s.Ammo) p.AddAmmo(s.Ammo - p.ReserveAmmo);
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
