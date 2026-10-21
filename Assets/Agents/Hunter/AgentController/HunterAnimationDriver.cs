using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * HunterAnimationDriver:
 * Drives the Animator on the rigged Hunter model using HunterBrain events and NavMeshAgent locomotion.
 * Manages 3 Animator layers:
 *   Layer 0 (Base Layer): Locomotion (Inactive/Idle/Walk/Run), Attack, Flinch, DoorPush, Death.
 *   Layer 1 (Upper Body): Zombie walking-1 upper torso/arms layered over Run legs during Chase.
 *   Layer 2 (Head Look): Look Around head/neck scanning overlay during Patrol/Investigate.
 *
 * Speed tuning rationale:
 *   NavMeshAgent base speed = 1.4 m/s (set on PF_Hunter prefab).
 *   Walk authored speed     = 1.15 m/s (Zombie walking-2.fbx forward displacement).
 *   Run authored speed      = 4.25 m/s (Zombie Run.fbx forward displacement).
 *   HunterSettings multipliers: patrol=0.82->1.15 m/s, search=0.93->1.30 m/s,
 *                               investigate=1.14->1.60 m/s, chase=1.50->2.10 m/s.
 *   Chase ratio: 2.10/4.25 = 0.49x (clamped [0.45, 1.5]) -- moderate lurching run, zero foot sliding.
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
    [Tooltip("Authored forward speed (m/s) of the Hunter_Walk clip (Zombie walking-2.fbx). Used for stride matching during Patrol/Investigate/Search.")]
    [SerializeField] private float walkAuthoredSpeed = 1.15f;

    [Tooltip("Authored forward speed (m/s) of the Hunter_Run clip (Zombie Run.fbx). Used for stride matching during Chase.")]
    [SerializeField] private float runAuthoredSpeed = 4.25f;

    [Tooltip("Speed multiplier for the Upper Body Chase layer clip to sync with 2 run strides per upper-body lurch.")]
    [SerializeField] private float chaseUpperBodySpeedMultiplier = 3.25f;

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

    // Animator parameter hashes
    private static readonly int SpeedParam      = Animator.StringToHash("Speed");
    private static readonly int AttackTrigger   = Animator.StringToHash("Attack");
    private static readonly int FlinchTrigger   = Animator.StringToHash("Flinch");
    private static readonly int DoorPushTrigger = Animator.StringToHash("DoorPush");
    private static readonly int DieTrigger      = Animator.StringToHash("Die");
    private static readonly int IsChasingParam  = Animator.StringToHash("IsChasing");
    private static readonly int IsLookingParam  = Animator.StringToHash("IsLooking");
    private static readonly int IsInactiveParam = Animator.StringToHash("IsInactive");

    private const int BaseLayerIndex      = 0;
    private const int UpperBodyLayerIndex = 1;
    private const int HeadLookLayerIndex  = 2;

    private HunterBrain hunter;
    private NavMeshAgent agent;
    private float nextFlinchTime;
    private bool isPushingDoor;

    private float currentUpperBodyWeight;
    private float currentHeadLookWeight;

    private void Awake()
    {
        hunter = GetComponent<HunterBrain>();
        agent  = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (model == null && animator != null)
            model = animator.transform;

        // Animator must never drive root motion: NavMeshAgent controls displacement
        if (animator != null)
            animator.applyRootMotion = false;
    }

    private void OnEnable()
    {
        if (hunter != null)
        {
            hunter.OnStateChanged += HandleStateChanged;
            hunter.OnDamaged      += HandleDamaged;
            hunter.OnDied         += HandleDied;
            hunter.OnSpawned      += HandleSpawned;
        }
    }

    private void OnDisable()
    {
        if (hunter != null)
        {
            hunter.OnStateChanged -= HandleStateChanged;
            hunter.OnDamaged      -= HandleDamaged;
            hunter.OnDied         -= HandleDied;
            hunter.OnSpawned      -= HandleSpawned;
        }
    }

    private void Update()
    {
        if (animator == null || !hunter.IsAlive) return;

        UpdateLocomotion();
        UpdateDoorPush();
        UpdateLayers();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Locomotion
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateLocomotion()
    {
        HunterState state = hunter.State;
        bool isInactive   = state == HunterState.Inactive;
        bool isChasing    = state == HunterState.Chase;
        float speed       = agent.velocity.magnitude;

        animator.SetFloat(SpeedParam, speed);
        animator.SetBool(IsInactiveParam, isInactive);

        // Stride matching: scale playback speed so feet match ground movement.
        // With NavMeshAgent base = 1.4 m/s and tuned HunterSettings multipliers,
        // ratios are ~1.0x for patrol walk and chase run -- zero foot sliding.
        if (isInactive || state == HunterState.Dead)
        {
            animator.speed = 1f;
        }
        else if (speed > 0.05f)
        {
            float authoredRef = isChasing ? runAuthoredSpeed : walkAuthoredSpeed;
            animator.speed = Mathf.Clamp(speed / authoredRef, 0.45f, 1.5f);
        }
        else
        {
            animator.speed = 1f;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Door Push
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateDoorPush()
    {
        bool isDoorWaiting = hunter.Navigator != null &&
                             hunter.Navigator.Status == HunterNavStatus.OpeningDoor;

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

    // ─────────────────────────────────────────────────────────────────────────
    // Layer blending
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateLayers()
    {
        // Layer 1: Upper Body Chase blend
        bool isChasing = hunter.State == HunterState.Chase;
        animator.SetBool(IsChasingParam, isChasing);

        float targetUpperWeight = isChasing ? 1f : 0f;
        currentUpperBodyWeight = Mathf.MoveTowards(
            currentUpperBodyWeight, targetUpperWeight, Time.deltaTime * layerBlendSpeed);

        if (animator.layerCount > UpperBodyLayerIndex)
            animator.SetLayerWeight(UpperBodyLayerIndex, currentUpperBodyWeight);

        // Layer 2: Head Look overlay while walking
        bool canHeadLook = headLookWhileWalking &&
                           (hunter.State == HunterState.Patrol     ||
                            hunter.State == HunterState.Investigate ||
                            hunter.State == HunterState.Search)    &&
                           agent.velocity.magnitude > 0.2f         &&
                           !isPushingDoor;

        animator.SetBool(IsLookingParam, canHeadLook);
        float targetHeadWeight = canHeadLook ? headLookWeight : 0f;
        currentHeadLookWeight = Mathf.MoveTowards(
            currentHeadLookWeight, targetHeadWeight, Time.deltaTime * layerBlendSpeed);

        if (animator.layerCount > HeadLookLayerIndex)
            animator.SetLayerWeight(HeadLookLayerIndex, currentHeadLookWeight);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Brain event handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleStateChanged(HunterState state)
    {
        if (animator == null) return;

        if (state == HunterState.Attack)
        {
            animator.speed = 1f;
            animator.SetTrigger(AttackTrigger);
        }
    }

    private void HandleDamaged(DamageInfo info)
    {
        if (animator == null || !hunter.IsAlive) return;

        if (Time.time >= nextFlinchTime)
        {
            nextFlinchTime = Time.time + flinchCooldown;
            animator.speed = 1f;
            animator.SetTrigger(FlinchTrigger);
        }
    }

    private void HandleDied()
    {
        if (animator == null) return;

        animator.speed = 1f;
        animator.SetTrigger(DieTrigger);

        currentUpperBodyWeight = 0f;
        currentHeadLookWeight  = 0f;

        if (animator.layerCount > UpperBodyLayerIndex)
            animator.SetLayerWeight(UpperBodyLayerIndex, 0f);
        if (animator.layerCount > HeadLookLayerIndex)
            animator.SetLayerWeight(HeadLookLayerIndex, 0f);
    }

    private void HandleSpawned()
    {
        if (animator == null) return;

        animator.speed = 1f;
        animator.Rebind();
        animator.Update(0f);

        currentUpperBodyWeight = 0f;
        currentHeadLookWeight  = 0f;

        if (animator.layerCount > UpperBodyLayerIndex)
            animator.SetLayerWeight(UpperBodyLayerIndex, 0f);
        if (animator.layerCount > HeadLookLayerIndex)
            animator.SetLayerWeight(HeadLookLayerIndex, 0f);
    }
}
