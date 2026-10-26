using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/*
 * Test-only check of the Thief's item choice, run from Tools > Thief Test > Check Item Choice without entering Play mode.
 * Item choice has no randomness (the highest utility score always wins), so instead of repeated draws this sets up
 * fixed scenarios with known answers and confirms the Thief's real ChooseItem picks them. The fallback order is read
 * the way the Thief itself falls back when an item is unreachable: ChooseItem, remove the winner, ChooseItem again.
 * Each call rescales the distances by the largest among the items still in the list, so the fallback order can
 * differ from the order of the first pass's scores.
 * The Thief stands at the origin and the player 14 m away along X. Each item is placed where its distance from the
 * player and its distance from the Thief meet, so the distances are exact.
 * Scenarios:
 *   1. The README's worked example: the Ammo Box wins (0.70 against 0.61 and 0.56). With the Ammo Box out of reach,
 *      the other two are rescored on their own and the Sanity Bottle comes next (0.60 against 0.55), then the Medkit.
 *   2. The same items with the player moved away from the Medkit: the Medkit becomes the target (0.74 against 0.70).
 *   3. Need decides between two items at mirrored, equal distances: the one whose stat is low wins, both ways round.
 *   4. Two Medkits the same distance from the Thief: the one further from the player wins.
 *   5. Two identical Medkits at mirrored positions: the first found keeps the tie, whichever order they are listed in.
 * Expected answers assume the weights 0.5 / 0.3 / 0.2; the weights in the scene are read and reported first.
 * The Thief's item choice is private, so this reaches it through reflection and changes nothing in ThiefAgent.
 * The Thief is moved and the items are temporary, the test scene is never saved, and the scene that was open is
 * reopened afterwards.
 */
public static class ThiefTestItemChoiceCheck
{
    private const string ScenePath = "Assets/Scenes/Sandbox/Thief_Test.unity";
    private const float PlayerToThief = 14f;
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    private static readonly Vector3 ThiefPosition = Vector3.zero;
    private static readonly Vector3 PlayerPosition = new Vector3(PlayerToThief, 0f, 0f);

