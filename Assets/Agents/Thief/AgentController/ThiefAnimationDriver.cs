using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * Maps ThiefAgent's published state onto the humanoid Animator on the model under Model. Locomotion blends
 * Idle and Run by the NavMeshAgent's speed, scaled while moving so the Run clip's strides match the agent's
 * actual speed; every other state plays its one-shot clip and returns to Locomotion on exit time (the Grab
 * clip holds for a minimum display time because Stealing itself lasts one frame). On death
 * ThiefAgent hides Model the same frame, so the driver shows it again, plays the Death clip and hides it
 * once the clip has finished; a respawn resets everything to Locomotion. The Animator never gets root
 * motion: the NavMeshAgent moves the Thief.
 */
[RequireComponent(typeof(ThiefAgent), typeof(NavMeshAgent))]
public class ThiefAnimationDriver : MonoBehaviour
{
    [Tooltip("Transform hidden and shown on death/vanish; ThiefAgent's own model reference points here too.")]
    [SerializeField] private Transform model;

    [Tooltip("Animator on the rigged model child; uses AC_Thief.")]
    [SerializeField] private Animator animator;

    [Tooltip("Forward metres per second of the authored Run clip; strides are scaled by agent speed over this.")]
    [SerializeField] private float runAuthoredSpeed = 6.43f;

    [Tooltip("Seconds the Grab clip must stay visible; Stealing ends in one frame, so the crossfade to Locomotion is delayed this long.")]
    [SerializeField] private float minGrabSeconds = 0.85f;

    private const float Fade = 0.2f;
    private static readonly int SpeedParam = Animator.StringToHash("Speed");
    private static readonly int IsCarryingParam = Animator.StringToHash("IsCarrying");
    private static readonly int FlinchTrigger = Animator.StringToHash("Flinch");

    private static readonly Dictionary<ThiefState, string> StateClips = new Dictionary<ThiefState, string>
    {
        { ThiefState.WaitAtDoor, "DoorOpen" },
        { ThiefState.Stealing, "Grab" },
        { ThiefState.Delivering, "Stash" },
        { ThiefState.Dead, "Death" },
    };

    [Header("Vanish Polish")]
    [Tooltip("Seconds into Delivering before the ghost dissolve begins (stashPause is 3s, so ~2.2s gives 0.8s of smooth vanish).")]
    [SerializeField] private float vanishFadeDelay = 2.2f;

    [Tooltip("Duration of the smooth sink/fade out before vanishing.")]
    [SerializeField] private float vanishFadeDuration = 0.8f;

    [Header("Flinch / Death Facing")]
    [Tooltip("Seconds over which the Thief snaps to face the damage source on hit.")]
    [SerializeField] private float flinchTurnDuration = 0.15f;

    [Tooltip("Minimum seconds between successive flinch triggers to avoid animation spam.")]
    [SerializeField] private float flinchCooldown = 0.6f;

    private ThiefAgent thief;
    private NavMeshAgent agent;
    private Coroutine hideRoutine;
    private Coroutine grabHold;
    private Coroutine vanishRoutine;
    private Coroutine flinchRoutine;
    private float grabTime = float.NegativeInfinity;
    private float lastKnownHealth;
    private float lastFlinchTime = float.NegativeInfinity;
    private Vector3 modelBaseLocalPos;
    private Vector3 modelBaseLocalScale = Vector3.one;

    private void Awake()
    {
        thief = GetComponent<ThiefAgent>();
        agent = GetComponent<NavMeshAgent>();
        if (animator == null && model != null) animator = model.GetComponentInChildren<Animator>();
        if (model != null)
        {
            modelBaseLocalPos = model.localPosition;
            modelBaseLocalScale = model.localScale;
        }
        lastKnownHealth = thief.MaxHealth;
    }

    private void OnEnable()
    {
        thief.OnStateChanged += HandleStateChanged;
        thief.OnSpawned += HandleSpawned;
    }

    private void OnDisable()
    {
        thief.OnStateChanged -= HandleStateChanged;
        thief.OnSpawned -= HandleSpawned;
        if (vanishRoutine != null) { StopCoroutine(vanishRoutine); vanishRoutine = null; }
        if (flinchRoutine != null) { StopCoroutine(flinchRoutine); flinchRoutine = null; }
        ResetModelTransform();
    }

    private void Update()
    {
        if (animator == null || !animator.gameObject.activeInHierarchy) return;

        // Detect non-lethal damage by polling health (ThiefAgent has no OnDamaged event)
        if (thief.IsInPlay && thief.Health < lastKnownHealth && thief.Health > 0f)
        {
            OnDamageDetected();
        }
        lastKnownHealth = thief.Health;

        float speed = agent.velocity.magnitude;
        animator.SetFloat(SpeedParam, speed);

        float held = Time.time - grabTime;
        bool isPlayingGrab = held < minGrabSeconds;

        // Only activate upper-body carry pose while actively traveling to stash with the item and after grab finishes.
        // During WaitAtDoor, Stealing, Delivering, Dead or Grab playback, this drops to false so action clips play clean.
        bool isCarryingToStash = thief.IsCarrying && thief.State == ThiefState.GoToStash && !isPlayingGrab;
        animator.SetBool(IsCarryingParam, isCarryingToStash);

        // Stride scaling only while actually moving and not playing grab, or the one-shots would crawl at near-zero speed.
        bool locomoting = (thief.State == ThiefState.GoToItem || thief.State == ThiefState.GoToStash) && !isPlayingGrab;
        animator.speed = locomoting && runAuthoredSpeed > 0f ? speed / runAuthoredSpeed : 1f;
    }

