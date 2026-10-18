using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/*
 * The escape cutscene (part one), played in its own scene after the key is turned in the lift.
 * Fades in facing the key switch, then turns to look back out of the open lift doors as the dimmed lights start to flicker.
 * The doors start sliding shut with the lift sound (its silent start skipped) and jam partway with a slam when that sound is cut;
 * slow echoing footsteps follow
 * from the corridor side as the Hunter walks slowly along the lobby and in through the doors without ever stopping,
 * turning its head to the camera and growling as it comes in.
 * Meanwhile the held syringe gun comes up and is fired, but only clicks empty; the player looks down at it, shakes it,
 * catches a panicked breath, looks back up, winds the gun back and throws it. The walk is timed so the gun hits the Hunter's head
 * just as it steps inside the lift; it flinches straight back out past the door line, the gun drops to the lobby floor outside,
 * and the doors slam shut. The screen fades to black and the end panel ("YOU ESCAPED", Play Again, Main Menu) fades in.
 * Space, Esc or a click skips straight to the end panel; a small "SPACE Skip" hint shows for a few seconds at the start.
 * Everything runs on real time. Camera and Hunter positions come from marker objects in the scene, so shots can be adjusted there.
 * The lift doors are this scene's own copy of the elevator; their panels are moved directly, and the door animation is switched off.
 */
