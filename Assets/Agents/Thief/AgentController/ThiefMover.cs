using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * The Agent Controller's real mover: follows the path ThiefAgent publishes through CurrentPath, point by
 * point, with the existing NavMeshAgent. Intermediate points are passed once the Thief is near them and the
 * last one is stopped at; a new or empty path from OnPathChanged replaces the old one straight away. The
 * NavMeshAgent's speed is its base speed times ThiefAgent.SpeedMultiplier. It never calls into ThiefAgent and
 * only ever sends the published points to the agent. While a ThiefTestMover sits on the same object (test
 * scenes) it yields and stays disabled, so only one mover ever drives the agent.
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

    private ThiefAgent thief;
    private NavMeshAgent agent;
    private float baseSpeed;
    private readonly List<Vector3> path = new List<Vector3>();
    private int index;
    private Coroutine grabPauseRoutine;
    private Coroutine deliverTurnRoutine;
    private bool isPausedForGrab;

    private void Awake()
    {
        thief = GetComponent<ThiefAgent>();
        agent = GetComponent<NavMeshAgent>();
        baseSpeed = agent.speed;

        // Test scenes keep their ThiefTestMover as the only mover; never both at once.
        if (GetComponent("ThiefTestMover") != null) enabled = false;
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
    }

    private void HandleStateChanged(ThiefState state)
    {
        if (state == ThiefState.Delivering)
        {
            if (deliverTurnRoutine != null) StopCoroutine(deliverTurnRoutine);
            deliverTurnRoutine = StartCoroutine(FaceStashDelivery());
        }
    }

    private void HandleItemStolen(PickupInteractable item)
    {
        if (grabPauseRoutine != null) StopCoroutine(grabPauseRoutine);
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
        isPausedForGrab = false;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
    }

    private IEnumerator FaceStashDelivery()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        Transform stashTarget = FindNearestStashObject();
        if (stashTarget != null)
        {
            Vector3 dir = stashTarget.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                float elapsed = 0f;
                while (elapsed < 0.4f)
                {
                    elapsed += Time.deltaTime;
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * faceStashTurnSpeed);
                    yield return null;
                }
            }
        }
        deliverTurnRoutine = null;
    }

    private Transform FindNearestStashObject()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 3.5f);
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (var col in colliders)
        {
            string n = col.name.ToLower();
            if (n.Contains("stash") || n.Contains("bag") || n.Contains("basket") || n.Contains("droppoint"))
            {
                float d = Vector3.Distance(transform.position, col.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = col.transform;
                }
            }
        }
        if (closest != null) return closest;

        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        foreach (var t in all)
        {
            string n = t.name.ToLower();
            if (n.Contains("stash") || n.Contains("bag") || n.Contains("basket") || n.Contains("droppoint"))
            {
                float d = Vector3.Distance(transform.position, t.position);
                if (d < 3.5f && d < minDist)
                {
                    minDist = d;
                    closest = t;
                }
            }
        }
        return closest;
    }

    private IEnumerator HoldForGrab(PickupInteractable item)
    {
        isPausedForGrab = true;
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        Vector3 itemPos = item != null ? item.transform.position : transform.position + transform.forward;
        Vector3 dir = itemPos - transform.position;
        dir.y = 0f;
        Quaternion targetRot = dir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(dir.normalized) : transform.rotation;

        float elapsed = 0f;
        while (elapsed < grabPauseSeconds)
        {
            elapsed += Time.deltaTime;
            // Smoothly turn to face the item during the first 0.35s
            if (elapsed < 0.35f && dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * faceItemTurnSpeed);
            }
            yield return null;
        }

        isPausedForGrab = false;
        grabPauseRoutine = null;

        if (agent.isOnNavMesh)
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

        // If paused performing the grab action, do not dispatch the agent until the grab completes.
        if (!isPausedForGrab)
        {
            agent.SetDestination(path[0]);
        }
    }

    private void Update()
    {
        agent.speed = baseSpeed * thief.SpeedMultiplier;

        if (isPausedForGrab || path.Count == 0 || !agent.isOnNavMesh) return;

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
}