    // Opens the Thief test scene, runs every scenario, logs the results and reopens the scene that was open before.
    [MenuItem("Tools/Thief Test/Check Item Choice")]
    public static void RunFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Item choice check: leave Play mode first.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string previousScene = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var created = new List<GameObject>();
        try
        {
            var thief = Object.FindAnyObjectByType<ThiefAgent>();
            thief.transform.position = ThiefPosition;
            Debug.Log("Item choice check:\n" + Run(thief, created));
        }
        finally
        {
            foreach (GameObject go in created) Object.DestroyImmediate(go);
            if (!string.IsNullOrEmpty(previousScene)) EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }
    }

    // Runs the five scenarios against the given Thief and returns one PASS or FAIL line per check.
    public static string Run(ThiefAgent thief, List<GameObject> created)
    {
        var report = new StringBuilder();
        report.AppendLine($"Weights in the scene: far from player {Weight(thief, "playerDistanceWeight")}, " +
                          $"need {Weight(thief, "needWeight")}, close to Thief {Weight(thief, "thiefDistanceWeight")} (expected 0.5 / 0.3 / 0.2)");

        // 1. The README's worked example: 20/100 health, full sanity, 40/120 ammo.
        PlayerSnapshot readme = Sample(PlayerPosition, 0.8f, 0f, 1f - 40f / 120f);
        PickupInteractable medkit = Item(created, "Medkit", PickupType.Health, 10f, 6f, 1f);
        PickupInteractable ammo = Item(created, "Ammo Box", PickupType.Ammo, 20f, 15f, 1f);
        PickupInteractable sanity = Item(created, "Sanity Bottle", PickupType.Sanity, 16f, 3f, 1f);
        var exampleItems = new List<PickupInteractable> { medkit, ammo, sanity };
        Check(report, "1a. README worked example picks the Ammo Box",
              Name(Choose(thief, exampleItems, readme)), "Ammo Box");
        Check(report, "1b. Fallback order, rescored after each removal",
              Ranking(thief, exampleItems, readme), "Ammo Box, Sanity Bottle, Medkit");

        // 2. The player moves away from the Medkit, which then becomes the target.
        PlayerSnapshot movedAway = Sample(new Vector3(-10f, 0f, -5f), 0.8f, 0f, 1f - 40f / 120f);
        Check(report, "2. Player moved away from the Medkit makes it the target",
              Ranking(thief, exampleItems, movedAway).Split(',')[0], "Medkit");

        // 3. Need decides between a Sanity Bottle and an Ammo Box at mirrored, equal distances.
        PickupInteractable sanityA = Item(created, "Sanity Bottle", PickupType.Sanity, 12f, 8f, 1f);
        PickupInteractable ammoB = Item(created, "Ammo Box", PickupType.Ammo, 12f, 8f, -1f);
        var needItems = new List<PickupInteractable> { sanityA, ammoB };
        Check(report, "3a. Low sanity picks the Sanity Bottle",
              Name(Choose(thief, needItems, Sample(PlayerPosition, 0f, 0.9f, 0f))), "Sanity Bottle");
        Check(report, "3b. Low ammo picks the Ammo Box",
              Name(Choose(thief, needItems, Sample(PlayerPosition, 0f, 0f, 0.9f))), "Ammo Box");

        // 4. Two Medkits 8 m from the Thief, one 18 m and one 10 m from the player: the further one wins.
        PickupInteractable farMedkit = Item(created, "Medkit far from player", PickupType.Health, 18f, 8f, 1f);
        PickupInteractable nearMedkit = Item(created, "Medkit near player", PickupType.Health, 10f, 8f, -1f);
        Check(report, "4. Equal need and Thief distance picks the item further from the player",
              Name(Choose(thief, new List<PickupInteractable> { nearMedkit, farMedkit }, Sample(PlayerPosition, 0.5f, 0f, 0f))),
              "Medkit far from player");

        // 5. Two identical Medkits at mirrored positions: the first in the list keeps the tie, in either order.
        PickupInteractable twinA = Item(created, "Medkit A", PickupType.Health, 12f, 8f, 1f);
        PickupInteractable twinB = Item(created, "Medkit B", PickupType.Health, 12f, 8f, -1f);
        PlayerSnapshot even = Sample(PlayerPosition, 0.5f, 0f, 0f);
        Check(report, "5a. Tie keeps the first item listed (A first)",
              Name(Choose(thief, new List<PickupInteractable> { twinA, twinB }, even)), "Medkit A");
        Check(report, "5b. Tie keeps the first item listed (B first)",
              Name(Choose(thief, new List<PickupInteractable> { twinB, twinA }, even)), "Medkit B");

        return report.ToString();
    }

    // Writes one PASS or FAIL line comparing what the Thief chose with the expected answer.
    private static void Check(StringBuilder report, string label, string actual, string expected)
    {
        report.AppendLine($"{(actual == expected ? "PASS" : "FAIL")} {label}: got {actual}, expected {expected}");
    }

    // The order the Thief falls back through when items are unreachable: its choice, then its choice among the rest, and so on.
    private static string Ranking(ThiefAgent thief, List<PickupInteractable> items, PlayerSnapshot sample)
    {
        var remaining = new List<PickupInteractable>(items);
        var order = new List<string>();
        while (remaining.Count > 0)
        {
            PickupInteractable best = Choose(thief, remaining, sample);
            order.Add(Name(best));
            remaining.Remove(best);
        }

        return string.Join(", ", order);
    }

    // Calls the Thief's real ChooseItem with a copy of the list, so the caller's list is never changed.
    private static PickupInteractable Choose(ThiefAgent thief, List<PickupInteractable> items, PlayerSnapshot sample)
    {
        MethodInfo chooseItem = typeof(ThiefAgent).GetMethod("ChooseItem", Private);
        return (PickupInteractable)chooseItem.Invoke(thief, new object[] { new List<PickupInteractable>(items), sample });
    }

    /*
     * A temporary supply of the given type, placed where its flat distance from the player and from the Thief are the
     * given values. side picks which of the two such points (either side of the line between them) is used.
     */
    private static PickupInteractable Item(List<GameObject> created, string name, PickupType type,
                                           float fromPlayer, float fromThief, float side)
    {
        float x = (fromThief * fromThief - fromPlayer * fromPlayer + PlayerToThief * PlayerToThief) / (2f * PlayerToThief);
        float z = side * Mathf.Sqrt(Mathf.Max(0f, fromThief * fromThief - x * x));

        var go = new GameObject(name);
        go.transform.position = ThiefPosition + new Vector3(x, 0f, z);
        created.Add(go);

        var item = go.AddComponent<PickupInteractable>();
        typeof(PickupInteractable).GetField("type", Private).SetValue(item, type);
        return item;
    }

    // A player sample at the given position with the given needs (0 = stat full, 1 = stat empty).
    private static PlayerSnapshot Sample(Vector3 position, float healthNeed, float sanityNeed, float ammoNeed)
    {
        return new PlayerSnapshot { Position = position, HealthNeed = healthNeed, SanityNeed = sanityNeed, AmmoNeed = ammoNeed };
    }

    private static string Name(PickupInteractable item) => item != null ? item.name : "nothing";

    private static float Weight(ThiefAgent thief, string field) => (float)typeof(ThiefAgent).GetField(field, Private).GetValue(thief);
}