public class EscapeCutscene : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform camStart;
    [SerializeField] private Transform camLookBack;
    [SerializeField] private Transform hunterStart;
    [Tooltip("Where the Hunter stops, just outside the lift doors, facing in.")]
    [FormerlySerializedAs("hunterGrab")]
    [SerializeField] private Transform hunterStop;
    [SerializeField] private Animator hunter;
    [SerializeField] private HunterDoorHold hunterHold;
    [SerializeField] private Transform heldGun;
    [SerializeField] private Light[] flickerLights;
    [Tooltip("How bright the flickering lights are compared to their normal brightness.")]
    [SerializeField, Range(0f, 1f)] private float lightDim = 0.6f;

    [Header("Lift doors (local X of each panel)")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private Transform doorLeft;
    [SerializeField] private Transform doorRight;
    [SerializeField] private float leftOpenX = -1.408f;
    [SerializeField] private float leftClosedX = -0.658f;
    [SerializeField] private float rightOpenX = 1.567f;
    [SerializeField] private float rightClosedX = 0.835f;
    [Tooltip("How far the doors slide while the Hunter approaches, before the final slam (0 open, 1 shut).")]
    [SerializeField, Range(0f, 1f)] private float doorsWaitAt = 0.3f;

    [Header("Hunter")]
    [Tooltip("Walking speed in metres per second. The walk is stretched if needed so it reaches its stop point as the gun hits.")]
    [SerializeField, Min(0.1f)] private float walkSpeed = 1.1f;
    [Tooltip("Speed the walk animation was made for, in metres per second; the animation is sped up or slowed to match the actual walk.")]
    [SerializeField, Min(0.1f)] private float walkAnimSpeed = 1.1f;
    [Tooltip("How far the hit pushes it back past the door line, in metres.")]
    [SerializeField, Min(0f)] private float knockOutside = 0.45f;
    [Tooltip("How far before the point in front of the doors the Hunter counts as in view; the gun beat starts from there.")]
    [SerializeField, Min(0f)] private float inViewBeforeDoors = 0.8f;

    [Header("Sounds")]
    [SerializeField] private AudioClip liftStart;
    [Tooltip("Where in the lift sound playback starts, in seconds; the silent start of the file is skipped.")]
    [SerializeField, Min(0f)] private float liftStartFrom = 2f;
    [Tooltip("How many seconds earlier than the footsteps' timing point the lift sound and doors start.")]
    [SerializeField, Min(0f)] private float liftLead = 1f;
    [Tooltip("Where in the lift sound it is cut, in seconds.")]
    [SerializeField, Min(0.1f)] private float liftCutAt = 6f;
    [SerializeField] private AudioClip footsteps;
    [Tooltip("Seconds after the lift sound starts before the footsteps begin and the Hunter starts walking.")]
    [SerializeField, Min(0f)] private float footstepsDelay = 1.5f;
    [Tooltip("Played when the doors jam partway, at the moment the lift sound is cut.")]
    [FormerlySerializedAs("hunterSlam")]
    [SerializeField] private AudioClip doorsJam;
    [SerializeField] private AudioClip growl;
    [SerializeField] private AudioClip emptyShot;
    [SerializeField] private AudioClip panicBreath;
    [SerializeField] private AudioClip gunThump;
    [Tooltip("Silent start of the thump sound that is skipped, in seconds, so it lands exactly on the hit.")]
    [SerializeField, Min(0f)] private float gunThumpFrom = 0.5f;
    [SerializeField] private AudioClip doorsShut;
    [Tooltip("How many seconds before the doors finish shutting their sound starts; the sound's loudest moment is 0.2 s in, so 0.2 lands it on the shut.")]
    [SerializeField, Min(0f)] private float doorsShutEarly = 0.2f;
    [SerializeField] private AudioClip winSound;

    [Header("Screen")]
    [SerializeField] private CanvasGroup fade;
    [SerializeField] private CanvasGroup skipHint;
    [SerializeField] private CanvasGroup endPanel;

    private Vector3 camBasePos;
    private Quaternion camBaseRot;
    private float shakeUntil;
    private float shakeStrength;
    private float doorsClosed;
    private bool flickering;
    private bool skipping;
    private bool hunterLooking;
    private float[] lightBase;
    private AudioSource liftSource;
    private AudioSource footstepSource;
    private AudioSource hunterVoice;

    private void Awake()
    {
        if (doorAnimator != null) doorAnimator.enabled = false;
        lightBase = new float[flickerLights.Length];
        for (int i = 0; i < flickerLights.Length; i++) lightBase[i] = flickerLights[i].intensity;

        liftSource = NewSource(gameObject, 0f);
        footstepSource = NewSource(hunter.gameObject, 1f);
        hunterVoice = NewSource(hunter.gameObject, 1f);

        hunter.gameObject.SetActive(false);
        heldGun.gameObject.SetActive(false);
        endPanel.gameObject.SetActive(false);
        skipHint.gameObject.SetActive(false);
        fade.gameObject.SetActive(true);
        fade.alpha = 1f;
        SetDoors(0f);

        camBasePos = camStart.position;
        camBaseRot = camStart.rotation;
        cam.transform.SetPositionAndRotation(camBasePos, camBaseRot);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Start()
    {
        StartCoroutine(Run());
        StartCoroutine(ShowSkipHint());
    }

    private void Update()
    {
        if (flickering) Flicker();
        if (hunterLooking) hunterHold.lookAt = cam.transform.position;
        if (skipping) return;

        bool skip = (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
                    || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (skip)
        {
            skipping = true;
            StopAllCoroutines();
            StartCoroutine(SkipToEnd());
        }
    }

    // The camera follows its base pose plus a short shake; the sequence only ever sets the base pose.
    private void LateUpdate()
    {
        Vector3 offset = Vector3.zero;
        if (Time.unscaledTime < shakeUntil)
        {
            float k = (shakeUntil - Time.unscaledTime) * shakeStrength;
            offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * k * 0.04f;
        }
        cam.transform.SetPositionAndRotation(camBasePos + camBaseRot * offset, camBaseRot);
    }

    private IEnumerator Run()
    {
        // Fade in facing the key switch, just after the key was turned.
        yield return FadeTo(fade, 0f, 0.8f);
        yield return Wait(0.6f);

        // Look back out of the open doors; the dimmed lights start to flicker.
        // The lift sound and the doors closing start near the end of the look-back, a set time before the footsteps' timing point.
        StartCoroutine(MoveCamera(camLookBack.position, camLookBack.rotation, 1.4f));
        yield return Wait(1.4f + 0.5f - liftLead);
        flickering = true;
        float liftSoundStarted = Time.unscaledTime;
        liftSource.clip = liftStart;
        liftSource.time = liftStartFrom;
        liftSource.Play();
        // The doors slide partway and jam exactly when the lift sound is cut, with a slam and a jolt.
        float liftPlays = liftCutAt - liftStartFrom;
        StartCoroutine(StopLiftSoundAt(liftSoundStarted + liftPlays));
        StartCoroutine(CloseDoors(doorsWaitAt, liftPlays));
        float liftStarted = liftSoundStarted + liftLead;

        // The Hunter starts walking with the footsteps and never stops: along the lobby, then in through the doors.
        // Once it comes into view at the doors it creeps the rest of the way in over exactly the length of the gun beat,
        // so the thrown gun hits it just as it steps inside the lift.
        StartCoroutine(HunterWalk(liftStarted + footstepsDelay));
        while (!hunterInView) yield return null;

        // Raise the gun towards the doorway and pull the trigger: empty.
        yield return LookAt(new Vector3(hunterStop.position.x, 1.6f, doorLeft.position.z), AimTime);
        heldGun.gameObject.SetActive(true);
        Vector3 gunRest = heldGun.localPosition;
        Quaternion gunRestRot = heldGun.localRotation;
        yield return MoveGun(gunRest + new Vector3(0f, -0.35f, -0.1f), gunRestRot * Quaternion.Euler(0f, 0f, -25f), gunRest, gunRestRot, RaiseTime);
        for (int i = 0; i < 2; i++)
        {
            UIButtonSounds.PlayClip(emptyShot, 0.9f);
            yield return MoveGun(gunRest, gunRestRot, gunRest + new Vector3(0f, 0.02f, -0.05f), gunRestRot * Quaternion.Euler(0f, 0f, 8f), ShotKick);
            yield return MoveGun(heldGun.localPosition, heldGun.localRotation, gunRest, gunRestRot, ShotRecover);
            yield return Wait(ShotGap);
        }

        // React: look down at the gun, shake it, a panicked breath, then back up at the Hunter.
        Quaternion lookingAtHunter = camBaseRot;
        UIButtonSounds.PlayClip(panicBreath, 0.8f);
        StartCoroutine(MoveCamera(camBasePos, lookingAtHunter * Quaternion.Euler(22f, 6f, 0f), ReactDown));
        yield return MoveGun(gunRest, gunRestRot, gunRest + new Vector3(-0.08f, 0.08f, -0.08f), gunRestRot * Quaternion.Euler(20f, 25f, 0f), ReactDown);
        Vector3 checkPos = heldGun.localPosition;
        Quaternion checkRot = heldGun.localRotation;
        for (float t = 0f; t < ReactShake; t += Time.unscaledDeltaTime)
        {
            heldGun.localPosition = checkPos + new Vector3(Mathf.Sin(t * 60f) * 0.015f, Mathf.Cos(t * 47f) * 0.01f, 0f);
            heldGun.localRotation = checkRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 55f) * 6f);
            yield return null;
        }
        hunterVoice.PlayOneShot(growl, 0.7f);
        StartCoroutine(MoveCamera(camBasePos, lookingAtHunter, ReactUp));
        yield return MoveGun(heldGun.localPosition, heldGun.localRotation, gunRest, gunRestRot, ReactUp);
        yield return Wait(ReactPause);

        // Wind up, then throw at its head. The doors' sound starts a set time before they finish shutting.
        yield return MoveGun(gunRest, gunRestRot, gunRest + new Vector3(0.12f, 0.12f, -0.35f), gunRestRot * Quaternion.Euler(0f, 0f, 35f), WindUpTime);
        yield return Wait(ThrowPause);
        StartCoroutine(PlayDoorsShutAt(Time.unscaledTime + ThrowTime + BeforeClose + CloseTime - doorsShutEarly));
        yield return ThrowGun(ThrowTime);

        // Hit: it flinches straight backwards out past the door line, the footsteps stop, and the gun drops on the lobby side.
        hitLanded = true;
        footstepSource.Stop();
        PlayAt(gunThump, HunterHead(), 1f, gunThumpFrom);
        Shake(0.2f, 0.6f);
        hunterLooking = false;
        StartCoroutine(BlendLook(0f, 0.1f));
        hunter.speed = 1f;
        hunter.CrossFade("Hit", 0.05f, 0);
        StartCoroutine(HunterKnockBack(0.6f));
        yield return Wait(BeforeClose);

        // The doors slam shut.
        yield return CloseDoors(1f, CloseTime);
        Shake(0.6f, 1.5f);
        hunter.gameObject.SetActive(false);
        heldGun.gameObject.SetActive(false);
        yield return Wait(1.0f);

        yield return ShowEnd();
    }

    // Fixed timings of the gun beat; their total is how long the Hunter takes to creep in from the doors, so the throw lands as it steps inside.
    private const float AimTime = 0.3f, RaiseTime = 0.3f, ShotKick = 0.05f, ShotRecover = 0.1f, ShotGap = 0.3f;
    private const float ReactDown = 0.25f, ReactShake = 0.35f, ReactUp = 0.2f, ReactPause = 0.1f;
    private const float WindUpTime = 0.6f, ThrowPause = 0.08f, ThrowTime = 0.28f;
    private const float BeforeClose = 0.25f, CloseTime = 0.3f;
    private const float GunBeatLength = AimTime + RaiseTime + 2f * (ShotKick + ShotRecover + ShotGap)
                                        + ReactDown + ReactShake + ReactUp + ReactPause + WindUpTime + ThrowPause + ThrowTime;

    private bool hitLanded;
    private bool hunterInView;

    private IEnumerator PlayDoorsShutAt(float time)
    {
        yield return WaitUntilTime(time);
        PlayAt(doorsShut, hunterStop.position + Vector3.up * 1.2f, 1f);
    }

    // The point on the lobby side of the doors where the Hunter turns in towards the lift.
    private Vector3 DoorFront()
    {
        return new Vector3(hunterStop.position.x, hunterStop.position.y, doorLeft.position.z - 0.8f);
    }

    private IEnumerator StopLiftSoundAt(float time)
    {
        yield return WaitUntilTime(time);
        liftSource.Stop();
        if (hitLanded) yield break;
        PlayAt(doorsJam, new Vector3(hunterStop.position.x, 1.2f, doorLeft.position.z), 1f);
        Shake(0.35f, 0.8f);
    }

    private IEnumerator SkipToEnd()
    {
        skipHint.gameObject.SetActive(false);
        foreach (var s in FindObjectsByType<AudioSource>()) s.Stop();
        yield return FadeTo(fade, 1f, 0.4f);
        hunter.gameObject.SetActive(false);
        heldGun.gameObject.SetActive(false);
        SetDoors(1f);
        yield return ShowEnd();
    }

    // Fades to black, then fades in "YOU ESCAPED" with its buttons and the win sound.
    private IEnumerator ShowEnd()
    {
        yield return FadeTo(fade, 1f, 0.8f);
        flickering = false;
        foreach (var s in FindObjectsByType<AudioSource>()) s.Stop();
        Cursor.visible = true;
        endPanel.gameObject.SetActive(true);
        endPanel.alpha = 0f;
        endPanel.interactable = false;
        endPanel.blocksRaycasts = false;
        UIButtonSounds.PlayClip(winSound, 0.9f);
        yield return FadeTo(endPanel, 1f, 1.2f);
        endPanel.interactable = true;
        endPanel.blocksRaycasts = true;
    }

    private IEnumerator ShowSkipHint()
    {
        yield return Wait(0.8f);
        skipHint.gameObject.SetActive(true);
        skipHint.alpha = 0f;
        yield return FadeTo(skipHint, 1f, 0.4f);
        yield return Wait(4f);
        yield return FadeTo(skipHint, 0f, 0.4f);
        skipHint.gameObject.SetActive(false);
    }

    /*
     * Walks the Hunter along the lobby at walking speed until it comes into view near the doors, then creeps the rest of the way,
     * turning in through the doors to its stop point just inside the lift over exactly the length of the gun beat.
     * It looks at the camera and growls as it comes into view, and the walk animation is sped up or slowed to match
     * how fast it actually moves. It never stops on its own; the hit takes over from here.
     */
    private IEnumerator HunterWalk(float startAt)
    {
        yield return WaitUntilTime(startAt);
        hunter.gameObject.SetActive(true);
        hunter.transform.SetPositionAndRotation(hunterStart.position, hunterStart.rotation);
        hunter.Play("Walk", 0, 0f);
        hunter.SetLayerWeight(1, 0f);
        hunterHold.handWeight = 0f;
        hunterHold.lookWeight = 0f;
        footstepSource.clip = footsteps;
        footstepSource.loop = true;
        footstepSource.Play();

        Vector3 a = hunterStart.position, b = DoorFront(), c = hunterStop.position;
        float first = Vector3.Distance(a, b), total = first + Vector3.Distance(b, c);
        float inViewAt = Mathf.Max(0f, first - inViewBeforeDoors);
        float d = 0f;
        float creepSpeed = (total - inViewAt) / GunBeatLength;

        while (!hitLanded)
        {
            float speed = hunterInView ? creepSpeed : walkSpeed;
            hunter.speed = speed / walkAnimSpeed;
            d = Mathf.Min(total, d + speed * Time.unscaledDeltaTime);

            Vector3 pos = d < first ? Vector3.Lerp(a, b, d / first) : Vector3.Lerp(b, c, (d - first) / (total - first));
            float turn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(first - 0.6f, first + 0.3f, d));
            hunter.transform.SetPositionAndRotation(pos, Quaternion.Slerp(hunterStart.rotation, hunterStop.rotation, turn));

            if (!hunterInView && d >= inViewAt)
            {
                hunterInView = true;
                hunterLooking = true;
                StartCoroutine(BlendLook(1f, 0.6f));
                hunterVoice.PlayOneShot(growl, 1f);
            }
            yield return null;
        }
    }

    // Pushes it straight back until it is a set distance outside the door line.
    private IEnumerator HunterKnockBack(float duration)
    {
        Vector3 from = hunter.transform.position;
        float behindDoors = (from.z - doorLeft.position.z) + knockOutside;
        Vector3 to = from - hunterStop.forward * Mathf.Max(0.2f, behindDoors);
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            hunter.transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
    }

    private IEnumerator BlendLook(float to, float duration)
    {
        float from = hunterHold.lookWeight;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            hunterHold.lookWeight = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        hunterHold.lookWeight = to;
    }

    private Vector3 HunterHead()
    {
        Transform head = hunter.GetBoneTransform(HumanBodyBones.Head);
        return head != null ? head.position : hunter.transform.position + Vector3.up * 1.7f;
    }

    private IEnumerator ThrowGun(float duration)
    {
        heldGun.SetParent(null, true);
        Vector3 from = heldGun.position;
        Quaternion spinFrom = heldGun.rotation;
        Vector3 axis = cam.transform.right;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            heldGun.position = Vector3.Lerp(from, HunterHead(), k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.15f;
            heldGun.rotation = Quaternion.AngleAxis(k * 900f, axis) * spinFrom;
            yield return null;
        }
        StartCoroutine(DropGun());
    }

    // After the hit the gun falls to the lobby floor just outside the door line, never inside the lift.
    private IEnumerator DropGun()
    {
        Vector3 from = heldGun.position;
        Vector3 to = new Vector3(Mathf.Lerp(from.x, hunterStop.position.x, 0.5f), 0.05f, doorLeft.position.z - 0.35f);
        Quaternion rotFrom = heldGun.rotation;
        Quaternion rotTo = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
        for (float t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.45f;
            heldGun.position = Vector3.Lerp(from, to, k * k);
            heldGun.rotation = Quaternion.Slerp(rotFrom, rotTo, k);
            yield return null;
        }
        heldGun.SetPositionAndRotation(to, rotTo);
    }

    private IEnumerator CloseDoors(float target, float duration)
    {
        float from = doorsClosed;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            SetDoors(Mathf.Lerp(from, target, t / duration));
            yield return null;
        }
        SetDoors(target);
    }

    // 0 = fully open, 1 = fully shut.
    private void SetDoors(float closed)
    {
        doorsClosed = Mathf.Clamp01(closed);
        Vector3 l = doorLeft.localPosition, r = doorRight.localPosition;
        l.x = Mathf.Lerp(leftOpenX, leftClosedX, doorsClosed);
        r.x = Mathf.Lerp(rightOpenX, rightClosedX, doorsClosed);
        doorLeft.localPosition = l;
        doorRight.localPosition = r;
    }

    private void Flicker()
    {
        for (int i = 0; i < flickerLights.Length; i++)
        {
            float level = Random.value < 0.08f ? Random.Range(0.05f, 0.3f) : 1f - Random.value * 0.15f;
            flickerLights[i].intensity = lightBase[i] * lightDim * level;
        }
    }

    private void Shake(float duration, float strength)
    {
        shakeUntil = Time.unscaledTime + duration;
        shakeStrength = strength / duration;
    }

    private IEnumerator MoveCamera(Vector3 pos, Quaternion rot, float duration)
    {
        Vector3 p0 = camBasePos;
        Quaternion r0 = camBaseRot;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            camBasePos = Vector3.Lerp(p0, pos, k);
            camBaseRot = Quaternion.Slerp(r0, rot, k);
            yield return null;
        }
        camBasePos = pos;
        camBaseRot = rot;
    }

    private IEnumerator LookAt(Vector3 point, float duration)
    {
        yield return MoveCamera(camBasePos, Quaternion.LookRotation(point - camBasePos), duration);
    }

    private IEnumerator MoveGun(Vector3 fromPos, Quaternion fromRot, Vector3 toPos, Quaternion toRot, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            heldGun.localPosition = Vector3.Lerp(fromPos, toPos, k);
            heldGun.localRotation = Quaternion.Slerp(fromRot, toRot, k);
            yield return null;
        }
        heldGun.localPosition = toPos;
        heldGun.localRotation = toRot;
    }

    private static IEnumerator FadeTo(CanvasGroup group, float to, float duration)
    {
        float from = group.alpha;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }

    private static IEnumerator Wait(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
    }

    private static IEnumerator WaitUntilTime(float time)
    {
        while (Time.unscaledTime < time) yield return null;
    }

    // Plays a sound from a point in the scene through a short-lived 3D source, optionally starting part-way into the sound.
    private static void PlayAt(AudioClip clip, Vector3 position, float volume, float startFrom = 0f)
    {
        if (clip == null) return;
        var go = new GameObject("CutsceneSound");
        go.transform.position = position;
        var s = NewSource(go, 0.8f);
        s.clip = clip;
        s.volume = volume;
        s.time = Mathf.Clamp(startFrom, 0f, clip.length - 0.01f);
        s.Play();
        Destroy(go, clip.length - s.time + 0.1f);
    }

    private static AudioSource NewSource(GameObject host, float spatial)
    {
        var s = host.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = spatial;
        s.minDistance = 2f;
        s.maxDistance = 25f;
        s.rolloffMode = AudioRolloffMode.Linear;
        return s;
    }
}
