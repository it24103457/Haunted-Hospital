using System;
using System.Collections.Generic;
using UnityEngine;

public enum HunterNavStatus
{
    Idle,         // no walk in progress
    Walking,      // following a route
    OpeningDoor,  // standing in front of a closed door, waiting for it to open
    Arrived,      // reached the end of the route
    Failed        // could not get there (blocked, door would not open, or stuck)
}

/*
 * Walks the Hunter along a route from HunterPathfinder. In front of a closed door it stops, opens the door,
 * waits until it is fully open, then carries on. A door that will not open is avoided for a while and the route is
 * re-planned; after too many failures the walk fails. It also re-plans when a door or barricade changes.
 * It never moves the Hunter: it hands the points to walk to HunterBrain's path output. HunterBrain calls Tick().
 */
public sealed class HunterNavigator
{
    public HunterNavStatus Status { get; private set; } = HunterNavStatus.Idle;

    /// <summary>True when the route could only reach the nearest reachable node, not the place asked for.</summary>
    public bool IsPartialRoute { get; private set; }

    /// <summary>The planned route, for the debug view.</summary>
    public IReadOnlyList<WaypointNode> Route => route;

    /// <summary>Index in Route of the closed door the Hunter is heading for, or -1.</summary>
    public int HoldIndex => holdIndex;

    private readonly HunterSettings settings;
    private readonly WaypointGraph graph;
    private readonly Transform body;
    private readonly GameObject owner;
    private readonly Action<IReadOnlyList<Vector3>> publish;
    private readonly Func<Door, bool> isBanned;

    private readonly List<WaypointNode> route = new List<WaypointNode>();
    private readonly List<Vector3> points = new List<Vector3>();
    private readonly List<WaypointNode> gateNodes = new List<WaypointNode>();   // door and barricade nodes
    private readonly Dictionary<Door, float> bannedUntil = new Dictionary<Door, float>();
    private readonly Dictionary<Door, int> failures = new Dictionary<Door, int>();

    private Vector3 target;
    private bool allowPartial;
    private int holdIndex = -1;
    private Door pendingDoor;
    private float doorWaitStart;
    private int worldSignature;
    private float nextWorldCheck;
    private float bestRemaining;
    private float lastProgressTime;
    private int stuckReplans;

    public HunterNavigator(HunterSettings settings, WaypointGraph graph, Transform body, GameObject owner,
                           Action<IReadOnlyList<Vector3>> publish)
    {
        this.settings = settings;
        this.graph = graph;
        this.body = body;
        this.owner = owner;
        this.publish = publish;
        isBanned = IsBanned;

        foreach (WaypointNode node in graph.Nodes)
        {
            if (node != null && node.Type != WaypointType.Normal) gateNodes.Add(node);
        }
    }

    /// <summary>
    /// Plans a route to the node nearest the destination and starts walking it. Returns false (and does nothing)
    /// when there is no route, unless allowPartialRoute is set, in which case it walks to the nearest reachable node.
    /// </summary>
    public bool GoTo(Vector3 destination, bool allowPartialRoute)
    {
        target = destination;
        allowPartial = allowPartialRoute;
        failures.Clear();
        stuckReplans = 0;

        return Plan();
    }

    /// <summary>Stops walking and clears the path.</summary>
    public void Stop()
    {
        bool wasActive = Status == HunterNavStatus.Walking || Status == HunterNavStatus.OpeningDoor;

        Status = HunterNavStatus.Idle;
        pendingDoor = null;
        holdIndex = -1;
        points.Clear();

        if (wasActive) publish(null);
    }

    public void Tick()
    {
        if (Status == HunterNavStatus.Walking) TickWalking();
        else if (Status == HunterNavStatus.OpeningDoor) TickDoor();
    }

    // Planning

    private bool Plan()
    {
        route.Clear();

        WaypointNode start = graph.GetNearestNode(body.position);
        WaypointNode goal = graph.GetNearestNode(target);
        if (start == null || goal == null) return false;

        List<WaypointNode> found = HunterPathfinder.FindPath(start, goal, settings, isBanned);
        IsPartialRoute = false;

        if (found.Count == 0)
        {
            if (!allowPartial) return false;

            // Unreachable: go as near as the building allows.
            found = HunterPathfinder.FindPathToNearest(start, target, settings, isBanned);
            IsPartialRoute = true;
            if (found.Count == 0) return false;
        }

        route.AddRange(found);
        worldSignature = ComputeWorldSignature();
        nextWorldCheck = Time.time + settings.ReplanIntervalSeconds;
        pendingDoor = null;
        Status = HunterNavStatus.Walking;

        PublishFrom(FirstIndex());
        return true;
    }

    private void Replan()
    {
        if (!Plan()) Finish(HunterNavStatus.Failed);
    }

    // Skips the first node when the Hunter is already past it, so it does not walk back to it.
    private int FirstIndex()
    {
        if (route.Count < 2) return 0;

        bool closerToSecond = Flat(body.position, route[1].Position) < Flat(route[0].Position, route[1].Position);
        return closerToSecond ? 1 : 0;
    }

