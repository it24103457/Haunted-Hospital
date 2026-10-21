using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * HunterMover:
 * Drives the Hunter's movement along the path published by HunterBrain.
 * Handles NavMeshAgent velocity/stopping via SpeedMultiplier, door facing when
 * HunterNavigator is opening a door, LookTarget tracking, and flinch stagger pauses.
 *
 * Anti-teleport design:
 *   - updatePosition is OFF: the NavMeshAgent simulates movement internally but we
 *     apply position changes ourselves via Vector3.MoveTowards, clamped to max speed.
 *     This makes it physically impossible for the Hunter to snap/teleport, regardless
 *     of what the NavMeshAgent's internal pathfinding does on replans.
 *   - On each FollowPath, picks the best forward waypoint using a simple
 *     closest-ahead algorithm rather than sequential pruning.
 *   - isDirectPursuing flag suppresses all waypoint dispatching during Chase
 *     with direct LOS to the player, preventing replan events from jerking the agent.
 *   - Smooth turning via exponential-damped Slerp (updateRotation permanently off).
 */
[RequireComponent(typeof(HunterBrain), typeof(NavMeshAgent))]
public class HunterMover : MonoBehaviour
{
    [Header("Movement Tuning")]
    [Tooltip("Distance at which an intermediate path point counts as reached and the next one is targeted.")]
    [SerializeField] private float passDistance = 0.5f;

    [Tooltip("Smoothing factor for turning (higher = snappier, lower = smoother organic deceleration).")]
    [SerializeField, Range(3f, 20f)] private float turnSmoothing = 9f;

    [Header("Position Smoothing")]
    [Tooltip("Extra multiplier on top of agent.speed for the max per-frame move distance. " +
             "1.1 = allow 10% overshoot so the agent doesn't lag behind the NavMesh simulation.")]
    [SerializeField] private float speedHeadroomMultiplier = 1.15f;

    [Header("Flinch Stagger")]
    [Tooltip("Seconds the Hunter pauses movement after taking non-lethal damage.")]
    [SerializeField] private float flinchStaggerSeconds = 0.35f;

    private HunterBrain hunter;
    private NavMeshAgent agent;
    private PlayerStats player;
    private float baseSpeed;
    private readonly List<Vector3> path = new List<Vector3>();
    private int index;
    private Coroutine staggerRoutine;
    private bool isStaggered;

    // True when we are actively pursuing the player directly (no waypoint path needed).
    private bool isDirectPursuing;

    // True when we have a valid destination dispatched to the NavMeshAgent.
    private bool hasActiveDestination;

