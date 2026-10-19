using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * Test-only stand-in for the Agent Controller's movement, used on the Hunter in test scenes.
 * Follows HunterBrain.CurrentPath point by point with the NavMeshAgent: intermediate points are passed once
 * the Hunter is near them, and it stops at the last one. A new or empty path from OnPathChanged replaces the old
 * one straight away. The NavMeshAgent's speed is its base speed times HunterBrain.SpeedMultiplier, and a
 * multiplier of 0 (Attack, Dead) makes it stand still. If the brain sets a LookTarget, the Hunter turns to face it.
 */
[RequireComponent(typeof(HunterBrain), typeof(NavMeshAgent))]
public class HunterTestMover : MonoBehaviour
{
    [Tooltip("Distance at which an intermediate path point counts as passed and the next one is targeted.")]
    [SerializeField] private float passDistance = 0.5f;

    [Tooltip("Degrees per second when turning to face a LookTarget.")]
    [SerializeField, Min(1f)] private float turnSpeed = 240f;

    private HunterBrain hunter;
    private NavMeshAgent agent;
    private float baseSpeed;
    private readonly List<Vector3> path = new List<Vector3>();
    private int index;

    private void Awake()
    {
        hunter = GetComponent<HunterBrain>();
        agent = GetComponent<NavMeshAgent>();
        baseSpeed = agent.speed;
    }

    private void OnEnable() => hunter.OnPathChanged += FollowPath;
    private void OnDisable() => hunter.OnPathChanged -= FollowPath;

    // Takes over a newly published path, or stands still when the path is empty.
    private void FollowPath(IReadOnlyList<Vector3> newPath)
    {
        path.Clear();
        path.AddRange(newPath);
        index = 0;

        if (!agent.isOnNavMesh) return;

        if (path.Count == 0) agent.ResetPath();
        else agent.SetDestination(path[0]);
    }

    private void Update()
    {
        if (!agent.isOnNavMesh) return;

        float multiplier = hunter.SpeedMultiplier;
        agent.isStopped = multiplier <= 0f;
        agent.speed = baseSpeed * multiplier;

        FaceLookTarget();
        AdvancePath();
    }

    // Moves on to the next path point once the current one is close enough.
    private void AdvancePath()
    {
        if (path.Count == 0 || index >= path.Count - 1) return;

        Vector3 offset = path[index] - transform.position;
        offset.y = 0f;
        if (offset.magnitude > passDistance) return;

        index++;
        agent.SetDestination(path[index]);
    }

    // Turns toward the brain's LookTarget; otherwise the NavMeshAgent turns the Hunter the way it walks.
    private void FaceLookTarget()
    {
        Vector3? look = hunter.LookTarget;
        agent.updateRotation = !look.HasValue;
        if (!look.HasValue) return;

        Vector3 direction = look.Value - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
    }
}
