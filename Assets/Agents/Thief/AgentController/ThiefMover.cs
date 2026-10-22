using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * The Agent Controller's real mover: follows the path ThiefAgent publishes through CurrentPath, point by
 * point, with the existing NavMeshAgent. Intermediate points are passed once the Thief is near them and the
 * last one is stopped at; a new or empty path from OnPathChanged replaces the old one straight away. The
 * NavMeshAgent's speed is its base speed times ThiefAgent.SpeedMultiplier. It never calls into ThiefAgent and
 * only ever sends the published points to the agent. When this mover is enabled it retires a ThiefTestMover
 * on the same object; test scenes that disable this mover can still use that stand-in.
 */
[RequireComponent(typeof(ThiefAgent), typeof(NavMeshAgent))]
public class ThiefMover : MonoBehaviour
{
    [Tooltip("Distance at which an intermediate path point counts as passed and the next one is targeted.")]
    [SerializeField] private float passDistance = 0.5f;

    [Header("Grab Behavior")]
    [Tooltip("Seconds to hold the Thief stationary while performing the grab animation.")]
    [SerializeField] private float grabPauseSeconds = 0.85f;

    [Tooltip("Speed to rotate towards the item upon arrival.")]
    [SerializeField] private float faceItemTurnSpeed = 12f;

    [Tooltip("Speed to turn towards the stash container upon arrival.")]
    [SerializeField] private float faceStashTurnSpeed = 10f;

    [Header("Flinch Stagger")]
    [Tooltip("Seconds the Thief pauses movement after taking a non-lethal hit.")]
    [SerializeField] private float flinchStaggerSeconds = 0.4f;

    private ThiefAgent thief;
    private NavMeshAgent agent;
    private float baseSpeed;
    private readonly List<Vector3> path = new List<Vector3>();
    private int index;
    private Coroutine grabPauseRoutine;
    private Coroutine deliverTurnRoutine;
    private Coroutine flinchStaggerRoutine;
    private bool isPausedForGrab;
    private bool isFacingStash;
    private float lastKnownHealth;
    private bool defaultAgentUpdateRotation;
    private int manualTurnDepth;

    private void Awake()
    {
        thief = GetComponent<ThiefAgent>();
        agent = GetComponent<NavMeshAgent>();
        baseSpeed = agent.speed;
        defaultAgentUpdateRotation = agent.updateRotation;
        lastKnownHealth = thief.MaxHealth;

        // If this real mover is enabled, retire the test-only stand-in so only one drives the agent.
        // Scenes that deliberately disable this mover keep using their ThiefTestMover.
        var testMover = GetComponent("ThiefTestMover") as MonoBehaviour;
        if (enabled && testMover != null) testMover.enabled = false;
    }

    private void OnEnable()
    {
        if (thief != null)
        {
            thief.OnPathChanged += FollowPath;
            thief.OnItemStolen += HandleItemStolen;
            thief.OnStateChanged += HandleStateChanged;
            thief.OnDied += HandleCancelGrab;
            thief.OnVanished += HandleCancelGrab;
        }
    }

    private void OnDisable()
    {
        if (thief != null)
        {
            thief.OnPathChanged -= FollowPath;
            thief.OnItemStolen -= HandleItemStolen;
            thief.OnStateChanged -= HandleStateChanged;
            thief.OnDied -= HandleCancelGrab;
            thief.OnVanished -= HandleCancelGrab;
        }
        HandleCancelGrab();
        if (deliverTurnRoutine != null) { StopCoroutine(deliverTurnRoutine); deliverTurnRoutine = null; }
        if (flinchStaggerRoutine != null) { StopCoroutine(flinchStaggerRoutine); flinchStaggerRoutine = null; }
        EndManualTurn();
    }

    private void BeginManualTurn()
    {
        if (agent == null) return;
        if (manualTurnDepth == 0) agent.updateRotation = false;
        manualTurnDepth++;
    }

    private void EndManualTurn()
    {
        if (agent == null) return;
        manualTurnDepth = Mathf.Max(0, manualTurnDepth - 1);
        if (manualTurnDepth == 0) agent.updateRotation = defaultAgentUpdateRotation;
    }

    private void HandleStateChanged(ThiefState state)
    {
        if (state == ThiefState.Delivering)
        {
            if (deliverTurnRoutine != null)
            {
                StopCoroutine(deliverTurnRoutine);
                deliverTurnRoutine = null;
                EndManualTurn();
            }
            deliverTurnRoutine = StartCoroutine(FaceStashDelivery());
        }
    }

