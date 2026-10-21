using System.Collections.Generic;
using UnityEngine;

/*
 * Chooses where the Hunter looks while it is in the Search state.
 * Begin() collects the Normal nodes within SearchRadius of the searched spot BY PATH (so a room on the other side
 * of a locked door does not count as near), and TryPickNext() hands them out one at a time in random order, each
 * only once. It never moves the Hunter: HunterBrain walks to the point it is given.
 */
public sealed class HunterSearch
{
    private readonly HunterNavigator navigator;
    private readonly HunterSettings settings;
    private readonly List<WaypointNode> candidates = new List<WaypointNode>();

    /// <summary>How many search points have not been visited yet.</summary>
    public int Remaining => candidates.Count;

    public HunterSearch(HunterNavigator navigator, HunterSettings settings)
    {
        this.navigator = navigator;
        this.settings = settings;
    }

    /// <summary>Starts a new search around a spot.</summary>
    public void Begin(Vector3 center)
    {
        candidates.Clear();

        foreach (WaypointNode node in navigator.NodesWithin(center, settings.SearchRadius))
        {
            if (node != null && node.Type == WaypointType.Normal) candidates.Add(node);
        }
    }

    /// <summary>The next point to look at, or false when every nearby point has been used.</summary>
    public bool TryPickNext(out Vector3 point)
    {
        point = default;
        if (candidates.Count == 0) return false;

        int i = Random.Range(0, candidates.Count);
        point = candidates[i].Position;
        candidates.RemoveAt(i);
        return true;
    }
}
