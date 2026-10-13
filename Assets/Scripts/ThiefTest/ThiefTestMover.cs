using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * Test-only stand-in for the Agent Controller's movement, used only on the Thief in the Thief test scene.
 * Follows ThiefAgent.CurrentPath point by point with the NavMeshAgent: intermediate points are passed once
 * the Thief is near them, and it stops at the last one. A new or empty path from OnPathChanged replaces the
 * old one straight away. The NavMeshAgent's speed is its base speed times ThiefAgent.SpeedMultiplier.
 */
[RequireComponent(typeof(ThiefAgent), typeof(NavMeshAgent))]
public class ThiefTestMover : MonoBehaviour
{
    [Tooltip("Distance at which an intermediate path point counts as passed and the next one is targeted.")]
    [SerializeField] private float passDistance = 0.5f;

    private ThiefAgent thief;
    private NavMeshAgent agent;
    private float baseSpeed;
    private readonly List<Vector3> path = new List<Vector3>();
    private int index;

    private void Awake()
    {
        thief = GetComponent<ThiefAgent>();
        agent = GetComponent<NavMeshAgent>();
        baseSpeed = agent.speed;
    }

    private void OnEnable() => thief.OnPathChanged += FollowPath;
    private void OnDisable() => thief.OnPathChanged -= FollowPath;

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
        agent.speed = baseSpeed * thief.SpeedMultiplier;

        if (path.Count == 0 || index >= path.Count - 1 || !agent.isOnNavMesh) return;

        Vector3 offset = path[index] - transform.position;
        offset.y = 0f;
        if (offset.magnitude > passDistance) return;

        index++;
        agent.SetDestination(path[index]);
    }
}