    private void Awake()
    {
        hunter = GetComponent<HunterBrain>();
        agent = GetComponent<NavMeshAgent>();
        player = FindAnyObjectByType<PlayerStats>();
        baseSpeed = agent.speed;

        // We control both rotation and position manually:
        // - Rotation: smooth Slerp instead of NavMeshAgent's binary snap
        // - Position: MoveTowards from agent.nextPosition, clamped to max speed,
        //   which eliminates teleporting entirely.
        agent.updateRotation = false;
        agent.updatePosition = false;

        // Disable auto-repath at runtime too (in case the prefab value gets overridden)
        agent.autoRepath = false;

        // Ensure HunterTestMover is disabled if present so only one mover drives the agent
        var testMover = GetComponent("HunterTestMover") as MonoBehaviour;
        if (testMover != null && testMover.enabled)
        {
            testMover.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (hunter != null)
        {
            hunter.OnPathChanged += FollowPath;
            hunter.OnDamaged += HandleDamaged;
            hunter.OnDied += HandleDied;
            hunter.OnSpawned += HandleSpawned;
        }
    }

    private void OnDisable()
    {
        if (hunter != null)
        {
            hunter.OnPathChanged -= FollowPath;
            hunter.OnDamaged -= HandleDamaged;
            hunter.OnDied -= HandleDied;
            hunter.OnSpawned -= HandleSpawned;
        }

        if (staggerRoutine != null)
        {
            StopCoroutine(staggerRoutine);
            staggerRoutine = null;
        }
        isStaggered = false;
    }

    private void FollowPath(IReadOnlyList<Vector3> newPath)
    {
        // During Chase with a clear NavMesh line to the player, the direct-pursuit loop in
        // Update() already drives the agent. Ignore every path replan from HunterNavigator so
        // the agent is never yanked back to an old waypoint mid-chase.
        if (isDirectPursuing) return;

        path.Clear();
        if (newPath != null) path.AddRange(newPath);
        index = 0;

        if (!agent.isOnNavMesh) return;

        if (path.Count == 0)
        {
            agent.isStopped = true;
            hasActiveDestination = false;
            return;
        }

        // Find the best starting waypoint: the closest one that is roughly ahead of us.
        index = FindBestStartIndex();
        DispatchCurrentWaypoint();
    }

    /// <summary>
    /// Finds the best waypoint index to start walking toward.
    /// Prefers the closest forward point; falls back to the nearest point overall.
    /// </summary>
    private int FindBestStartIndex()
    {
        if (path.Count <= 1) return 0;

        int bestForwardIdx = -1;
        float bestForwardDist = float.MaxValue;
        int closestIdx = 0;
        float closestDist = float.MaxValue;

        Vector3 fwd = transform.forward;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 toPoint = path[i] - transform.position;
            toPoint.y = 0f;
            float dist = toPoint.magnitude;

            // Track the closest point overall (as a fallback)
            if (dist < closestDist)
            {
                closestDist = dist;
                closestIdx = i;
            }

            // Skip points we're already standing on
            if (dist <= passDistance && i < path.Count - 1) continue;

            // Prefer points that are ahead of us (dot > -0.15)
            float dot = dist > 0.01f ? Vector3.Dot(fwd, toPoint / dist) : 0f;
            if (dot > -0.15f && dist < bestForwardDist)
            {
                bestForwardDist = dist;
                bestForwardIdx = i;
            }
        }

        if (bestForwardIdx >= 0) return bestForwardIdx;

        // No forward point found (we're surrounded by backward points).
        // Use the closest point, or the last point as absolute fallback.
        return closestIdx;
    }

    private void DispatchCurrentWaypoint()
    {
        if (!agent.isOnNavMesh || path.Count == 0) return;

        Vector3 target = path[index];
        agent.isStopped = false;
        agent.SetDestination(target);
        hasActiveDestination = true;
    }

    private void HandleDamaged(DamageInfo info)
    {
        if (!hunter.IsAlive) return;

        if (staggerRoutine != null) StopCoroutine(staggerRoutine);
        staggerRoutine = StartCoroutine(StaggerPause());
    }

    private IEnumerator StaggerPause()
    {
        isStaggered = true;
        yield return new WaitForSeconds(flinchStaggerSeconds);
        isStaggered = false;
        staggerRoutine = null;
    }

