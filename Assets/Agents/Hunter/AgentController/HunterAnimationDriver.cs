using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * HunterAnimationDriver:
 * Drives the Animator on the rigged Hunter model using HunterBrain events and NavMeshAgent locomotion.
 * Manages 3 Animator layers:
 *   Layer 0 (Base Layer): Locomotion (Idle/Walk), Attack, Flinch, DoorPush, Death.
 *   Layer 1 (Upper Body): Zombie walking-1 upper torso/arms layered over walking-2 legs during Chase.
 *   Layer 2 (Head Look): Look Around head/neck scanning overlay during Patrol/Investigate.
 */
[RequireComponent(typeof(HunterBrain), typeof(NavMeshAgent))]
public class HunterAnimationDriver : MonoBehaviour
{
    [Header("Model & Components")]
    [Tooltip("Rigged child model container.")]
    [SerializeField] private Transform model;

    [Tooltip("Humanoid Animator on the Hunter model; uses AC_Hunter.")]
    [SerializeField] private Animator animator;

    [Header("Locomotion Tuning")]
    [Tooltip("Authored meters per second of the Hunter_Walk clip for stride matching.")]
    [SerializeField] private float walkAuthoredSpeed = 1.4f;

    [Header("Hit & Facing Tuning")]
    [Tooltip("Minimum seconds between successive flinch animations.")]
    [SerializeField] private float flinchCooldown = 0.5f;

    [Header("Layer Overlays")]
    [Tooltip("Whether to enable subtle head-look overlay while walking in Patrol/Investigate.")]
    [SerializeField] private bool headLookWhileWalking = true;

    [Tooltip("Target layer weight for Head Look Layer.")]
    [SerializeField, Range(0.1f, 1f)] private float headLookWeight = 0.35f;

    [Tooltip("Speed to blend layer weights in and out (4.0 = ~0.25s).")]
    [SerializeField] private float layerBlendSpeed = 4f;

    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int FlinchTrigger = Animator.StringToHash("Flinch");
    private static readonly int DoorPushTrigger = Animator.StringToHash("DoorPush");
    private static readonly int DieTrigger = Animator.StringToHash("Die");
    private static readonly int IsChasingParam = Animator.StringToHash("IsChasing");
    private static readonly int IsLookingParam = Animator.StringToHash("IsLooking");

    private const int BaseLayerIndex = 0;
    private const int UpperBodyLayerIndex = 1;
    private const int HeadLookLayerIndex = 2;

    private HunterBrain hunter;
    private NavMeshAgent agent;
    private float nextFlinchTime;
    private bool isPushingDoor;

    private float currentUpperBodyWeight;
    private float currentHeadLookWeight;

    private void Awake()
    {
        hunter = GetComponent<HunterBrain>();
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (model == null && animator != null)
        {
            model = animator.transform;
        }

        // Animator must never drive root motion: NavMeshAgent controls displacement
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    private void OnEnable()
    {
        if (hunter != null)
        {
            hunter.OnStateChanged += HandleStateChanged;
            hunter.OnDamaged += HandleDamaged;
            hunter.OnDied += HandleDied;
            hunter.OnSpawned += HandleSpawned;
        }
    }

    private void OnDisable()
    {
        if (hunter != null)
        {
            hunter.OnStateChanged -= HandleStateChanged;
            hunter.OnDamaged -= HandleDamaged;
            hunter.OnDied -= HandleDied;
            hunter.OnSpawned -= HandleSpawned;
        }
    }

    private void Update()
    {
        if (animator == null || !hunter.IsAlive) return;

        UpdateLocomotion();
        UpdateDoorPush();
        UpdateLayers();
    }

    private void UpdateLocomotion()
    {
        float speed = agent.velocity.magnitude;
        animator.SetFloat(SpeedParam, speed);

        // Stride speed matching during walk
        if (speed > 0.1f && walkAuthoredSpeed > 0.01f)
        {
            if (hunter.State == HunterState.Patrol || hunter.State == HunterState.Investigate || hunter.State == HunterState.Search)
            {
                animator.speed = Mathf.Clamp(speed / walkAuthoredSpeed, 0.7f, 1.5f);
            }
            else
            {
                animator.speed = 1f;
            }
        }
        else
        {
            animator.speed = 1f;
        }
    }

    private void UpdateDoorPush()
    {
        bool isDoorWaiting = hunter.Navigator != null && hunter.Navigator.Status == HunterNavStatus.OpeningDoor;

        if (isDoorWaiting)
        {
            if (!isPushingDoor)
            {
                isPushingDoor = true;
                animator.SetTrigger(DoorPushTrigger);
            }
        }
        else
        {
            isPushingDoor = false;
        }
    }

    private void UpdateLayers()
    {
        // Layer 1: Upper Body Chase blend
        bool isChasing = hunter.State == HunterState.Chase;
        animator.SetBool(IsChasingParam, isChasing);
        float targetUpperWeight = isChasing ? 1f : 0f;
        currentUpperBodyWeight = Mathf.MoveTowards(currentUpperBodyWeight, targetUpperWeight, Time.deltaTime * layerBlendSpeed);
        if (animator.layerCount > UpperBodyLayerIndex)
        {
            animator.SetLayerWeight(UpperBodyLayerIndex, currentUpperBodyWeight);
        }

        // Layer 2: Head Look overlay
        bool canHeadLook = headLookWhileWalking &&
                           (hunter.State == HunterState.Patrol || hunter.State == HunterState.Investigate || hunter.State == HunterState.Search) &&
                           agent.velocity.magnitude > 0.2f &&
                           !isPushingDoor;

        animator.SetBool(IsLookingParam, canHeadLook);
        float targetHeadWeight = canHeadLook ? headLookWeight : 0f;
        currentHeadLookWeight = Mathf.MoveTowards(currentHeadLookWeight, targetHeadWeight, Time.deltaTime * layerBlendSpeed);
        if (animator.layerCount > HeadLookLayerIndex)
        {
            animator.SetLayerWeight(HeadLookLayerIndex, currentHeadLookWeight);
        }
    }

    private void HandleStateChanged(HunterState state)
    {
        if (animator == null) return;

        if (state == HunterState.Attack)
        {
            animator.SetTrigger(AttackTrigger);
        }
    }

    private void HandleDamaged(DamageInfo info)
    {
        if (animator == null || !hunter.IsAlive) return;

        if (Time.time >= nextFlinchTime)
        {
            nextFlinchTime = Time.time + flinchCooldown;
            animator.SetTrigger(FlinchTrigger);
        }
    }

    private void HandleDied()
    {
        if (animator == null) return;

        animator.speed = 1f;
        animator.SetTrigger(DieTrigger);

        currentUpperBodyWeight = 0f;
        currentHeadLookWeight = 0f;

        if (animator.layerCount > UpperBodyLayerIndex)
        {
            animator.SetLayerWeight(UpperBodyLayerIndex, 0f);
        }
        if (animator.layerCount > HeadLookLayerIndex)
        {
            animator.SetLayerWeight(HeadLookLayerIndex, 0f);
        }
    }

    private void HandleSpawned()
    {
        if (animator == null) return;

        animator.speed = 1f;
        animator.Rebind();
        animator.Update(0f);

        currentUpperBodyWeight = 0f;
        currentHeadLookWeight = 0f;

        if (animator.layerCount > UpperBodyLayerIndex)
        {
            animator.SetLayerWeight(UpperBodyLayerIndex, 0f);
        }
        if (animator.layerCount > HeadLookLayerIndex)
        {
            animator.SetLayerWeight(HeadLookLayerIndex, 0f);
        }
    }
}
