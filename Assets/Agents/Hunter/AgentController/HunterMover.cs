using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * HunterMover:
 * Drives the Hunter's movement along the path published by HunterBrain.
 * Handles NavMeshAgent velocity/stopping via SpeedMultiplier, door facing when
 * HunterNavigator is opening a door, LookTarget tracking, and flinch stagger pauses.
 * Yields if HunterTestMover is present, disabling it so this production mover takes over.
 */
[RequireComponent(typeof(HunterBrain), typeof(NavMeshAgent))]
public class HunterMover : MonoBehaviour
{
    [Header("Movement Tuning")]
    [Tooltip("Distance at which an intermediate path point counts as reached and the next one is targeted.")]
    [SerializeField] private float passDistance = 0.5f;

    [Tooltip("Degrees per second when turning to face a LookTarget or doorway.")]
    [SerializeField, Min(1f)] private float turnSpeed = 240f;

    [Header("Flinch Stagger")]
    [Tooltip("Seconds the Hunter pauses movement after taking non-lethal damage.")]
    [SerializeField] private float flinchStaggerSeconds = 0.35f;

    private HunterBrain hunter;
    private NavMeshAgent agent;
    private float baseSpeed;
    private readonly List<Vector3> path = new List<Vector3>();
    private int index;
    private Coroutine staggerRoutine;
    private bool isStaggered;

    private void Awake()
    {
        hunter = GetComponent<HunterBrain>();
        agent = GetComponent<NavMeshAgent>();
        baseSpeed = agent.speed;

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
        path.Clear();
        if (newPath != null) path.AddRange(newPath);
        index = 0;

        if (!agent.isOnNavMesh) return;

        if (path.Count == 0)
        {
            agent.ResetPath();
        }
        else
        {
            agent.SetDestination(path[0]);
        }
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
        path.Clear();
    }

    private void Update()
    {
        if (!agent.isOnNavMesh || !hunter.IsAlive) return;

        bool isDoorWaiting = hunter.Navigator != null && hunter.Navigator.Status == HunterNavStatus.OpeningDoor;

        float multiplier = (isStaggered || isDoorWaiting) ? 0f : hunter.SpeedMultiplier;
        agent.isStopped = multiplier <= 0f;
        agent.speed = baseSpeed * Mathf.Max(0f, multiplier);

        if (isDoorWaiting)
        {
            FaceDoorway();
        }
        else
        {
            FaceLookTarget();
            AdvancePath();
        }
    }

    private void AdvancePath()
    {
        if (path.Count == 0 || index >= path.Count - 1) return;

        Vector3 offset = path[index] - transform.position;
        offset.y = 0f;
        if (offset.magnitude > passDistance) return;

        index++;
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(path[index]);
        }
    }

    private void FaceLookTarget()
    {
        Vector3? look = hunter.LookTarget;
        agent.updateRotation = !look.HasValue;
        if (!look.HasValue) return;

        Vector3 direction = look.Value - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    private void FaceDoorway()
    {
        agent.updateRotation = false;

        if (hunter.Navigator == null || hunter.Navigator.Route == null) return;
        int hold = hunter.Navigator.HoldIndex;
        if (hold < 0 || hold >= hunter.Navigator.Route.Count) return;

        Vector3 doorPos = hunter.Navigator.Route[hold].Position;
        Vector3 direction = doorPos - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }
}