    // Publishes the points from startIndex up to the node in front of the next closed door (or the end of the route).
    private void PublishFrom(int startIndex)
    {
        holdIndex = -1;
        for (int i = Mathf.Max(startIndex, 1); i < route.Count; i++)
        {
            Door door = route[i].Type == WaypointType.Door ? route[i].Door : null;
            if (door == null || door.IsFullyOpen) continue;

            holdIndex = i;
            break;
        }

        int last = holdIndex >= 0 ? holdIndex - 1 : route.Count - 1;

        points.Clear();
        for (int i = startIndex; i <= last; i++) points.Add(route[i].Position);
        if (points.Count == 0) points.Add(route[Mathf.Max(last, 0)].Position);

        bestRemaining = Remaining();
        lastProgressTime = Time.time;
        publish(points);
    }

    // Walking

    private void TickWalking()
    {
        // A door or barricade changed: plan again from where the Hunter is.
        if (Time.time >= nextWorldCheck)
        {
            nextWorldCheck = Time.time + settings.ReplanIntervalSeconds;
            if (ComputeWorldSignature() != worldSignature)
            {
                Log("A door or barricade changed; planning the route again.");
                Replan();
                return;
            }
        }

        float remaining = Remaining();
        if (remaining <= settings.ArrivalRadius)
        {
            if (holdIndex >= 0) BeginOpeningDoor();
            else Finish(HunterNavStatus.Arrived);
            return;
        }

        if (remaining < bestRemaining - 0.25f)
        {
            bestRemaining = remaining;
            lastProgressTime = Time.time;
        }
        else if (Time.time - lastProgressTime > settings.StuckSeconds)
        {
            // Not getting anywhere: re-plan a couple of times, then give up.
            if (++stuckReplans > 2) Finish(HunterNavStatus.Failed);
            else Replan();
        }
    }

    // Doors

    private void BeginOpeningDoor()
    {
        pendingDoor = route[holdIndex].Door;
        doorWaitStart = Time.time;
        Status = HunterNavStatus.OpeningDoor;

        publish(null);               // stand still in front of the door
        pendingDoor.Open(owner);
        Log($"Waiting at door '{pendingDoor.name}' for it to open.");
    }

    private void TickDoor()
    {
        if (pendingDoor == null)
        {
            Replan();
            return;
        }

        if (pendingDoor.IsFullyOpen)
        {
            Log($"Door '{pendingDoor.name}' is open, carrying on.");
            failures.Remove(pendingDoor);
            pendingDoor = null;
            worldSignature = ComputeWorldSignature();   // the change was ours, so no need to re-plan for it
            Status = HunterNavStatus.Walking;
            PublishFrom(holdIndex);                     // carry on from the door itself
            return;
        }

        if (pendingDoor.IsLocked || Time.time - doorWaitStart > settings.DoorOpenTimeoutSeconds) DoorFailed();
    }

    // The door would not open in time: avoid it for a while and plan another way, but only so many times.
    private void DoorFailed()
    {
        Door door = pendingDoor;
        pendingDoor = null;

        failures.TryGetValue(door, out int count);
        failures[door] = ++count;
        bannedUntil[door] = Time.time + settings.DoorBanSeconds;
        Log($"Door '{door.name}' did not open (attempt {count} of {settings.DoorRetryLimit}); avoiding it for {settings.DoorBanSeconds:0.#} s.");

        if (count >= settings.DoorRetryLimit)
        {
            Finish(HunterNavStatus.Failed);
            return;
        }

        Replan();
    }

    private bool IsBanned(Door door)
    {
        return bannedUntil.TryGetValue(door, out float until) && Time.time < until;
    }

    // Helpers

    private void Finish(HunterNavStatus status)
    {
        if (status == HunterNavStatus.Failed) Log("Could not get there; giving up on this walk.");

        Status = status;
        pendingDoor = null;
        holdIndex = -1;
        points.Clear();
        publish(null);
    }

    // A number that changes whenever a door or barricade changes state (or a ban starts or ends).
    private int ComputeWorldSignature()
    {
        int hash = 17;

        foreach (WaypointNode node in gateNodes)
        {
            int state = 0;

            if (node.Type == WaypointType.Door && node.Door != null)
            {
                Door door = node.Door;
                state = (door.IsLocked ? 1 : 0) | (door.IsOpen ? 2 : 0) | (door.IsFullyOpen ? 4 : 0) | (IsBanned(door) ? 8 : 0);
            }
            else if (node.Type == WaypointType.Barricade)
            {
                state = node.IsBarricadeIntact ? 1 : 0;
            }

            hash = unchecked(hash * 31 + state);
        }

        return hash;
    }

    private void Log(string message)
    {
        if (settings.EnableDebugLogging) Debug.Log("[Hunter] " + message, owner);
    }

    private float Remaining() => Flat(points[points.Count - 1], body.position);

    private static float Flat(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}
