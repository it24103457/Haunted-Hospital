using System;
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
 *   Run authored speed      = 2.60 m/s (Zombie Run.fbx forward displacement).
 *   HunterSettings multipliers: patrol=0.70->0.98 m/s, search=0.80->1.12 m/s,
 *                               investigate=1.00->1.40 m/s, chase=2.5->3.5 m/s.
 *   Chase ratio: clamped to a 0.85x minimum so the run never looks like slow motion.
 *
 * Attack:
 *   The brain hits once per attack cooldown for as long as it stays in Attack. A swing starts on entering Attack,
 *   and a new one starts each time the previous swing has finished while the Hunter is still attacking, so it keeps
 *   swinging for the whole attack instead of standing idle after the first swing. While in Attack the Speed parameter
 *   is held at 0, because the Hunter is still braking from its run (and turning to face the player) when the attack
 *   starts, and a Speed above 0.1 would fire the Attack -> Walk/Run transitions and cut the swing off.
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

    [Header("Attack Tuning")]
    [Tooltip("Crossfade duration into each attack swing.")]
    [SerializeField] private float attackFadeSeconds = 0.1f;

    [Header("Hit & Facing Tuning")]
    [Tooltip("Minimum seconds between successive flinch animations. " +
             "Keep this below the weapon cadence (0.5 s) so every shot produces a reaction.")]
    [SerializeField] private float flinchCooldown = 0.2f;

    [Tooltip("Seconds the Hunter is locked in the flinch state. " +
             "Keep this in sync with the Flinch state's exit time (0.8 of the 0.5 s trimmed clip).")]
    [SerializeField] private float flinchDuration = 0.4f;

    [Tooltip("Crossfade duration into the flinch. Short and aggressive so the hit reads immediately.")]
    [SerializeField] private float flinchFadeSeconds = 0.06f;

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

    /// <summary>Raised only when a damage event actually starts a flinch (cooldown passed).</summary>
    public event Action FlinchStarted;

    /// <summary>How long the movement lock should last, matching the Flinch state's exit window.</summary>
    public float FlinchDuration => flinchDuration;

    // Animator parameter hashes
    private static readonly int SpeedParam      = Animator.StringToHash("Speed");
    private static readonly int AttackTrigger   = Animator.StringToHash("Attack");
    private static readonly int AttackStateHash = Animator.StringToHash("Attack");
    private static readonly int FlinchStateHash = Animator.StringToHash("Flinch");
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
        UpdateAttackSwings();
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
        bool isAttacking  = state == HunterState.Attack;
        bool flinching    = IsFlinchActive();
        float speed       = agent.velocity.magnitude;

        // Angular velocity calculation for banking and pivot footwork
        float deltaAngle = Mathf.DeltaAngle(lastYRotation, transform.eulerAngles.y);
        lastYRotation = transform.eulerAngles.y;
        float angularVelocity = Time.deltaTime > 0.0001f ? (deltaAngle / Time.deltaTime) : 0f;

        // Dynamic turn banking: smoothly roll torso into the turn
        float targetBank = Mathf.Clamp(-angularVelocity * 0.035f, -maxBankAngle, maxBankAngle);
        if (speed < 0.2f || isInactive || isAttacking || state == HunterState.Dead) targetBank = 0f;
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
        if (isInactive || isAttacking || state == HunterState.Dead)
        {
            // Attacking: the agent is still braking from its run and turning to face the player. Speed stays 0 so
            // the Attack -> Walk/Run transitions cannot cut the swing short, and the swing plays at authored speed.
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

        // One-shots play at authored speed. Without this guard the stride matching below
        // overwrites the 1x speed set on damage and the hit reaction crawls at 0.5-0.85x.
        if (flinching)
            animator.speed = 1f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Attack
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateAttackSwings()
    {
        // The swing clip is longer than the brain's attack cooldown, so each swing plays to the end and the next one
        // starts once it has finished, keeping the Hunter swinging for as long as it stays in Attack.
        if (hunter.State != HunterState.Attack) return;
        if (IsAttackActive() || IsFlinchActive()) return;

        StartSwing();
    }

    private void StartSwing()
    {
        animator.speed = 1f;
        animator.ResetTrigger(AttackTrigger);

        // A shot landing as the attack starts plays the flinch; the swing starts once the flinch has finished.
        if (IsFlinchActive()) return;

        // Drive the state directly, like the flinch, so a swing is never queued behind a running transition.
        animator.CrossFadeInFixedTime(AttackStateHash, attackFadeSeconds, BaseLayerIndex);
    }

    private bool IsAttackActive()
    {
        if (animator == null) return false;

        if (animator.GetCurrentAnimatorStateInfo(BaseLayerIndex).shortNameHash == AttackStateHash)
            return true;

        return animator.IsInTransition(BaseLayerIndex) &&
               animator.GetNextAnimatorStateInfo(BaseLayerIndex).shortNameHash == AttackStateHash;
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
            // The first swing starts as soon as the Hunter enters Attack; its first hit lands one wind-up later.
            StartSwing();
        }
    }

    private void HandleDamaged(DamageInfo info)
    {
        if (animator == null || !hunter.IsAlive) return;
        if (Time.time < nextFlinchTime) return;

        nextFlinchTime = Time.time + flinchCooldown;
        animator.speed = 1f;

        // Drive the state directly instead of using the AnyState trigger. A trigger is
        // queued behind any transition that is already running; CrossFade interrupts it.
        if (IsFlinchActive())
        {
            // CrossFade does not restart a state that is already current. Play does.
            animator.Play(FlinchStateHash, BaseLayerIndex, 0f);
        }
        else
        {
            animator.CrossFadeInFixedTime(FlinchStateHash, flinchFadeSeconds, BaseLayerIndex);
        }

        // The mover listens to this instead of OnDamaged so a hit suppressed by the
        // cooldown cannot pause movement without a matching animation.
        FlinchStarted?.Invoke();
    }

    private bool IsFlinchActive()
    {
        if (animator == null) return false;

        if (animator.GetCurrentAnimatorStateInfo(BaseLayerIndex).shortNameHash == FlinchStateHash)
            return true;

        return animator.IsInTransition(BaseLayerIndex) &&
               animator.GetNextAnimatorStateInfo(BaseLayerIndex).shortNameHash == FlinchStateHash;
    }

    private void HandleDied()
    {
        if (animator == null) return;

        animator.speed = 1f;
        animator.ResetTrigger(AttackTrigger);
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

        nextFlinchTime = 0f;
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
