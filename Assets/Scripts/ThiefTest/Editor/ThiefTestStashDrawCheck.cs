using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/*
 * Test-only check of the Thief's stash draw, run from Tools > Thief Test > Check Stash Draw without entering Play mode.
 * It opens the Thief test scene, and for several fixed starting nodes builds the candidate stashes with the Thief's own
 * PlanRoute and RouteLength, calls the Thief's real ChooseStash many times and compares how often each stash wins with
 * the chance the Thief reports for it. A chi-square value per situation measures the gap: with four stashes (3 degrees
 * of freedom) anything under 7.81 matches the reported chances at the 5% level.
 * The skip of the last stash used happens before ChooseStash and is not part of this check.
 * The Thief's stash code is private, so this reaches it through reflection and changes nothing in ThiefAgent.
 * The scene that was open is reopened afterwards; the test scene is never saved.
 */
public static class ThiefTestStashDrawCheck
{
    private const string ScenePath = "Assets/Scenes/Sandbox/Thief_Test.unity";
    private const int Draws = 5000;
    private const int Situations = 6;
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    // Opens the Thief test scene, runs the check, logs the result and reopens the scene that was open before.
    [MenuItem("Tools/Thief Test/Check Stash Draw")]
    public static void RunFromMenu()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Stash draw check: leave Play mode first.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string previousScene = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        try
        {
            var thief = UnityEngine.Object.FindAnyObjectByType<ThiefAgent>();
            var graph = UnityEngine.Object.FindAnyObjectByType<WaypointGraph>();
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerStats>();

            Prepare(thief, graph, player.transform.position);
            Debug.Log($"Stash draw check: {thief.StashCount} stashes, {graph.Nodes.Count} nodes, player at {player.transform.position}\n"
                      + Run(thief, Draws, Situations));
        }
        finally
        {
            if (!string.IsNullOrEmpty(previousScene)) EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
        }
    }

    // In edit mode nothing has run Awake or Start, so the graph's node list and the Thief's stash list are filled here.
    public static void Prepare(ThiefAgent thief, WaypointGraph graph, Vector3 playerPosition)
    {
        Type agentType = typeof(ThiefAgent);
        typeof(WaypointGraph).GetMethod("Awake", Private).Invoke(graph, null);
        agentType.GetField("graph", Private).SetValue(thief, graph);
        agentType.GetMethod("FindHunterSpawnNodes", Private).Invoke(thief, null);
        agentType.GetMethod("FindStashes", Private).Invoke(thief, null);
        agentType.GetProperty("LastPlayerSample", Private).SetValue(thief, new PlayerSnapshot { Position = playerPosition });
    }

    /*
     * For each situation (a starting node spread evenly through the graph), builds the candidates as GoToStash does,
     * draws a stash the given number of times and reports, per stash, the reported chance against the share of wins,
     * with the chi-square value of the difference.
     */
    public static string Run(ThiefAgent thief, int draws, int situations)
    {
        Type agentType = typeof(ThiefAgent);
        Type optionType = agentType.GetNestedType("StashOption", BindingFlags.NonPublic);
        Type stashType = agentType.GetNestedType("Stash", BindingFlags.NonPublic);

        var graph = (WaypointGraph)agentType.GetField("graph", Private).GetValue(thief);
        var stashes = (IList)agentType.GetField("stashes", Private).GetValue(thief);
        MethodInfo planRoute = agentType.GetMethod("PlanRoute", Private);
        MethodInfo routeLength = agentType.GetMethod("RouteLength", BindingFlags.NonPublic | BindingFlags.Static);
        MethodInfo chooseStash = agentType.GetMethod("ChooseStash", Private);
        FieldInfo chancesField = agentType.GetField("stashChances", Private);

        var report = new StringBuilder();
        IReadOnlyList<WaypointNode> nodes = graph.Nodes;

        for (int s = 0; s < situations; s++)
        {
            WaypointNode start = nodes[(s * nodes.Count) / situations];

            // Every stash with a route from the start node, as GoToStash collects them.
            Type listType = typeof(List<>).MakeGenericType(optionType);
            var candidates = (IList)Activator.CreateInstance(listType);
            var candidateStashes = new List<int>();
            for (int i = 0; i < stashes.Count; i++)
            {
                var node = (WaypointNode)stashType.GetField("Node").GetValue(stashes[i]);
                var route = (List<WaypointNode>)planRoute.Invoke(thief, new object[] { start, node });
                if (route.Count == 0) continue;

                object option = Activator.CreateInstance(optionType);
                optionType.GetField("Index").SetValue(option, i);
                optionType.GetField("Route").SetValue(option, route);
                optionType.GetField("Length").SetValue(option, routeLength.Invoke(null, new object[] { route }));
                candidates.Add(option);
                candidateStashes.Add(i);
            }

            // The chances are the same on every draw for a fixed situation, so the first draw's are kept as the expectation.
            var wins = new int[stashes.Count];
            float[] expected = null;
            for (int d = 0; d < draws; d++)
            {
                object chosen = chooseStash.Invoke(thief, new object[] { candidates });
                wins[(int)optionType.GetField("Index").GetValue(chosen)]++;
                if (expected == null) expected = (float[])((float[])chancesField.GetValue(thief)).Clone();
            }

            double chiSquare = 0;
            report.Append($"Situation {s + 1} (start node {start.name}):");
            foreach (int i in candidateStashes)
            {
                double expectedWins = expected[i] * draws;
                if (expectedWins > 0) chiSquare += (wins[i] - expectedWins) * (wins[i] - expectedWins) / expectedWins;
                report.Append($"  S{i + 1} expected {expected[i]:0.0%} got {(double)wins[i] / draws:0.0%}");
            }
            report.AppendLine($"  | chi-square {chiSquare:0.00} (df {candidateStashes.Count - 1})");
        }

        return report.ToString();
    }
}
