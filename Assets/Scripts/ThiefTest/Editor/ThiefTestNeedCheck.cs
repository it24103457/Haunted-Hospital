using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/*
 * Test-only check of how the Thief judges what the player needs, run from Tools > Thief Test > Check Need without
 * entering Play mode. It has two parts.
 * Need values: the player's health, sanity, reserve ammo and loaded rounds are set to known amounts and the Thief's real
 * SamplePlayer reads them, so the rule for each stat is checked end to end: health and sanity count any shortfall
 * against their maximum, while ammo (reserve plus loaded) only counts below the comfortable amount.
 * Pick rates: for each situation, thousands of random layouts (player, Thief and items anywhere in a 60 by 40 m area)
 * are passed to the Thief's real ChooseItem with the needs read the same way, and the share of picks that go to the
 * most-needed supply is reported. Item counts follow the Item Spawn Director: 2 of each supply, but only 1 of a supply
 * whose stat is below 30% of its maximum. With one supply needed, a situation passes when that supply wins at least
 * 70% of the time; the rest go to items far safer from the player, which the distance weights are meant to allow.
 * With more than one supply needed, the Thief is meant to split between them, so a situation passes when the
 * most-needed supply wins most often and the needed supplies together win at least 90% of the time.
 * With nothing needed, the picks should spread evenly, so that situation passes when every supply gets 33% +/- 5%.
 * The Thief's need reading and item choice are private, so this reaches them through reflection and changes nothing
 * in ThiefAgent, PlayerStats or PlayerWeapon. The player's stats are only changed in the unsaved test scene, the items
 * are temporary, and the scene that was open is reopened afterwards.
 */
public static class ThiefTestNeedCheck
{
    private const string ScenePath = "Assets/Scenes/Sandbox/Thief_Test.unity";
    private const int Layouts = 5000;
    private const float PassShare = 0.7f;
    private const float NeededPassShare = 0.9f;
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly Vector2 Area = new Vector2(60f, 40f);

