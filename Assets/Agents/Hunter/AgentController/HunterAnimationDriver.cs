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
 *   HunterSettings multipliers: patrol=0.70->0.98 m/s, search=0.80->1.12 m/s,
 *                               investigate=1.00->1.40 m/s, chase=1.35->1.89 m/s.
 *   Chase ratio: 1.89/4.25 = 0.44x (clamped [0.35, 1.5]) -- realistic menacing shambling run, zero foot sliding.
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
    [SerializeField] private float runAuthoredSpeed = 2.6f;

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

    [Header("Visual Fidelity & Banking")]
    [Tooltip("Maximum roll lean angle (degrees) during sharp turns.")]
    [SerializeField, Range(0f, 10f)] private float maxBankAngle = 4.5f;

    [Tooltip("Rate at which the model banks into turns.")]
    [SerializeField] private float bankSpeed = 6f;

    [Tooltip("Rate at which the model recovers from banking.")]
    [SerializeField] private float bankRecoverSpeed = 8f;

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
    private float lastYRotation;
    private float currentBankAngle;

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

        // Angular velocity calculation for banking and pivot footwork
        float deltaAngle = Mathf.DeltaAngle(lastYRotation, transform.eulerAngles.y);
        lastYRotation = transform.eulerAngles.y;
        float angularVelocity = Time.deltaTime > 0.0001f ? (deltaAngle / Time.deltaTime) : 0f;

        // Dynamic turn banking: smoothly roll torso into the turn
        float targetBank = Mathf.Clamp(-angularVelocity * 0.035f, -maxBankAngle, maxBankAngle);
        if (speed < 0.2f || isInactive || state == HunterState.Dead) targetBank = 0f;
        currentBankAngle = Mathf.MoveTowards(
            currentBankAngle,
            targetBank,
            Time.deltaTime * (Mathf.Abs(targetBank) > Mathf.Abs(currentBankAngle) ? bankSpeed : bankRecoverSpeed));

        if (model != null)
        {
            model.localRotation = Quaternion.Euler(0f, 0f, currentBankAngle);
        }

        animator.SetBool(IsInactiveParam, isInactive);

        // Stride matching & Pivot shuffling:
        if (isInactive || state == HunterState.Dead)
        {
            animator.SetFloat(SpeedParam, 0f);
            animator.speed = 1f;
        }
        else if (speed > 0.05f)
        {
            animator.SetFloat(SpeedParam, speed);
            if (isChasing)
            {
                // In Chase, sprint animation plays at a crisp, natural playback rate (never slow motion)
                animator.speed = Mathf.Clamp(speed / runAuthoredSpeed, 0.85f, 1.25f);
            }
            else
            {
                animator.speed = Mathf.Clamp(speed / walkAuthoredSpeed, 0.50f, 1.5f);
            }
        }
        else if (Mathf.Abs(angularVelocity) > 25f)
        {
            // Pivot in place: shuffle feet with the turn rate so the zombie never rotates like a frozen statue
            float turnShuffle = Mathf.Clamp01(Mathf.Abs(angularVelocity) / 140f) * 0.35f;
            animator.SetFloat(SpeedParam, turnShuffle);
            animator.speed = Mathf.Clamp(turnShuffle / walkAuthoredSpeed, 0.45f, 1.0f);
        }
        else
        {
            animator.SetFloat(SpeedParam, 0f);
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
        // Layer 1: Upper Body Layer is not used to override the sprint with a walk clip during Chase,
        // allowing Zombie Run.fbx to play its full authentic sprint motion without distortion.
        bool isChasing = hunter.State == HunterState.Chase;
        animator.SetBool(IsChasingParam, isChasing);

        float targetUpperWeight = 0f;
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