    private void HandleStateChanged(ThiefState state)
    {
        if (hideRoutine != null) { StopCoroutine(hideRoutine); hideRoutine = null; }
        if (grabHold != null) { StopCoroutine(grabHold); grabHold = null; }
        if (vanishRoutine != null) { StopCoroutine(vanishRoutine); vanishRoutine = null; }
        if (flinchRoutine != null) { StopCoroutine(flinchRoutine); flinchRoutine = null; }

        if (state == ThiefState.Dead)
        {
            ResetModelTransform();
            FacePlayerInstant();
            if (model != null) model.gameObject.SetActive(true);
            if (animator != null)
            {
                animator.speed = 1f;
                animator.Play("Death", 0, 0f);
                hideRoutine = StartCoroutine(HideAfterClip());
            }
            return;
        }

        if (state == ThiefState.Inactive || state == ThiefState.Vanished || animator == null) return;

        if (state == ThiefState.Delivering)
        {
            ResetModelTransform();
            vanishRoutine = StartCoroutine(SmoothVanishAfterDelivery());
        }

        if (StateClips.TryGetValue(state, out string clip))
        {
            if (state == ThiefState.Stealing) grabTime = Time.time;
            animator.CrossFadeInFixedTime(clip, Fade);
            return;
        }

        // Stealing ends in a single frame, so the blend back to Locomotion waits out the Grab hold
        // instead of firing one frame after the Grab crossfade started; any newer state stops it above.
        float held = Time.time - grabTime;
        if (held < minGrabSeconds)
            grabHold = StartCoroutine(ResumeLocomotionAfterGrab(minGrabSeconds - held));
        else
            animator.CrossFadeInFixedTime("Locomotion", Fade);
    }

    private void HandleSpawned()
    {
        if (hideRoutine != null) { StopCoroutine(hideRoutine); hideRoutine = null; }
        if (grabHold != null) { StopCoroutine(grabHold); grabHold = null; }
        if (vanishRoutine != null) { StopCoroutine(vanishRoutine); vanishRoutine = null; }
        if (flinchRoutine != null) { StopCoroutine(flinchRoutine); flinchRoutine = null; }
        ResetModelTransform();
        grabTime = float.NegativeInfinity;
        lastKnownHealth = thief.Health;
        lastFlinchTime = float.NegativeInfinity;
        if (animator == null) return;
        animator.speed = 1f;
        animator.CrossFadeInFixedTime("Locomotion", Fade);
    }

    private IEnumerator SmoothVanishAfterDelivery()
    {
        if (vanishFadeDelay > 0f)
            yield return new WaitForSeconds(vanishFadeDelay);

        if (model == null) yield break;

        float elapsed = 0f;
        while (elapsed < vanishFadeDuration)
        {
            if (model == null || thief.State != ThiefState.Delivering) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / vanishFadeDuration);
            model.localPosition = Vector3.Lerp(modelBaseLocalPos, modelBaseLocalPos - Vector3.up * 0.9f, t);
            model.localScale = Vector3.Lerp(modelBaseLocalScale, new Vector3(modelBaseLocalScale.x * 0.1f, modelBaseLocalScale.y * 0.01f, modelBaseLocalScale.z * 0.1f), t);
            yield return null;
        }

        vanishRoutine = null;
    }

    private void ResetModelTransform()
    {
        if (model != null)
        {
            model.localPosition = modelBaseLocalPos;
            model.localScale = modelBaseLocalScale;
        }
    }

    private IEnumerator ResumeLocomotionAfterGrab(float wait)
    {
        yield return new WaitForSeconds(wait);
        grabHold = null;
        // Only blend back if nothing newer took over the Animator while waiting.
        if (animator != null && (thief.State == ThiefState.GoToItem || thief.State == ThiefState.GoToStash))
            animator.CrossFadeInFixedTime("Locomotion", Fade);
    }

    private IEnumerator HideAfterClip()
    {
        // Wait out the clip itself (fixed duration 1x playback), then fade the hidden model out of sight.
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        yield return new WaitForSeconds(info.length + 0.2f);
        if (thief.State == ThiefState.Dead && model != null) model.gameObject.SetActive(false);
        hideRoutine = null;
    }

    // ── Flinch / Death facing ────────────────────────────────────────

    private void OnDamageDetected()
    {
        if (animator == null || thief.State == ThiefState.Dead) return;
        if (Time.time - lastFlinchTime < flinchCooldown) return;

        lastFlinchTime = Time.time;

        // Trigger flinch animation and reset stride scaling so it plays at authored speed
        animator.speed = 1f;
        animator.SetTrigger(FlinchTrigger);

        // Snap-rotate to face the player (damage source)
        if (flinchRoutine != null) StopCoroutine(flinchRoutine);
        flinchRoutine = StartCoroutine(FacePlayerOnHit());
    }

    private IEnumerator FacePlayerOnHit()
    {
        PlayerStats player = FindAnyObjectByType<PlayerStats>();
        if (player == null) yield break;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) yield break;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        float elapsed = 0f;

        while (elapsed < flinchTurnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / flinchTurnDuration);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, t);
            yield return null;
        }

        transform.rotation = targetRot;
        flinchRoutine = null;
    }

    private void FacePlayerInstant()
    {
        PlayerStats player = FindAnyObjectByType<PlayerStats>();
        if (player == null) return;

        Vector3 dir = player.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized);
    }
}