    // Opens the Thief test scene, runs both parts, logs the results and reopens the scene that was open before.
    [MenuItem("Tools/Thief Test/Check Need")]
    public static void RunFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Need check: leave Play mode first.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string previousScene = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var created = new List<GameObject>();
        try
        {
            var thief = Object.FindAnyObjectByType<ThiefAgent>();
            var stats = Object.FindAnyObjectByType<PlayerStats>();
            var weapon = stats.GetComponent<PlayerWeapon>();

            // In edit mode the Thief's Awake has not run, so its player and weapon references are filled the way Awake fills them.
            typeof(ThiefAgent).GetField("player", Private).SetValue(thief, stats);
            typeof(ThiefAgent).GetField("playerWeapon", Private).SetValue(thief, weapon);

            Debug.Log("Need check:\n" + NeedValues(thief, stats, weapon) + PickRates(thief, stats, weapon, created));
        }
        finally
        {
            foreach (GameObject go in created) Object.DestroyImmediate(go);
            if (!string.IsNullOrEmpty(previousScene)) EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }
    }

    /*
     * Sets the player to each case, reads the needs through SamplePlayer and compares them with the expected values,
     * one PASS or FAIL line per case. The comfortable amount is read from the Thief so the expectations follow it.
     */
    private static string NeedValues(ThiefAgent thief, PlayerStats stats, PlayerWeapon weapon)
    {
        int comfortable = (int)typeof(ThiefAgent).GetField("comfortableAmmo", Private).GetValue(thief);
        var report = new StringBuilder();
        report.AppendLine($"Need values (comfortable ammo {comfortable}, health and sanity out of {stats.MaxHealth} and {stats.MaxSanity}):");

        var cases = new (string label, float health, float sanity, int reserve, int loaded, float h, float s, float a)[]
        {
            ("Fresh start: everything full, 40 reserve + 6 loaded", 100f, 100f, 40, 6, 0f, 0f, 0f),
            ("More ammo than the comfortable amount (80 + 6)", 100f, 100f, 80, 6, 0f, 0f, 0f),
            ("Ammo running low (10 + 2)", 100f, 100f, 10, 2, 0f, 0f, 1f - 12f / comfortable),
            ("Ammo empty (0 + 0)", 100f, 100f, 0, 0, 0f, 0f, 1f),
            ("Loaded rounds count: 40 reserve, empty gun", 100f, 100f, 40, 0, 0f, 0f, 1f - 40f / comfortable),
            ("Health 20%", 20f, 100f, 40, 6, 0.8f, 0f, 0f),
            ("Sanity 50%", 100f, 50f, 40, 6, 0f, 0.5f, 0f),
            ("Health 15%, sanity 20%, ammo 20 + 6", 15f, 20f, 20, 6, 0.85f, 0.8f, 1f - 26f / comfortable),
        };

        foreach (var c in cases)
        {
            SetPlayer(stats, weapon, c.health, c.sanity, c.reserve, c.loaded);
            PlayerSnapshot sample = Sample(thief);
            bool ok = Close(sample.HealthNeed, c.h) && Close(sample.SanityNeed, c.s) && Close(sample.AmmoNeed, c.a);
            report.AppendLine($"{(ok ? "PASS" : "FAIL")} {c.label}: health {sample.HealthNeed:0.00}, sanity {sample.SanityNeed:0.00}, " +
                              $"ammo {sample.AmmoNeed:0.00} (expected {c.h:0.00}, {c.s:0.00}, {c.a:0.00})");
        }

        return report.ToString();
    }

    /*
     * For each situation, sets the player, reads the needs through SamplePlayer once and then scores the given number of
     * random layouts with ChooseItem, counting which supply wins. Items are spawned per the Item Spawn Director's counts.
     */
    private static string PickRates(ThiefAgent thief, PlayerStats stats, PlayerWeapon weapon, List<GameObject> created)
    {
        var report = new StringBuilder();
        report.AppendLine($"Pick rates ({Layouts} random layouts each; one need: pass at {PassShare:0%} for that supply; " +
                          $"several needs: the most-needed wins most often and the needed ones together reach {NeededPassShare:0%}):");

        var situations = new (string label, float health, float sanity, int reserve, int loaded, PickupType? mostNeeded)[]
        {
            ("Health 20%, starting ammo", 20f, 100f, 40, 6, PickupType.Health),
            ("Health 50%, starting ammo", 50f, 100f, 40, 6, PickupType.Health),
            ("Sanity 25%, starting ammo", 100f, 25f, 40, 6, PickupType.Sanity),
            ("Health 20%, sanity 60%, starting ammo", 20f, 60f, 40, 6, PickupType.Health),
            ("Ammo nearly out (4 + 2), the rest full", 100f, 100f, 4, 2, PickupType.Ammo),
            ("Fresh start, nothing needed", 100f, 100f, 40, 6, null),
        };

        Random.InitState(26);
        foreach (var s in situations)
        {
            SetPlayer(stats, weapon, s.health, s.sanity, s.reserve, s.loaded);
            PlayerSnapshot sample = Sample(thief);

            List<PickupInteractable> items = SpawnItems(created, stats);
            var wins = new Dictionary<PickupType, int> { { PickupType.Health, 0 }, { PickupType.Sanity, 0 }, { PickupType.Ammo, 0 } };
            for (int i = 0; i < Layouts; i++)
            {
                thief.transform.position = RandomPoint();
                sample.Position = RandomPoint();
                foreach (PickupInteractable item in items) item.transform.position = RandomPoint();

                PickupInteractable chosen = Choose(thief, items, sample);
                wins[chosen.Type]++;
            }

            foreach (PickupInteractable item in items) item.gameObject.SetActive(false);

            float health = (float)wins[PickupType.Health] / Layouts;
            float sanity = (float)wins[PickupType.Sanity] / Layouts;
            float ammo = (float)wins[PickupType.Ammo] / Layouts;
            bool ok = s.mostNeeded.HasValue
                ? Passes(wins, sample, s.mostNeeded.Value)
                : Mathf.Abs(health - 1f / 3f) <= 0.05f && Mathf.Abs(sanity - 1f / 3f) <= 0.05f && Mathf.Abs(ammo - 1f / 3f) <= 0.05f;

            string counts = $"{Count(items, PickupType.Health)} Medkit, {Count(items, PickupType.Sanity)} Sanity, {Count(items, PickupType.Ammo)} Ammo";
            report.AppendLine($"{(ok ? "PASS" : "FAIL")} {s.label} [{counts}; needs {sample.HealthNeed:0.00}/{sample.SanityNeed:0.00}/{sample.AmmoNeed:0.00}]: " +
                              $"Medkit {health:0.0%}, Sanity Bottle {sanity:0.0%}, Ammo Box {ammo:0.0%}");
        }

        return report.ToString();
    }

    /*
     * The pass rule for a situation with a most-needed supply. With one supply needed, it must win at least PassShare of
     * the layouts. With several needed, it must win more often than any other supply, and the needed supplies together
     * must win at least NeededPassShare of them.
     */
    private static bool Passes(Dictionary<PickupType, int> wins, PlayerSnapshot sample, PickupType mostNeeded)
    {
        var needs = new Dictionary<PickupType, float>
        {
            { PickupType.Health, sample.HealthNeed }, { PickupType.Sanity, sample.SanityNeed }, { PickupType.Ammo, sample.AmmoNeed }
        };

        int neededCount = 0;
        int neededWins = 0;
        foreach (var pair in needs)
        {
            if (pair.Value <= 0f) continue;
            neededCount++;
            neededWins += wins[pair.Key];
        }

        if (neededCount <= 1) return (float)wins[mostNeeded] / Layouts >= PassShare;

        foreach (var pair in wins)
            if (pair.Key != mostNeeded && pair.Value >= wins[mostNeeded]) return false;

        return (float)neededWins / Layouts >= NeededPassShare;
    }

    /*
     * Temporary supplies in the counts the Item Spawn Director allows for the player's current stats: 2 of each, but 1 of
     * a supply whose stat is below 30% of its maximum (ammo judged on reserve rounds against the reserve maximum, as the
     * Item Spawn Director judges it).
     */
    private static List<PickupInteractable> SpawnItems(List<GameObject> created, PlayerStats stats)
    {
        var items = new List<PickupInteractable>();
        AddItems(items, created, PickupType.Health, stats.Health / stats.MaxHealth < 0.3f ? 1 : 2);
        AddItems(items, created, PickupType.Sanity, stats.Sanity / stats.MaxSanity < 0.3f ? 1 : 2);
        AddItems(items, created, PickupType.Ammo, (float)stats.ReserveAmmo / stats.MaxReserveAmmo < 0.3f ? 1 : 2);
        return items;
    }

    // Adds the given number of temporary supplies of one type, each a new object with a PickupInteractable of that type.
    private static void AddItems(List<PickupInteractable> items, List<GameObject> created, PickupType type, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"{type} {i + 1}");
            created.Add(go);

            var item = go.AddComponent<PickupInteractable>();
            typeof(PickupInteractable).GetField("type", Private).SetValue(item, type);
            items.Add(item);
        }
    }

    // Sets the player's health, sanity, reserve ammo and loaded rounds through their private setters.
    private static void SetPlayer(PlayerStats stats, PlayerWeapon weapon, float health, float sanity, int reserve, int loaded)
    {
        typeof(PlayerStats).GetProperty("Health").SetValue(stats, health);
        typeof(PlayerStats).GetProperty("Sanity").SetValue(stats, sanity);
        typeof(PlayerStats).GetProperty("ReserveAmmo").SetValue(stats, reserve);
        if (weapon != null) typeof(PlayerWeapon).GetProperty("Magazine").SetValue(weapon, loaded);
    }

    // The player's needs as the Thief reads them at a decision, from its real SamplePlayer.
    private static PlayerSnapshot Sample(ThiefAgent thief)
    {
        return (PlayerSnapshot)typeof(ThiefAgent).GetMethod("SamplePlayer", Private).Invoke(thief, null);
    }

    // Calls the Thief's real ChooseItem with a copy of the list, so the caller's list is never changed.
    private static PickupInteractable Choose(ThiefAgent thief, List<PickupInteractable> items, PlayerSnapshot sample)
    {
        MethodInfo chooseItem = typeof(ThiefAgent).GetMethod("ChooseItem", Private);
        return (PickupInteractable)chooseItem.Invoke(thief, new object[] { new List<PickupInteractable>(items), sample });
    }

    // A random point on the floor of the test area.
    private static Vector3 RandomPoint() => new Vector3(Random.Range(0f, Area.x), 0f, Random.Range(0f, Area.y));

    // How many of the items are of the given type.
    private static int Count(List<PickupInteractable> items, PickupType type) => items.FindAll(i => i.Type == type).Count;

    // True when two needs match to within rounding.
    private static bool Close(float a, float b) => Mathf.Abs(a - b) < 0.005f;
}
