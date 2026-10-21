using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * ThiefAudioDriver:
 * Drives 3D spatialized audio for the Thief agent.
 * Plays the looping running audio (thief running.mp3) while the Thief is actively moving / traveling
 * (GoToItem, GoToStash), and fades out when stationary, opening doors, stealing, delivering, dead or vanished.
 */
[RequireComponent(typeof(ThiefAgent), typeof(NavMeshAgent))]
public class ThiefAudioDriver : MonoBehaviour
{
    [Header("Clips")]
    [Tooltip("Looping running audio clip (e.g. thief running.mp3).")]
    [SerializeField] private AudioClip runClip;

    [Header("Audio Source (auto-created if null)")]
    [SerializeField] private AudioSource runSource;

    [Header("Volume & Tuning")]
    [Tooltip("Volume for the running sound.")]
    [SerializeField, Range(0f, 1f)] private float runVolume = 0.85f;

    [Tooltip("Speed to fade audio in and out (5.0 = ~0.2s).")]
    [SerializeField] private float fadeSpeed = 5f;

    [Tooltip("Pitch scales subtly with speed for natural pacing.")]
    [SerializeField] private bool scalePitchWithSpeed = true;

    [Tooltip("Authored base running speed for pitch = 1.0.")]
    [SerializeField] private float baseRunSpeed = 6f;

    [Header("Spatial Audio")]
    [Tooltip("Spatial blend for 3D positional audio (0.45 = clear 3D direction without being silent from distance).")]
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 0.45f;

    [Tooltip("Distance at which audio begins attenuating.")]
    [SerializeField] private float minDistance = 2f;

    [Tooltip("Maximum distance at which the player can hear the Thief.")]
    [SerializeField] private float maxDistance = 22f;

    private ThiefAgent thief;
    private NavMeshAgent agent;
    private float currentVolume;

    private void Awake()
    {
        thief = GetComponent<ThiefAgent>();
        agent = GetComponent<NavMeshAgent>();

        EnsureAudioSource();
    }

    private void OnEnable()
    {
        if (thief != null)
        {
            thief.OnStateChanged += HandleStateChanged;
            thief.OnSpawned      += HandleSpawned;
            thief.OnDied         += HandleDied;
            thief.OnVanished     += HandleVanished;
        }
    }

    private void OnDisable()
    {
        if (thief != null)
        {
            thief.OnStateChanged -= HandleStateChanged;
            thief.OnSpawned      -= HandleSpawned;
            thief.OnDied         -= HandleDied;
            thief.OnVanished     -= HandleVanished;
        }

        if (runSource != null)
        {
            runSource.Stop();
            runSource.volume = 0f;
        }
    }

    private void Update()
    {
        if (thief == null || agent == null || runSource == null) return;

        bool isTraveling = thief.IsInPlay &&
                           (thief.State == ThiefState.GoToItem || thief.State == ThiefState.GoToStash) &&
                           agent.velocity.magnitude > 0.15f;

        float targetVol = isTraveling ? runVolume : 0f;
        currentVolume = Mathf.MoveTowards(currentVolume, targetVol, Time.deltaTime * fadeSpeed);
        runSource.volume = currentVolume;

        if (currentVolume > 0.001f)
        {
            if (!runSource.isPlaying && runClip != null)
            {
                runSource.clip = runClip;
                runSource.loop = true;
                runSource.Play();
            }

            if (scalePitchWithSpeed && baseRunSpeed > 0f)
            {
                float speed = agent.velocity.magnitude;
                float pitch = Mathf.Clamp(speed / baseRunSpeed, 0.75f, 1.25f);
                runSource.pitch = pitch;
            }
        }
        else if (runSource.isPlaying && targetVol <= 0f)
        {
            runSource.Stop();
        }
    }

    private void HandleStateChanged(ThiefState state)
    {
        if (state == ThiefState.Dead || state == ThiefState.Inactive || state == ThiefState.Vanished || state == ThiefState.WaitAtDoor || state == ThiefState.Stealing || state == ThiefState.Delivering)
        {
            currentVolume = 0f;
            if (runSource != null)
            {
                runSource.volume = 0f;
                runSource.Stop();
            }
        }
    }

    private void HandleSpawned()
    {
        currentVolume = 0f;
        if (runSource != null)
        {
            runSource.Stop();
            runSource.volume = 0f;
        }
    }

    private void HandleDied()
    {
        currentVolume = 0f;
        if (runSource != null)
        {
            runSource.Stop();
            runSource.volume = 0f;
        }
    }

    private void HandleVanished()
    {
        currentVolume = 0f;
        if (runSource != null)
        {
            runSource.Stop();
            runSource.volume = 0f;
        }
    }

    private void EnsureAudioSource()
    {
        if (runSource == null)
        {
            Transform existing = transform.Find("AudioSource_Run");
            if (existing != null)
            {
                runSource = existing.GetComponent<AudioSource>();
            }
            else
            {
                GameObject go = new GameObject("AudioSource_Run");
                go.transform.SetParent(transform, false);
                runSource = go.AddComponent<AudioSource>();
            }
        }

        runSource.playOnAwake = false;
        runSource.loop = true;
        runSource.spatialBlend = spatialBlend;
        runSource.minDistance = minDistance;
        runSource.maxDistance = maxDistance;
        runSource.rolloffMode = AudioRolloffMode.Linear;
        runSource.dopplerLevel = 0f;

        if (runClip != null && runSource.clip == null)
            runSource.clip = runClip;
    }
}
