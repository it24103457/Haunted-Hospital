using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/*
 * The escape cutscene, played in its own scene after the key is turned in the lift. Part one is the escape into the lift (below);
 * part two, the ride up to the roof and the helicopter, is described above RoofPart.
 * Fades in facing the key switch, then turns to look back out of the open lift doors as the dimmed lights start to flicker.
 * The doors start sliding shut with the lift sound (its silent start skipped) and jam partway with a slam when that sound is cut;
 * slow echoing footsteps follow
 * from the corridor side as the Hunter walks slowly along the lobby and in through the doors without ever stopping,
 * turning its head to the camera and growling as it comes in.
 * Meanwhile the held syringe gun comes up and is fired, but only clicks empty; the player looks down at it, shakes it,
 * catches a panicked breath, looks back up, winds the gun back and throws it. The walk is timed so the gun hits the Hunter's head
 * just as it steps inside the lift; it flinches straight back out past the door line, the gun drops to the lobby floor outside,
 * and the doors slam shut. Part two follows; without the roof set, the screen fades to black and the end panel
 * ("YOU ESCAPED", Play Again, Main Menu) fades in.
 * Space (and only Space, so Esc or a stray click never cuts it short) skips straight to the end panel; a small "SPACE Skip" hint shows for a few seconds at the start.
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

    [Header("Part two: roof")]
    [Tooltip("The roof copy of the lift's door panels. Left empty, the cutscene ends after part one.")]
    [SerializeField] private Transform roofDoorLeft;
    [SerializeField] private Transform roofDoorRight;
    [Tooltip("Lights in the roof copy of the lift; they flicker with the lift's own lights.")]
    [SerializeField] private Light[] roofLiftLights;
    [Tooltip("Lights outside the lift (lobby, corridor and the rest of the floor). They are switched off once the doors are shut, "
             + "so the lift is lit only by its own lights, exactly like its copy on the roof.")]
    [SerializeField] private Light[] outsideLights;
    [Tooltip("The roof's lights (moonlight, floodlights, door lamp). Off until the roof doors open, so none of them reach the lift copy during the ride.")]
    [SerializeField] private GameObject roofLights;
    [Tooltip("The helicopter's root; its pivot is under the skids and its forward is the nose.")]
    [SerializeField] private Transform heli;
    [SerializeField] private HeliRotors heliRotors;
    [Tooltip("Standing just outside the helicopter's open side door, facing in.")]
    [SerializeField] private Transform heliEntry;
    [Tooltip("Seated in the helicopter, looking out through the windscreen.")]
    [SerializeField] private Transform heliSeat;
    [Tooltip("First shot of the lift-off: low on the roof, behind the helicopter.")]
    [SerializeField] private Transform shotLiftOff;
    [Tooltip("Second shot of the lift-off: higher, from a back corner of the roof.")]
    [SerializeField] private Transform shotClimb;
    [Tooltip("The far shot: a camera a long way off, facing the moon.")]
    [SerializeField] private Transform shotMoon;
    [Tooltip("Where the helicopter flies from and to across the moon in the far shot.")]
    [SerializeField] private Transform moonPathFrom;
    [SerializeField] private Transform moonPathTo;
    [Tooltip("How long the lift ride lasts, in seconds.")]
    [SerializeField, Min(1f)] private float rideTime = 7f;
    [Tooltip("How bright the lift lights are during the ride, compared to their dimmed flicker level.")]
    [SerializeField, Range(0f, 1f)] private float rideLight = 0.45f;
    [Tooltip("Walking speed out to the helicopter, in metres per second.")]
    [SerializeField, Min(0.1f)] private float playerWalkSpeed = 1.5f;
    [Tooltip("How long the helicopter takes to start up (rotors spinning up, engine sound fading in) before it lifts off, in seconds.")]
    [SerializeField, Min(0.5f)] private float spinUpTime = 4f;
    [SerializeField, Range(10f, 60f)] private float shotFov = 50f;
    [SerializeField, Range(10f, 60f)] private float moonShotFov = 30f;
    [Tooltip("How long the far shot lasts, in seconds; it fades to black over its last few seconds.")]
    [SerializeField, Min(1f)] private float moonShotTime = 7f;

    [Header("Part two: sounds")]
    [SerializeField] private AudioClip liftRide;
    [Tooltip("Where in the ride sound playback starts, in seconds.")]
    [SerializeField, Min(0f)] private float liftRideFrom = 3f;
    [SerializeField] private AudioClip liftDing;
    [Tooltip("The part of the ding sound that is played, in seconds.")]
    [SerializeField, Min(0f)] private float liftDingFrom = 2f;
    [SerializeField, Min(0f)] private float liftDingTo = 3f;
    [Tooltip("Where in the lift sound playback starts when the roof doors open, in seconds.")]
    [SerializeField, Min(0f)] private float roofDoorsFrom = 3f;
    [SerializeField] private AudioClip heliSound;

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
    private float[] roofLightBase;
    private float lightLevel = 1f;
    private float rumble;
    private AudioSource liftSource;
    private AudioSource rideSource;
    private AudioSource footstepSource;
    private AudioSource hunterVoice;
    private AudioSource heliSource;
    private AudioSource playerSteps;

    private bool HasRoof => roofDoorLeft != null && roofDoorRight != null && heli != null;

    // Takes the doors off their Animator and remembers each light's normal brightness, so the sequence can drive them directly.
    private void Awake()
    {
        if (doorAnimator != null) doorAnimator.enabled = false;
        lightBase = new float[flickerLights.Length];
        for (int i = 0; i < flickerLights.Length; i++) lightBase[i] = flickerLights[i].intensity;
        roofLightBase = new float[roofLiftLights.Length];
        for (int i = 0; i < roofLiftLights.Length; i++) roofLightBase[i] = roofLiftLights[i].intensity;

        liftSource = NewSource(gameObject, 0f);
        rideSource = NewSource(gameObject, 0f);
        playerSteps = NewSource(gameObject, 0f);
        footstepSource = NewSource(hunter.gameObject, 1f);
        hunterVoice = NewSource(hunter.gameObject, 1f);
        if (HasRoof)
        {
            // The helicopter is heard from where it is, but never drops out completely in the far shots.
            heliSource = NewSource(heli.gameObject, 0.6f);
            heliSource.clip = heliSound;
            heliSource.loop = true;
            heliSource.minDistance = 6f;
            heliSource.maxDistance = 150f;
            // The helicopter waits switched off until the player is in it.
            if (heliRotors != null) heliRotors.speed = 0f;
            SetRoofDoors(1f);
        }
        if (roofLights != null) roofLights.SetActive(false);

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

    // Starts the sequence and the skip hint together.
    private void Start()
    {
        StartCoroutine(Run());
        StartCoroutine(ShowSkipHint());
    }

    // Runs the light flicker and keeps the Hunter looking at the camera while asked to; Space skips to the end.
    private void Update()
    {
        if (flickering) Flicker();
        if (hunterLooking) hunterHold.lookAt = cam.transform.position;
        if (skipping) return;

        bool skip = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
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

        // A steady low rumble while the lift is moving.
        if (rumble > 0f)
        {
            float t = Time.unscaledTime * 9f;
            offset += new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * rumble * 0.012f;
        }
        cam.transform.SetPositionAndRotation(camBasePos + camBaseRot * offset, camBaseRot);
    }

    // The whole escape, start to finish: the lift ride and the Hunter's attack, then the roof and the helicopter, ending on the win screen.
    private IEnumerator Run()
    {
        yield return PrewarmRoof();

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

        if (HasRoof)
        {
            yield return RoofPart();
            yield break;
        }
        yield return Wait(1.0f);
        yield return ShowEnd();
    }

    /*
     * Part two. The lift rides up in low, flickering light with a rumble. Halfway up, the camera is moved across to the copy of the lift
     * built on the roof (the same room, straight above), so nothing in view changes. The lift stops with a jolt, the lights steady,
     * it dings, and the doors open onto the roof with the helicopter waiting on the pad, switched off.
     * The player walks out to its open side door, climbs in and looks out through the windscreen. Then it cuts to a shot from behind
     * as the helicopter starts up and lifts off, a second shot from another angle as it climbs away, and a far shot of it flying
     * across the moon, which fades to black before "YOU ESCAPED".
     */
    private IEnumerator RoofPart()
    {
        yield return Wait(0.6f);

        // The ride: lights low, the freight lift sound from its set start, a steady rumble.
        // With the doors shut, only the lift's own lights stay on, so it looks the same as its roof copy.
        foreach (Light l in outsideLights) l.enabled = false;
        lightLevel = rideLight;
        PlayFrom(rideSource, liftRide, liftRideFrom, 1f);
        StartCoroutine(BlendRumble(1f, 1f));
        yield return Wait(rideTime * 0.5f);
        camBasePos += roofDoorLeft.parent.position - doorLeft.parent.position;
        yield return Wait(rideTime * 0.5f);

        // Arrival: the ride sound dies away, a jolt, the lights steady, then the ding.
        StartCoroutine(FadeOut(rideSource, 0.4f));
        StartCoroutine(BlendRumble(0f, 0.3f));
        Shake(0.3f, 0.7f);
        flickering = false;
        SetLiftLights(lightDim);
        yield return Wait(0.5f);
        PlayFrom(liftSource, liftDing, liftDingFrom, 0.9f);
        StartCoroutine(StopSourceAfter(liftSource, liftDingTo - liftDingFrom));
        yield return Wait(liftDingTo - liftDingFrom + 0.3f);

        // The doors open onto the roof, lit by the moon and floodlights; the helicopter sits there switched off.
        if (roofLights != null) roofLights.SetActive(true);
        PlayFrom(liftSource, liftStart, roofDoorsFrom, 1f);
        yield return MoveRoofDoors(0f, 1.8f);
        // The door sound dies away as soon as the doors are fully open.
        StartCoroutine(FadeOut(liftSource, 0.6f));
        yield return Wait(0.4f);

        // Walk out through the doors to the helicopter's side door, looking at it.
        // The player's own footsteps use the slow steps sound, sped up slightly to match the faster walk.
        Vector3 doorway = new Vector3(roofDoorLeft.parent.position.x, camBasePos.y, roofDoorLeft.position.z - 0.6f);
        playerSteps.clip = footsteps;
        playerSteps.loop = true;
        playerSteps.volume = 0.45f;
        playerSteps.pitch = 1.1f;
        playerSteps.Play();
        yield return Walk(new[] { doorway, heliEntry.position }, HeliCentre());
        StartCoroutine(FadeOut(playerSteps, 0.3f));

        // Climb in and sit, then look out through the windscreen.
        Vector3 inward = heliSeat.position - heliEntry.position;
        inward.y = 0f;
        yield return MoveCamera(heliSeat.position, Quaternion.LookRotation(inward), 1.0f);
        yield return MoveCamera(heliSeat.position, heliSeat.rotation, 1.0f);
        yield return Wait(1.6f);

        // Seen only from behind: it starts up, the rotors spinning up as its sound fades in, then lifts off from the same angle,
        // and a second shot switches angle as it climbs away.
        float fovBefore = cam.fieldOfView;
        cam.fieldOfView = shotFov;
        StartCoroutine(HeliStartUp(spinUpTime));
        yield return TrackShot(shotLiftOff, spinUpTime);
        Coroutine takeOff = StartCoroutine(HeliTakeOff());
        yield return TrackShot(shotLiftOff, 3.2f);
        yield return TrackShot(shotClimb, 4.2f);
        StopCoroutine(takeOff);

        // The far shot: the helicopter flies across the moon, then everything fades to black.
        yield return MoonShot();
        cam.fieldOfView = fovBefore;
        yield return ShowEnd(0f, 2.5f);
    }

    /*
     * While the screen is still black at the start, switches the roof lights on and points the camera at the part-two views
     * (inside the roof lift, the helicopter seat, the lift-off shots) for a couple of frames each, then puts everything back.
     * In the Editor, a material lit in a way it hasn't been drawn before shows flat cyan for a moment while its shader is
     * prepared; this gets that done out of sight, long before the roof doors open. A built game prepares shaders in advance anyway.
     */
    private IEnumerator PrewarmRoof()
    {
        if (!HasRoof) yield break;

        if (roofLights != null) roofLights.SetActive(true);
        Vector3 roofOffset = roofDoorLeft.parent.position - doorLeft.parent.position;
        var views = new List<(Vector3 pos, Quaternion rot)>
        {
            (camLookBack.position + roofOffset, camLookBack.rotation),
            (camLookBack.position + roofOffset, Quaternion.LookRotation(HeliCentre() - (camLookBack.position + roofOffset)))
        };
        foreach (Transform marker in new[] { heliSeat, shotLiftOff, shotClimb })
            if (marker != null) views.Add((marker.position, Quaternion.LookRotation(HeliCentre() - marker.position)));
        if (heliSeat != null) views.Add((heliSeat.position, heliSeat.rotation));

        // The camera is placed from camBasePos/camBaseRot every frame, so the views go through those and are put back after.
        Vector3 startPos = camBasePos;
        Quaternion startRot = camBaseRot;
        foreach (var view in views)
        {
            camBasePos = view.pos;
            camBaseRot = view.rot;
            yield return null;
            yield return null;
        }

        if (roofLights != null) roofLights.SetActive(false);
        camBasePos = startPos;
        camBaseRot = startRot;
        yield return null;
    }

    // The middle of the helicopter's body, which the shots keep in frame.
    private Vector3 HeliCentre()
    {
        return heli.position + heli.up * 1.2f - heli.forward * 1.3f;
    }

    // Walks the camera along a path at walking speed with a slight head bob, turning to face a point as it goes.
    private IEnumerator Walk(Vector3[] points, Vector3 lookAt)
    {
        float travelled = 0f;
        foreach (Vector3 target in points)
        {
            Vector3 from = camBasePos;
            float length = Vector3.Distance(from, target);
            for (float d = 0f; d < length; d += playerWalkSpeed * Time.unscaledDeltaTime)
            {
                Vector3 pos = Vector3.Lerp(from, target, d / length);
                float bob = Mathf.Sin((travelled + d) * Mathf.PI * 2f / 1.4f) * 0.025f;
                camBasePos = pos + Vector3.up * bob;
                camBaseRot = Quaternion.Slerp(camBaseRot, Quaternion.LookRotation(lookAt - pos), Time.unscaledDeltaTime * 3f);
                yield return null;
            }
            travelled += length;
            camBasePos = target;
        }
    }

    // Starts the helicopter: the rotors spin up slowly at first, and the engine sound fades in, rising in pitch as it gets going.
    private IEnumerator HeliStartUp(float duration)
    {
        heliSource.volume = 0f;
        heliSource.pitch = 0.6f;
        heliSource.Play();
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            if (heliRotors != null) heliRotors.speed = k * k;
            heliSource.volume = k;
            heliSource.pitch = Mathf.Lerp(0.6f, 1f, k);
            yield return null;
        }
        if (heliRotors != null) heliRotors.speed = 1f;
        heliSource.volume = 1f;
        heliSource.pitch = 1f;
    }

    /*
     * Lifts the helicopter off the pad: the rotors spin up, it rises slowly with a slight sway, then tips its nose down
     * and climbs away forwards, speeding up. Runs until the shots that watch it are over.
     */
    private IEnumerator HeliTakeOff()
    {
        Vector3 p0 = heli.position;
        Quaternion r0 = heli.rotation;
        for (float t = 0f; ; t += Time.unscaledDeltaTime)
        {
            if (heliRotors != null) heliRotors.speed = Mathf.Lerp(1f, 1.5f, t / 0.8f);
            heliSource.pitch = 1f + 0.12f * Mathf.Clamp01(t / 3f);
            float rise = t < 0.6f ? 0f : 3f * Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 2.4f) + Mathf.Max(0f, t - 3f) * Mathf.Max(0f, t - 3f) * 0.9f;
            float forward = t < 2.6f ? 0f : (t - 2.6f) * (t - 2.6f) * 1.4f;
            float pitch = 10f * Mathf.SmoothStep(0f, 1f, (t - 2.6f) / 1.5f);
            float sway = rise > 0f ? 1f : 0f;
            heli.position = p0 + Vector3.up * (rise + Mathf.Sin(t * 1.7f) * 0.06f * sway) + (r0 * Vector3.forward) * forward;
            heli.rotation = r0 * Quaternion.Euler(pitch, 0f, Mathf.Sin(t * 1.3f) * 1.5f * sway);
            yield return null;
        }
    }

    // Holds the camera at a marker, turning smoothly to keep the helicopter in frame.
    private IEnumerator TrackShot(Transform marker, float duration)
    {
        camBasePos = marker.position;
        camBaseRot = Quaternion.LookRotation(HeliCentre() - camBasePos);
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            camBaseRot = Quaternion.Slerp(camBaseRot, Quaternion.LookRotation(HeliCentre() - camBasePos), Time.unscaledDeltaTime * 4f);
            yield return null;
        }
    }

    // A fixed camera far off, facing the moon; the helicopter crosses in front of it, and the screen fades to black at the end.
    private IEnumerator MoonShot()
    {
        cam.fieldOfView = moonShotFov;
        camBasePos = shotMoon.position;
        camBaseRot = shotMoon.rotation;
        Vector3 from = moonPathFrom.position, to = moonPathTo.position;
        heli.rotation = Quaternion.LookRotation(to - from) * Quaternion.Euler(8f, 0f, 0f);
        if (heliRotors != null) heliRotors.speed = 1.5f;
        heliSource.volume = 0.6f;
        const float fadeTime = 2.5f;
        bool fading = false;
        for (float t = 0f; t < moonShotTime; t += Time.unscaledDeltaTime)
        {
            heli.position = Vector3.Lerp(from, to, t / moonShotTime);
            if (!fading && t >= moonShotTime - fadeTime)
            {
                fading = true;
                StartCoroutine(FadeTo(fade, 1f, fadeTime));
                StartCoroutine(FadeOut(heliSource, fadeTime));
            }
            yield return null;
        }
        fade.alpha = 1f;
    }

    // Slides the roof doors smoothly to the given closed amount (0 open, 1 shut) over the duration.
    private IEnumerator MoveRoofDoors(float closed, float duration)
    {
        float from = roofDoorsClosed;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            SetRoofDoors(Mathf.Lerp(from, closed, Mathf.SmoothStep(0f, 1f, t / duration)));
            yield return null;
        }
        SetRoofDoors(closed);
    }

    private float roofDoorsClosed;

    // Same as SetDoors, for the roof copy of the lift doors.
    private void SetRoofDoors(float closed)
    {
        roofDoorsClosed = Mathf.Clamp01(closed);
        Vector3 l = roofDoorLeft.localPosition, r = roofDoorRight.localPosition;
        l.x = Mathf.Lerp(leftOpenX, leftClosedX, roofDoorsClosed);
        r.x = Mathf.Lerp(rightOpenX, rightClosedX, roofDoorsClosed);
        roofDoorLeft.localPosition = l;
        roofDoorRight.localPosition = r;
    }

    // Sets both lifts' lights to a steady level, as a fraction of their normal brightness.
    private void SetLiftLights(float level)
    {
        for (int i = 0; i < flickerLights.Length; i++) flickerLights[i].intensity = lightBase[i] * level;
        for (int i = 0; i < roofLiftLights.Length; i++) roofLiftLights[i].intensity = roofLightBase[i] * level;
    }

    // Eases the lift's camera rumble to the given strength over the duration.
    private IEnumerator BlendRumble(float to, float duration)
    {
        float from = rumble;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            rumble = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        rumble = to;
    }

    // Plays a clip once on the given source from a point part-way in, replacing whatever it was playing.
    private static void PlayFrom(AudioSource source, AudioClip clip, float startFrom, float volume)
    {
        if (clip == null) return;
        source.Stop();
        source.clip = clip;
        source.loop = false;
        source.volume = volume;
        source.time = Mathf.Clamp(startFrom, 0f, clip.length - 0.01f);
        source.Play();
    }

    // Stops the source after the given time, unless it has moved on to another clip by then.
    private static IEnumerator StopSourceAfter(AudioSource source, float seconds)
    {
        AudioClip clip = source.clip;
        yield return Wait(seconds);
        if (source.clip == clip) source.Stop();
    }

    // Eases the source's volume to the given level over the duration.
    private static IEnumerator FadeVolume(AudioSource source, float to, float duration)
    {
        float from = source.volume;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        source.volume = to;
    }

    // Fades the source to silence and stops it, unless it has moved on to another clip by then.
    private static IEnumerator FadeOut(AudioSource source, float duration)
    {
        AudioClip clip = source.clip;
        yield return FadeVolume(source, 0f, duration);
        if (source.clip == clip) source.Stop();
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

    // Plays the doors slamming at the given moment, from where the Hunter stands.
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

    // At the given moment stops the lift sound and, unless the Hunter's hit already landed, jams the doors with a clunk and a shake.
    private IEnumerator StopLiftSoundAt(float time)
    {
        yield return WaitUntilTime(time);
        liftSource.Stop();
        if (hitLanded) yield break;
        PlayAt(doorsJam, new Vector3(hunterStop.position.x, 1.2f, doorLeft.position.z), 1f);
        Shake(0.35f, 0.8f);
    }

    // Skips the rest: silences everything, fades to black, and jumps to the final roof shot before fading back in.
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
    private IEnumerator ShowEnd(float fadeTime = 0.8f, float panelTime = 1.2f)
    {
        yield return FadeTo(fade, 1f, fadeTime);
        flickering = false;
        foreach (var s in FindObjectsByType<AudioSource>()) s.Stop();
        Cursor.visible = true;
        endPanel.gameObject.SetActive(true);
        endPanel.alpha = 0f;
        endPanel.interactable = false;
        endPanel.blocksRaycasts = false;
        UIButtonSounds.PlayClip(winSound, 0.9f);
        yield return FadeTo(endPanel, 1f, panelTime);
        endPanel.interactable = true;
        endPanel.blocksRaycasts = true;
    }

    /*
     * Shows the "SPACE Skip" hint once the opening fade from black has finished, so it comes in on a settled picture,
     * then fades it out again after a few seconds.
     */
    private IEnumerator ShowSkipHint()
    {
        while (fade.alpha > 0.01f) yield return null;
        yield return Wait(0.3f);
        skipHint.gameObject.SetActive(true);
        skipHint.alpha = 0f;
        yield return FadeHint(1f, 0.5f);
        yield return Wait(4f);
        yield return FadeHint(0f, 0.5f);
        skipHint.gameObject.SetActive(false);
    }

    // Eased fade for the skip hint. Each frame moves it on by at most 1/30 s, so a slow frame (common just after this heavy scene loads) can't make it jump.
    private IEnumerator FadeHint(float to, float duration)
    {
        float from = skipHint.alpha;
        for (float t = 0f; t < duration; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
        {
            skipHint.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        skipHint.alpha = to;
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

    // Eases how strongly the Hunter turns its head towards the camera.
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

    // The Hunter's head position, or a point at head height above it when the model has no head bone.
    private Vector3 HunterHead()
    {
        Transform head = hunter.GetBoneTransform(HumanBodyBones.Head);
        return head != null ? head.position : hunter.transform.position + Vector3.up * 1.7f;
    }

    // Throws the held gun at the Hunter's head in a spinning arc, then lets it drop to the floor.
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

    // Moves the lift doors to the given closed amount over the duration.
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

    // Sets each flickering light for this frame: mostly near full, sometimes dropping almost to dark.
    private void Flicker()
    {
        for (int i = 0; i < flickerLights.Length; i++)
        {
            float level = Random.value < 0.08f ? Random.Range(0.05f, 0.3f) : 1f - Random.value * 0.15f;
            flickerLights[i].intensity = lightBase[i] * lightDim * lightLevel * level;
        }
        for (int i = 0; i < roofLiftLights.Length; i++)
        {
            float level = Random.value < 0.08f ? Random.Range(0.05f, 0.3f) : 1f - Random.value * 0.15f;
            roofLiftLights[i].intensity = roofLightBase[i] * lightDim * lightLevel * level;
        }
    }

    // Starts a camera shake that fades out over the duration; LateUpdate applies it.
    private void Shake(float duration, float strength)
    {
        shakeUntil = Time.unscaledTime + duration;
        shakeStrength = strength / duration;
    }

    // Moves and turns the camera smoothly to the given pose over the duration.
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

    // Turns the camera, without moving it, to look at the given point over the duration.
    private IEnumerator LookAt(Vector3 point, float duration)
    {
        yield return MoveCamera(camBasePos, Quaternion.LookRotation(point - camBasePos), duration);
    }

    // Moves the held gun smoothly between two poses in the camera's hands over the duration.
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

    // Fades a screen overlay to the given opacity over the duration.
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

    // Waits the given time on real time, so it is unaffected by the game clock.
    private static IEnumerator Wait(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
    }

    // Waits until the given moment on the real-time clock.
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

    // Adds an audio source to the given object: flat (0) or placed in the scene (1), with a short audible range.
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
