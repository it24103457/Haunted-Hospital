using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * HunterAudioDriver (Option B - Stride & Animation Synced Audio):
 * Drives the Hunter zombie audio with frame-accurate footstep synchronization and 3D spatial presence.
 *
 * Sound design:
 *   - Per-stride footstep one-shots: triggers at the exact foot-down frame of the locomotion animation.
 *     Run strides trigger at normalizedTime ~0.25 (left) and ~0.75 (right).
 *     Walk strides trigger at normalizedTime ~0.35 (left) and ~0.85 (right).
 *     Zero footsteps when stationary or transitioning -- eliminating foot sliding audio.
 *   - One-shot growl/roar on Chase alert, Attack, Flinch, and Spawn with anti-spam cooldowns.
 *   - Linear 3D rolloff (min 3m, max 25m) with balanced spatialBlend (0.45) so audio has clear
 *     spatial direction while remaining distinctly audible through hospital corridors.
 */
[RequireComponent(typeof(HunterBrain), typeof(NavMeshAgent))]
public class HunterAudioDriver : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // Inspector fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Clips - Footsteps")]
    [Tooltip("Individual footstep sounds for Chase (alternated or randomized).")]
    [SerializeField] private AudioClip[] runStepClips;

    [Tooltip("Individual footstep sounds for Patrol / Investigate / Search.")]
    [SerializeField] private AudioClip[] walkStepClips;

    [Header("Clips - Voice")]
    [Tooltip("One-shot growl/roar played on alerts, attacks, and flinches.")]
    [SerializeField] private AudioClip growlClip;

    [Header("Audio Sources (auto-created if null)")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioSource growlSource;

    [Header("Volume & Tuning")]
    [Tooltip("Volume for footstep sounds.")]
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.85f;

    [Tooltip("Volume for growl one-shots.")]
    [SerializeField, Range(0f, 1f)] private float growlVolume = 1.0f;

    [Tooltip("Random pitch variation per footstep (+/- around 1.0) for natural sound.")]
    [SerializeField, Range(0f, 0.15f)] private float footstepPitchVariation = 0.06f;

    [Header("Timing")]
    [Tooltip("Minimum seconds between successive Chase alert growls.")]
    [SerializeField] private float growlCooldown = 4f;

    [Tooltip("Minimum seconds between Attack/Flinch growls.")]
    [SerializeField] private float attackGrowlCooldown = 1.5f;

    [Header("Spatial Audio")]
    [Tooltip("Spatial blend for footstep and growl sounds. 0.45 = clear 3D direction without being silent from distance.")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 0.45f;

    [Tooltip("Distance at which audio begins attenuating.")]
    [SerializeField] private float minAudioDistance = 3f;

    [Tooltip("Maximum distance at which the player can hear the Hunter.")]
    [SerializeField] private float maxAudioDistance = 25f;

    // ─────────────────────────────────────────────────────────────────────────
    // Private state
    // ─────────────────────────────────────────────────────────────────────────

    private HunterBrain hunter;
    private NavMeshAgent agent;
    private Animator animator;

    private float nextAlertGrowlTime;
    private float nextAttackGrowlTime;

    private int lastRunStepIndex = -1;
    private int lastWalkStepIndex = -1;
    private int clipIndex;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        hunter   = GetComponent<HunterBrain>();
        agent    = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        EnsureAudioSource(ref footstepSource, "AudioSource_Footsteps");
        EnsureAudioSource(ref growlSource,    "AudioSource_Growl");
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
        if (!hunter.IsAlive || animator == null) return;

        float speed = agent.velocity.magnitude;
        if (speed <= 0.1f)
        {
            // Reset step indices when stopped so resuming immediately detects the next contact
            lastRunStepIndex  = -1;
            lastWalkStepIndex = -1;
            return;
        }

        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float norm = stateInfo.normalizedTime % 1.0f;
        if (norm < 0f) norm += 1.0f;

        // Run stride tracking: Left foot ~0.25, Right foot ~0.75
        if (stateInfo.IsName("Run"))
        {
            int stepIndex = (norm >= 0.22f && norm < 0.72f) ? 0 : 1;
            if (stepIndex != lastRunStepIndex)
            {
                lastRunStepIndex = stepIndex;
                PlayStepSound(runStepClips);
            }
        }
        // Walk stride tracking: Left foot ~0.35, Right foot ~0.85
        else if (stateInfo.IsName("Walk"))
        {
            int stepIndex = (norm >= 0.32f && norm < 0.82f) ? 0 : 1;
            if (stepIndex != lastWalkStepIndex)
            {
                lastWalkStepIndex = stepIndex;
                PlayStepSound(walkStepClips);
            }
        }
        else
        {
            lastRunStepIndex  = -1;
            lastWalkStepIndex = -1;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Footstep Playback
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Plays an individual footstep sound with pitch variation.
    /// Can also be called directly via Animation Events on the model.
    /// </summary>
    public void OnFootstep(int footIndex = 0)
    {
        bool isChasing = hunter != null && hunter.State == HunterState.Chase;
        PlayStepSound(isChasing ? runStepClips : walkStepClips);
    }

    private void PlayStepSound(AudioClip[] clips)
    {
        if (footstepSource == null || clips == null || clips.Length == 0) return;

        // Alternate or pick valid clip
        clipIndex = (clipIndex + 1) % clips.Length;
        AudioClip clip = clips[clipIndex];
        if (clip == null) return;

        // Pitch variation prevents machine-gun monotony
        float pitchMod = 1f + Random.Range(-footstepPitchVariation, footstepPitchVariation);
        footstepSource.pitch = pitchMod;
        footstepSource.PlayOneShot(clip, footstepVolume);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Brain event handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleStateChanged(HunterState state)
    {
        switch (state)
        {
            case HunterState.Chase:
                PlayAlertGrowl();
                break;

            case HunterState.Attack:
                PlayAttackGrowl();
                break;
        }
    }

    private void HandleDamaged(DamageInfo info)
    {
        PlayAttackGrowl();
    }

    private void HandleDied()
    {
        if (footstepSource != null) footstepSource.Stop();
        if (growlSource    != null) growlSource.Stop();
    }

    private void HandleSpawned()
    {
        PlayGrowlDirect();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Growl helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void PlayAlertGrowl()
    {
        float now = Time.time;
        if (now < nextAlertGrowlTime) return;
        nextAlertGrowlTime = now + growlCooldown;
        PlayGrowlDirect();
    }

    private void PlayAttackGrowl()
    {
        float now = Time.time;
        if (now < nextAttackGrowlTime) return;
        nextAttackGrowlTime = now + attackGrowlCooldown;
        PlayGrowlDirect();
    }

    private void PlayGrowlDirect()
    {
        if (growlSource == null || growlClip == null) return;
        growlSource.pitch = 1f + Random.Range(-0.04f, 0.04f);
        growlSource.PlayOneShot(growlClip, growlVolume);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Source initialization
    // ─────────────────────────────────────────────────────────────────────────

    private void EnsureAudioSource(ref AudioSource src, string goName)
    {
        if (src != null)
        {
            ConfigureSource(src);
            return;
        }

        // Check if child already exists
        Transform child = transform.Find(goName);
        if (child != null)
        {
            src = child.GetComponent<AudioSource>();
        }

        if (src == null)
        {
            GameObject go = new GameObject(goName);
            go.transform.SetParent(transform, false);
            src = go.AddComponent<AudioSource>();
        }

        ConfigureSource(src);
    }

    private void ConfigureSource(AudioSource src)
    {
        src.playOnAwake  = false;
        src.loop         = false;
        src.spatialBlend = spatialBlend;
        src.minDistance  = minAudioDistance;
        src.maxDistance  = maxAudioDistance;
        src.rolloffMode  = AudioRolloffMode.Linear;
        src.volume       = 1.0f;
    }
}