    private void HandleDied()
    {
        if (staggerRoutine != null)
        {
            StopCoroutine(staggerRoutine);
            staggerRoutine = null;
        }
        isStaggered = false;
        isDirectPursuing = false;
        hasActiveDestination = false;

        path.Clear();
        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    private void HandleSpawned()
    {
        isStaggered = false;
        isDirectPursuing = false;
        hasActiveDestination = false;
        path.Clear();

        // Sync agent's internal position with where the Hunter was placed (Warp in HunterBrain
        // already moved agent.nextPosition, but our transform needs to match too).
        if (agent.isOnNavMesh)
        {
            agent.nextPosition = transform.position;
        }
    }

    private void Update()
    {
        if (!agent.isOnNavMesh || !hunter.IsAlive) return;

        bool isDoorWaiting = hunter.Navigator != null && hunter.Navigator.Status == HunterNavStatus.OpeningDoor;

        float multiplier = (isStaggered || isDoorWaiting) ? 0f : hunter.SpeedMultiplier;
        bool shouldStop = multiplier <= 0f;

        if (shouldStop)
        {
            agent.isStopped = true;
        }
        else if (hasActiveDestination)
        {
            agent.isStopped = false;
        }
        agent.speed = baseSpeed * Mathf.Max(0f, multiplier);

        // ── Position smoothing (anti-teleport) ──
        // The NavMeshAgent simulates internally where it wants to be (agent.nextPosition).
        // Instead of letting Unity stamp that directly onto transform.position (which can
        // cause teleporting on path replans), we move toward it at a clamped speed.
        ApplySmoothedPosition();

        SmoothRotate(isDoorWaiting);

        if (!isDoorWaiting && !shouldStop)
        {
            // During Chase, if we have a direct clear line to the player on the NavMesh,
            // pursue the player directly rather than oscillating between static waypoint nodes.
            if (hunter.State == HunterState.Chase && player != null && player.gameObject.activeInHierarchy)
            {
                Vector3 playerPos = player.transform.position;
                if (!NavMesh.Raycast(transform.position, playerPos, out _, NavMesh.AllAreas))
                {
                    if (!isDirectPursuing) isDirectPursuing = true;
                    agent.isStopped = false;
                    agent.SetDestination(playerPos);
                    hasActiveDestination = true;
                    return;
                }
            }

            // Lost LOS or left Chase — stop suppressing path-follow
            if (isDirectPursuing)
            {
                isDirectPursuing = false;
                if (path.Count > 0 && index < path.Count)
                    DispatchCurrentWaypoint();
            }

            AdvancePath();
        }
    }

    /// <summary>
    /// Moves transform.position toward the NavMeshAgent's internally simulated nextPosition,
    /// but never faster than the agent's current speed. This makes visual teleporting impossible.
    /// </summary>
    private void ApplySmoothedPosition()
    {
        Vector3 agentTarget = agent.nextPosition;
        float maxDelta = agent.speed * speedHeadroomMultiplier * Time.deltaTime;

        // Clamp the per-frame movement so the Hunter can never snap/teleport
        Vector3 newPos = Vector3.MoveTowards(transform.position, agentTarget, maxDelta);
        transform.position = newPos;

        // Keep the agent's internal position in sync with where we actually placed the transform,
        // so the NavMeshAgent doesn't accumulate drift between its simulation and reality.
        agent.nextPosition = newPos;
    }

    private void AdvancePath()
    {
        if (path.Count == 0 || index >= path.Count - 1) return;

        Vector3 toCurrent = path[index] - transform.position;
        toCurrent.y = 0f;

        bool shouldAdvance = toCurrent.magnitude <= passDistance;

        // Also check if we passed the plane of the current waypoint towards the next
        if (!shouldAdvance && index < path.Count - 1)
        {
            Vector3 segment = path[index + 1] - path[index];
            segment.y = 0f;
            if (segment.sqrMagnitude > 0.001f)
            {
                Vector3 fromCurrentToAgent = transform.position - path[index];
                fromCurrentToAgent.y = 0f;
                if (Vector3.Dot(fromCurrentToAgent, segment.normalized) > 0f)
                {
                    Vector3 toNext = path[index + 1] - transform.position;
                    toNext.y = 0f;
                    if (toNext.sqrMagnitude < segment.sqrMagnitude)
                    {
                        shouldAdvance = true;
                    }
                }
            }
        }

        if (shouldAdvance)
        {
            index++;
            DispatchCurrentWaypoint();
        }
    }

    private void SmoothRotate(bool isDoorWaiting)
    {
        Vector3 targetDirection = Vector3.zero;

        if (isDoorWaiting)
        {
            if (hunter.Navigator != null && hunter.Navigator.Route != null)
            {
                int hold = hunter.Navigator.HoldIndex;
                if (hold >= 0 && hold < hunter.Navigator.Route.Count)
                {
                    targetDirection = hunter.Navigator.Route[hold].Position - transform.position;
                }
            }
        }
        else if (hunter.LookTarget.HasValue)
        {
            targetDirection = hunter.LookTarget.Value - transform.position;
        }
        else if (hunter.State == HunterState.Chase && player != null &&
                 Vector3.Distance(transform.position, player.transform.position) < 3.5f)
        {
            targetDirection = player.transform.position - transform.position;
        }
        else if (agent.desiredVelocity.sqrMagnitude > 0.05f)
        {
            targetDirection = agent.desiredVelocity;
        }
        else if (agent.velocity.sqrMagnitude > 0.05f)
        {
            targetDirection = agent.velocity;
        }

        targetDirection.y = 0f;
        if (targetDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetDirection);
            float factor = 1f - Mathf.Exp(-turnSmoothing * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, factor);
        }
    }
}
