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

        // Test scenes keep their ThiefTestMover as the only mover; never both at once.
        if (GetComponent("ThiefTestMover") != null) enabled = false;
    }

    private void OnEnable()
    {
        if (thief != null) thief.OnPathChanged += FollowPath;
    }

    private void OnDisable()
    {
        if (thief != null) thief.OnPathChanged -= FollowPath;
    }

    private void FollowPath(IReadOnlyList<Vector3> points)
    {
        path.Clear();
        for (int i = 0; i < points.Count; i++) path.Add(points[i]);
        index = 0;

        if (path.Count == 0)
        {
            agent.ResetPath();
            return;
        }
        agent.SetDestination(path[0]);
    }

    private void Update()
    {
        agent.speed = baseSpeed * thief.SpeedMultiplier;

        if (path.Count == 0 || index >= path.Count - 1) return;

        Vector3 target = path[index];
        Vector3 flat = transform.position - target;
        flat.y = 0f;
        if (flat.magnitude > passDistance) return;

        index++;
        agent.SetDestination(path[index]);
    }
}