    private void HandleItemStolen(PickupInteractable item)
    {
        if (grabPauseRoutine != null)
        {
            StopCoroutine(grabPauseRoutine);
            grabPauseRoutine = null;
            EndManualTurn();
        }
        grabPauseRoutine = StartCoroutine(HoldForGrab(item));
    }

    private void HandleCancelGrab()
    {
        if (grabPauseRoutine != null)
        {
            StopCoroutine(grabPauseRoutine);
            grabPauseRoutine = null;
        }
        if (deliverTurnRoutine != null)
        {
            StopCoroutine(deliverTurnRoutine);
            deliverTurnRoutine = null;
        }
        if (flinchStaggerRoutine != null)
        {
            StopCoroutine(flinchStaggerRoutine);
            flinchStaggerRoutine = null;
        }
        isPausedForGrab = false;
        isFacingStash = false;
        EndManualTurn();
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
    }

    private IEnumerator FaceStashDelivery()
    {
        isFacingStash = true;
        BeginManualTurn();

        // Deliver() can leave the mover's cached path with its final point because ThiefAgent's
        // current path may already be empty. Drop it so Update cannot re-issue a destination and
        // walk the Thief away while it is supposed to be facing the stash.
        path.Clear();
        index = 0;
        FreezeAgent();

        Vector3? stashCenter = FindNearestStashVisualCenter();
        if (stashCenter.HasValue)
        {
            Vector3 center = stashCenter.Value;
            Vector3 dir = center - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                float elapsed = 0f;
                while (elapsed < 0.4f)
                {
                    if (thief.State != ThiefState.Delivering) break;
                    elapsed += Time.deltaTime;

                    // Recompute toward the stash from the current position. This keeps the final
                    // facing exact even if the agent was still settling when the turn began.
                    Vector3 toCenter = center - transform.position;
                    toCenter.y = 0f;
                    if (toCenter.sqrMagnitude > 0.001f)
                    {
                        Quaternion lookRot = Quaternion.LookRotation(toCenter.normalized);
                        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * faceStashTurnSpeed);
                    }
                    yield return null;
                }

                if (thief.State == ThiefState.Delivering)
                {
                    Vector3 finalDir = center - transform.position;
                    finalDir.y = 0f;
                    Quaternion finalRot = finalDir.sqrMagnitude > 0.001f
                        ? Quaternion.LookRotation(finalDir.normalized)
                        : transform.rotation;
                    transform.rotation = finalRot;
                }
            }
        }

        isFacingStash = false;
        EndManualTurn();
        deliverTurnRoutine = null;
    }

    /*
     * The visible centre of the stash the Thief has just reached. The DropPoint is only the deposit
     * marker and often sits apart from the bag/basket, so start there and walk up to the first
     * ancestor that owns renderers: that ancestor is the stash visual, and its combined bounds give
     * the real centre to face.
     */
    private Vector3? FindNearestStashVisualCenter()
    {
        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Exclude);

        Transform nearestDropPoint = null;
        float nearestDropDistance = 3.5f;
        foreach (Transform t in all)
        {
            if (t.name.ToLowerInvariant() != "droppoint") continue;

            float d = FlatDistance(transform.position, t.position);
            if (d >= nearestDropDistance) continue;

            nearestDropDistance = d;
            nearestDropPoint = t;
        }

        if (nearestDropPoint != null)
        {
            Transform stashRoot = FindFirstAncestorWithRenderers(nearestDropPoint);
            if (stashRoot != null && TryGetRendererCenter(stashRoot, out Vector3 center))
                return center;
        }

        // Scenes without a DropPoint under the visual fall back to the nearest stash-named object.
        Transform nearestStash = null;
        float nearestStashDistance = 3.5f;
        foreach (Transform t in all)
        {
            string n = t.name.ToLowerInvariant();
            if (!n.Contains("stash") && !n.Contains("bag") && !n.Contains("basket")) continue;
            if (t.GetComponentsInChildren<Renderer>(false).Length == 0) continue;

            float d = FlatDistance(transform.position, t.position);
            if (d >= nearestStashDistance) continue;

            nearestStashDistance = d;
            nearestStash = t;
        }

        if (nearestStash != null && TryGetRendererCenter(nearestStash, out Vector3 fallbackCenter))
            return fallbackCenter;

        return null;
    }

    private static Transform FindFirstAncestorWithRenderers(Transform start)
    {
        for (Transform t = start; t != null; t = t.parent)
            if (t.GetComponentsInChildren<Renderer>(false).Length > 0) return t;

        return null;
    }

    private static bool TryGetRendererCenter(Transform root, out Vector3 center)
    {
        center = root.position;
        bool found = false;
        Bounds bounds = default;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(false))
        {
            if (renderer == null || !renderer.enabled) continue;
            if (!found) { bounds = renderer.bounds; found = true; }
            else bounds.Encapsulate(renderer.bounds);
        }

        if (!found) return false;
        center = bounds.center;
        return true;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void FreezeAgent()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        agent.ResetPath();
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    private IEnumerator HoldForGrab(PickupInteractable item)
    {
        isPausedForGrab = true;
        BeginManualTurn();
        FreezeAgent();

        Vector3 itemPos = item != null ? item.transform.position : transform.position + transform.forward;
        if (item != null && TryGetRendererCenter(item.transform, out Vector3 itemCenter)) itemPos = itemCenter;

        float elapsed = 0f;
        while (elapsed < grabPauseSeconds)
        {
            elapsed += Time.deltaTime;
            // Smoothly turn to face the item's visible centre during the first 0.35s. Recompute the
            // direction from the current position so any residual agent settling cannot skew it.
            if (elapsed < 0.35f)
            {
                Vector3 toItem = itemPos - transform.position;
                toItem.y = 0f;
                if (toItem.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(toItem.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * faceItemTurnSpeed);
                }
            }
            yield return null;
        }

        // Snap to the exact direction from the final, settled position.
        Vector3 finalDir = itemPos - transform.position;
        finalDir.y = 0f;
        Quaternion finalRot = finalDir.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(finalDir.normalized)
            : transform.rotation;
        transform.rotation = finalRot;

        isPausedForGrab = false;
        EndManualTurn();
        grabPauseRoutine = null;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            if (path.Count > 0 && index < path.Count)
            {
                agent.SetDestination(path[index]);
            }
        }
    }

    private void FollowPath(IReadOnlyList<Vector3> points)
    {
        path.Clear();
        for (int i = 0; i < points.Count; i++) path.Add(points[i]);
        index = 0;

        if (!agent.isOnNavMesh) return;

        if (path.Count == 0)
        {
            agent.ResetPath();
            return;
        }

        // If paused for the grab or facing the stash, do not dispatch the agent until that turn completes.
        if (!isPausedForGrab && !isFacingStash)
        {
            agent.SetDestination(path[0]);
        }
    }

    private void Update()
    {
        agent.speed = baseSpeed * thief.SpeedMultiplier;

        // Detect non-lethal damage → brief movement stagger (only while actively traveling)
        bool isTraveling = thief.State == ThiefState.GoToItem || thief.State == ThiefState.GoToStash;
        if (isTraveling && thief.Health < lastKnownHealth && thief.Health > 0f && !isPausedForGrab)
        {
            if (flinchStaggerRoutine != null) StopCoroutine(flinchStaggerRoutine);
            flinchStaggerRoutine = StartCoroutine(FlinchStagger());
        }
        lastKnownHealth = thief.Health;

        if (isPausedForGrab || isFacingStash || path.Count == 0 || !agent.isOnNavMesh) return;

        Vector3 toTarget = path[index] - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= passDistance && index < path.Count - 1)
        {
            index++;
            agent.SetDestination(path[index]);
            return;
        }

        // A publish can be dropped when the agent is off the mesh for a moment (spawn race), and the
        // agent can lose its path mid-trip: re-issue the current point so the Thief never strands
        // with a stored path and no destination.
        if (!agent.hasPath) agent.SetDestination(path[index]);
    }

    private IEnumerator FlinchStagger()
    {
        if (agent.isOnNavMesh) agent.isStopped = true;
        yield return new WaitForSeconds(flinchStaggerSeconds);

        flinchStaggerRoutine = null;
        if (agent.isOnNavMesh) agent.isStopped = false;

        // Re-issue current destination so the Thief resumes cleanly
        if (path.Count > 0 && index < path.Count && agent.isOnNavMesh)
            agent.SetDestination(path[index]);
    }
}
