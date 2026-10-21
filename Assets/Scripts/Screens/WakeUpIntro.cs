using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;

/*
 * The wake-up intro played after Play is pressed in the main menu, before the game scene loads.
 * The whole screen fades to black; behind the black the menu is hidden and the camera is laid on the floor beside the syringe gun.
 * Two eyelids (black bands from the top and bottom) then open in fits and starts, following the Eye Openness curve,
 * while a blur (a Depth of Field volume) eases off along the Blur curve, the camera bobs with heavy breathing that slowly calms,
 * and the panting sound loops in. Once the eyes are open the player gets up, looks right and then left, then turns
 * rightwards all the way round until the start door comes into view, pauses on it briefly and runs straight to it,
 * a footstep landing with each dip of the head. They yank the door twice
 * (camera jerks timed to the two loud spikes of the door handle sound, since there are no hands to show), but it is locked.
 * They then slowly turn down and to the right to the syringe gun on the floor, the camera zooms straight in on it (along the
 * line of sight, only tilting down, never turning sideways) and holds a moment,
 * and the screen fades to black, as if the gun has been picked up, before the game scene loads.
 * Space (and only Space, so Esc or a stray click never cuts it short) skips straight to that last fade. A small "SPACE Skip" hint shows in the corner for a few seconds
 * as the eyes start to open. Once the screen is black, "Loading" with cycling dots shows in the centre while the game scene
 * loads in the background. All timing runs on real time.
 * Every camera position (lying, standing, looking back at the door, at the door, spotting the gun, close on the gun) is a marker
 * object in the scene, so they can be moved in the Scene view.
 */
public class WakeUpIntro : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private Camera menuCamera;
    [SerializeField] private Transform lyingPoint;
    [Tooltip("Standing where the player woke, looking ahead down the corridor; the glance left and right turns from here.")]
    [SerializeField] private Transform standPoint;
    [Tooltip("Standing where the player woke, looking back at the start door.")]
    [SerializeField] private Transform doorLookPoint;
    [Tooltip("Right at the start door, facing its handles.")]
    [SerializeField] private Transform doorPoint;
    [Tooltip("Standing at the door, looking down and to the right at the syringe gun on the floor.")]
    [SerializeField] private Transform gunSpotPoint;
    [Tooltip("Close up on the syringe gun: where the camera zooms in to.")]
    [SerializeField] private Transform gunLookPoint;
    [SerializeField] private Volume blurVolume;
    [SerializeField] private AudioSource panting;

    [Header("Screen")]
    [Tooltip("Menu objects hidden once the screen is black (e.g. the menu text and the left shade).")]
    [SerializeField] private GameObject[] menuPanels;
    [SerializeField] private CanvasGroup fade;
    [SerializeField] private RectTransform eyelidTop;
    [SerializeField] private RectTransform eyelidBottom;
    [Tooltip("Height of each eyelid when fully closed, in canvas units (the canvas is 1080 tall); a little over half so the soft edges overlap.")]
    [SerializeField] private float closedEyelidHeight = 660f;
    [SerializeField] private CanvasGroup skipHint;
    [SerializeField] private TMPro.TMP_Text loadingText;

    [Header("Skip hint and loading (seconds)")]
    [SerializeField, Min(0f)] private float skipHintShown = 4f;
    [SerializeField, Min(0.01f)] private float skipHintFade = 0.4f;
    [SerializeField, Min(0.05f)] private float loadingDotInterval = 0.4f;
    [Tooltip("Shortest time Loading stays on screen, so a fast load does not just flash it.")]
    [SerializeField, Min(0f)] private float minimumLoadingTime = 1f;

    [Header("Timing (seconds)")]
    [SerializeField, Min(0.01f)] private float fadeOut = 1.5f;
    [SerializeField, Min(0f)] private float blackHold = 1f;
    [Tooltip("How open the eyes are (0 closed, 1 open) over time. The last key sets how long the opening takes.")]
    [SerializeField] private AnimationCurve eyeOpenness = DefaultEyes();
    [Tooltip("How strong the blur is (1 full, 0 sharp) over the same time.")]
    [SerializeField] private AnimationCurve blur = AnimationCurve.EaseInOut(0f, 1f, 5.5f, 0f);
    [SerializeField, Min(0f)] private float openHold = 1.2f;
    [Tooltip("Getting up from the floor.")]
    [SerializeField, Min(0.01f)] private float getUp = 1.2f;
    [Tooltip("The look to the right, then to the left.")]
    [SerializeField, Min(0.01f)] private float lookRight = 0.9f;
    [SerializeField, Min(0.01f)] private float lookLeft = 1.2f;
    [Tooltip("How far the glance turns to each side, in degrees.")]
    [SerializeField, Range(0f, 90f)] private float lookAroundAngle = 40f;
    [Tooltip("The turn rightwards all the way round, after the look to the left, that comes to rest on the start door.")]
    [SerializeField, Min(0.01f)] private float turnAround = 1.8f;
    [Tooltip("The short pause on seeing the door, before running to it.")]
    [SerializeField, Min(0f)] private float doorSpotted = 0.5f;
    [Tooltip("The panicked run up to the door.")]
    [SerializeField, Min(0.01f)] private float runToDoor = 0.9f;
    [Tooltip("Rattling the locked door.")]
    [SerializeField, Min(0.01f)] private float tryDoor = 1.4f;
    [SerializeField, Min(0f)] private float afterDoor = 0.4f;
    [Tooltip("Turning down and to the right to the syringe gun on the floor.")]
    [SerializeField, Min(0.01f)] private float turnToGun = 1.2f;
    [Tooltip("Zooming in on the syringe gun.")]
    [SerializeField, Min(0.01f)] private float zoomToGun = 1.2f;
    [SerializeField, Min(0f)] private float gunHold = 1f;
    [SerializeField, Min(0.01f)] private float finalFade = 1.5f;

    [Header("Door")]
    [SerializeField] private AudioClip doorHandle;
    [SerializeField, Range(0f, 1f)] private float doorHandleVolume = 1f;
    [Tooltip("Seconds skipped at the start of the door handle sound.")]
    [SerializeField, Min(0f)] private float doorHandleFrom = 0f;
    [Tooltip("How hard the camera shakes while the door is rattled, in metres.")]
    [SerializeField, Min(0f)] private float doorShake = 0.03f;
    [Tooltip("When each of the two tries at the door happens, in seconds into the door handle sound (its two loud spikes).")]
    [SerializeField] private float firstTry = 0.32f;
    [SerializeField] private float secondTry = 1.0f;
    [Tooltip("How hard the first try is compared to the second, which is the harder one.")]
    [SerializeField, Range(0f, 1f)] private float firstTryStrength = 0.65f;

    [Header("Footsteps")]
    [Tooltip("A recording of footsteps; single steps are cut from it, one for each step of the run.")]
    [SerializeField] private AudioClip footsteps;
    [Tooltip("Where single steps start in the footsteps recording, in seconds; the run uses them in turn.")]
    [SerializeField] private float[] stepStarts = { 0.91f, 1.47f, 2.60f, 3.14f, 4.22f, 4.76f };
    [Tooltip("How much of the recording each step plays, in seconds.")]
    [SerializeField, Min(0.05f)] private float stepLength = 0.35f;
    [SerializeField, Range(0f, 1f)] private float stepVolume = 0.8f;
    [Tooltip("Seconds between steps while running.")]
    [SerializeField, Min(0.1f)] private float stepInterval = 0.3f;
    [Tooltip("How far the head dips at each step of the run, in metres.")]
    [SerializeField, Min(0f)] private float stepBob = 0.05f;

    [Header("Breathing")]
    [SerializeField] private float breathsPerSecondStart = 1.1f;
    [SerializeField] private float breathsPerSecondEnd = 0.45f;
    [SerializeField] private float bobStart = 0.035f;
    [SerializeField] private float bobEnd = 0.012f;
    [SerializeField] private float swayDegrees = 1.2f;
    [SerializeField, Range(0f, 1f)] private float pantingVolume = 0.8f;

    private string scenePath;
    private bool skipping;
    private bool started;
    private bool onFloor;
    private float breathPhase;
    private float breathTime;
    private Vector3 basePosition;
    private Quaternion baseRotation;

    public bool IsRunning => started;

    // Starts with the fade, eyelids, skip hint and loading text hidden, so the menu looks normal until Play is pressed.
    private void Awake()
    {
        fade.gameObject.SetActive(false);
        eyelidTop.gameObject.SetActive(false);
        eyelidBottom.gameObject.SetActive(false);
        if (skipHint != null) skipHint.gameObject.SetActive(false);
        if (loadingText != null) loadingText.gameObject.SetActive(false);
        if (blurVolume != null) blurVolume.weight = 0f;
    }

    // Starts the intro; the given scene is loaded at the end.
    public void Begin(string gameScenePath)
    {
        if (started) return;
        started = true;
        scenePath = gameScenePath;

        // The menu's camera drift stops here, so from the moment Play is pressed only the intro moves the camera.
        var drift = menuCamera.GetComponent<MenuCameraDrift>();
        if (drift != null) drift.enabled = false;
        StartCoroutine(Run());
    }

    // While the intro runs, Space skips straight to loading the game.
    private void Update()
    {
        if (!started || skipping) return;

        bool skip = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        if (skip)
        {
            skipping = true;
            StopAllCoroutines();
            StartCoroutine(FinishAndLoad());
        }
    }

    // The whole intro: fade from the menu, wake up, look around, run to the locked door and rattle it, find the gun, then load the game.
    private IEnumerator Run()
    {
        Cursor.visible = false;

        fade.gameObject.SetActive(true);
        fade.blocksRaycasts = true;
        yield return FadeGroup(fade, 0f, 1f, fadeOut);

        // Everything below happens behind the black.
        foreach (var p in menuPanels) if (p != null) p.SetActive(false);
        menuCamera.transform.SetPositionAndRotation(lyingPoint.position, lyingPoint.rotation);
        basePosition = lyingPoint.position;
        baseRotation = lyingPoint.rotation;
        onFloor = true;
        SetEyes(0f);
        eyelidTop.gameObject.SetActive(true);
        eyelidBottom.gameObject.SetActive(true);
        if (blurVolume != null) blurVolume.weight = blur.Evaluate(0f);
        if (panting != null)
        {
            panting.volume = 0f;
            panting.Play();
        }

        yield return Prewarm();
        yield return new WaitForSecondsRealtime(blackHold);
        fade.alpha = 0f;
        StartCoroutine(ShowSkipHint());

        float openTime = eyeOpenness.keys[eyeOpenness.length - 1].time;
        for (float t = 0f; t < openTime; t += Time.unscaledDeltaTime)
        {
            SetEyes(eyeOpenness.Evaluate(t));
            if (blurVolume != null) blurVolume.weight = Mathf.Clamp01(blur.Evaluate(t));
            if (panting != null) panting.volume = pantingVolume * Mathf.Clamp01(t / 1.5f);
            Breathe(Mathf.Clamp01(t / SequenceLength(openTime)));
            yield return null;
        }
        SetEyes(1f);
        eyelidTop.gameObject.SetActive(false);
        eyelidBottom.gameObject.SetActive(false);

        sceneTime = openTime;
        sceneLength = SequenceLength(openTime);
        yield return Hold(openHold);

        // Gets up, looks right and then left, turns rightwards all the way round to the start door and runs straight to it.
        onFloor = false;
        yield return MoveTo(standPoint.position, standPoint.rotation, getUp);
        // The looks turn about the true vertical, so the gaze keeps its slight downward tilt with no sideways lean.
        yield return MoveTo(standPoint.position, Quaternion.Euler(0f, lookAroundAngle, 0f) * standPoint.rotation, lookRight);
        yield return MoveTo(standPoint.position, Quaternion.Euler(0f, -lookAroundAngle, 0f) * standPoint.rotation, lookLeft);
        yield return TurnRightTo(doorLookPoint.position, doorLookPoint.rotation, turnAround);
        yield return Hold(doorSpotted);
        yield return Run(doorPoint, runToDoor);

        // Rattles the locked door in a panic: no hands to show, so the camera jerks with the handle sound.
        UIButtonSounds.PlayClip(doorHandle, doorHandleVolume, doorHandleFrom);
        yield return RattleDoor(tryDoor);
        yield return Hold(afterDoor);

        // Slowly turns down and to the right to the syringe gun on the floor, then zooms in on it.
        yield return MoveTo(gunSpotPoint.position, gunSpotPoint.rotation, turnToGun);
        yield return MoveTo(gunLookPoint.position, gunLookPoint.rotation, zoomToGun);

        for (float t = 0f; t < gunHold; t += Time.unscaledDeltaTime)
        {
            Breathe(1f);
            yield return null;
        }

        yield return FinishAndLoad();
    }

    /*
     * While the screen is still black, points the camera at each later view for one frame, so everything there
     * (the door, the gun and the walls around them) is drawn once in advance. Drawing something for the first time
     * loads its textures and shaders and can cause a small hitch, which then happens here, out of sight, instead of mid-turn.
     */
    private IEnumerator Prewarm()
    {
        foreach (Transform view in new[] { standPoint, doorLookPoint, doorPoint, gunSpotPoint, gunLookPoint })
        {
            if (view == null) continue;
            menuCamera.transform.SetPositionAndRotation(view.position, view.rotation);
            yield return null;
        }
        menuCamera.transform.SetPositionAndRotation(lyingPoint.position, lyingPoint.rotation);
        yield return null;
    }

    // Where the sequence is after the eyes open, for the blur and the breathing to follow.
    private float sceneTime;
    private float sceneLength = 1f;

    // Holds the current view for the duration, with the breathing continuing.
    private IEnumerator Hold(float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            Tick();
            Breathe(sceneTime / sceneLength);
            yield return null;
        }
    }

    // How long the whole sequence takes from the start of the eyes opening to the end of the zoom.
    private float SequenceLength(float openTime)
    {
        return openTime + openHold + getUp + lookLeft + lookRight + turnAround + doorSpotted + runToDoor + tryDoor + afterDoor + turnToGun + zoomToGun;
    }

    // Moves the camera smoothly to a position and rotation, easing in and out, with the breathing on top.
    private IEnumerator MoveTo(Vector3 position, Quaternion rotation, float duration)
    {
        Vector3 fromPos = basePosition;
        Quaternion fromRot = baseRotation;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            basePosition = Vector3.Lerp(fromPos, position, k);
            baseRotation = Quaternion.Slerp(fromRot, rotation, k);
            Tick();
            Breathe(sceneTime / sceneLength);
            yield return null;
        }
        basePosition = position;
        baseRotation = rotation;
    }

    /*
     * Turns to a rotation always going rightwards (clockwise seen from above), however far round that is, so a turn back
     * from a look to the left sweeps right across the room instead of continuing left. It moves like a head turn:
     * easing out of the look to the left, sweeping round, and easing to a stop on the door.
     * Height of the gaze and position follow along. The turn starts from the current gaze exactly, so nothing jumps.
     */
    private IEnumerator TurnRightTo(Vector3 position, Quaternion rotation, float duration)
    {
        Vector3 fromPos = basePosition;
        Vector3 from = baseRotation.eulerAngles;
        Vector3 to = rotation.eulerAngles;
        float yawTurn = Mathf.Repeat(to.y - from.y, 360f);
        float pitchTurn = Mathf.DeltaAngle(from.x, to.x);
        float rollTurn = Mathf.DeltaAngle(from.z, to.z);
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            basePosition = Vector3.Lerp(fromPos, position, k);
            baseRotation = Quaternion.Euler(from.x + pitchTurn * k, from.y + yawTurn * k, from.z + rollTurn * k);
            Tick();
            Breathe(sceneTime / sceneLength);
            yield return null;
        }
        basePosition = position;
        baseRotation = rotation;
    }

    /*
     * Runs to a marker at an even pace: the head dips once per step, and a single footstep plays each time a foot lands,
     * at the bottom of each dip. Steps are cut from the footsteps recording in turn.
     */
    private IEnumerator Run(Transform marker, float duration)
    {
        Vector3 fromPos = basePosition;
        Quaternion fromRot = baseRotation;
        int stepsPlayed = 0;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            basePosition = Vector3.Lerp(fromPos, marker.position, k);
            baseRotation = Quaternion.Slerp(fromRot, marker.rotation, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(k * 2f)));
            Tick();
            Breathe(sceneTime / sceneLength);

            // A foot lands at every whole step; the head is lowest then and rises in between.
            float phase = t / stepInterval;
            menuCamera.transform.position -= Vector3.up * (1f - Mathf.Abs(Mathf.Sin(phase * Mathf.PI))) * stepBob;
            if (Mathf.FloorToInt(phase) >= stepsPlayed)
            {
                PlayStep(stepsPlayed);
                stepsPlayed++;
            }
            yield return null;
        }
        basePosition = marker.position;
        baseRotation = marker.rotation;
    }

    // Plays one step from the footsteps recording through its own short-lived source, then removes it.
    private void PlayStep(int index)
    {
        if (footsteps == null || stepStarts == null || stepStarts.Length == 0) return;

        var go = new GameObject("IntroStep");
        var source = go.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.clip = footsteps;
        source.volume = stepVolume;
        source.time = Mathf.Clamp(stepStarts[index % stepStarts.Length], 0f, footsteps.length - 0.01f);
        source.Play();
        source.SetScheduledEndTime(AudioSettings.dspTime + stepLength);
        Destroy(go, stepLength + 0.2f);
    }

    /*
     * Two hard yanks at the door's handles, timed to the two loud spikes in the door handle sound: the camera jerks
     * sharply as each one lands and settles straight after. The second try is the harder of the two.
     */
    private IEnumerator RattleDoor(float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            Tick();
            Breathe(sceneTime / sceneLength);
            float soundTime = t + doorHandleFrom;
            float burst = Mathf.Max(Yank(soundTime, firstTry) * firstTryStrength, Yank(soundTime, secondTry));
            float n = t * 28f;
            var jerk = new Vector3(Mathf.PerlinNoise(n, 0.1f) - 0.5f, Mathf.PerlinNoise(0.4f, n) - 0.5f, Mathf.PerlinNoise(n, 0.9f) - 0.5f);
            menuCamera.transform.position += menuCamera.transform.rotation * jerk * (doorShake * 2f * burst);
            menuCamera.transform.rotation *= Quaternion.Euler(jerk.y * 4f * burst, jerk.x * 3f * burst, 0f);
            yield return null;
        }
    }

    // How strong one yank is at a given time: builds up over the 0.08 s before its moment, then dies away over about 0.3 s.
    private static float Yank(float time, float at)
    {
        if (time < at - 0.08f) return 0f;
        if (time < at) return (time - (at - 0.08f)) / 0.08f;
        return Mathf.Exp(-(time - at) / 0.12f);
    }

    // Moves the sequence clock on and keeps the blur following its curve.
    private void Tick()
    {
        sceneTime += Time.unscaledDeltaTime;
        if (blurVolume != null) blurVolume.weight = Mathf.Clamp01(blur.Evaluate(sceneTime));
    }

    // Fades the skip hint in, keeps it up for a few seconds, then fades it out.
    private IEnumerator ShowSkipHint()
    {
        if (skipHint == null) yield break;

        skipHint.gameObject.SetActive(true);
        yield return FadeGroup(skipHint, 0f, 1f, skipHintFade);
        yield return new WaitForSecondsRealtime(skipHintShown);
        yield return FadeGroup(skipHint, 1f, 0f, skipHintFade);
        skipHint.gameObject.SetActive(false);
    }

    // Fades to black and fades the sounds out, then shows Loading while the game scene loads in the background.
    private IEnumerator FinishAndLoad()
    {
        // From here on, skip presses are ignored, so the scene is never loaded twice.
        bool breathing = onFloor && !skipping;
        skipping = true;
        if (skipHint != null) skipHint.gameObject.SetActive(false);
        fade.gameObject.SetActive(true);
        float startAlpha = fade.alpha;
        var sources = FindObjectsByType<AudioSource>();
        var startVolumes = new float[sources.Length];
        for (int i = 0; i < sources.Length; i++) startVolumes[i] = sources[i].volume;

        for (float t = 0f; t < finalFade; t += Time.unscaledDeltaTime)
        {
            float k = t / finalFade;
            fade.alpha = Mathf.Lerp(startAlpha, 1f, k);
            for (int i = 0; i < sources.Length; i++) if (sources[i] != null) sources[i].volume = startVolumes[i] * (1f - k);
            if (breathing) Breathe(1f);
            yield return null;
        }

        fade.alpha = 1f;
        foreach (var s in sources) if (s != null) s.Stop();
        Cursor.visible = true;

        AsyncOperation load = SceneLoader.LoadAsync(scenePath);
        if (load == null) yield break;

        // The new scene only takes over once the shortest loading time has passed, so Loading never just flashes.
        load.allowSceneActivation = false;
        if (loadingText != null) loadingText.gameObject.SetActive(true);

        float elapsed = 0f;
        while (load.progress < 0.9f || elapsed < minimumLoadingTime)
        {
            SetLoadingDots(elapsed);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        load.allowSceneActivation = true;
        while (!load.isDone)
        {
            SetLoadingDots(elapsed);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // Cycles "Loading", "Loading.", "Loading..", "Loading..."; the unused dots are drawn invisible so the word never shifts.
    private void SetLoadingDots(float elapsed)
    {
        if (loadingText == null) return;

        int dots = (int)(elapsed / loadingDotInterval) % 4;
        loadingText.text = "Loading" + new string('.', dots) + "<alpha=#00>" + new string('.', 3 - dots);
    }

    // Moves the camera with breathing: fast and deep at first (progress 0), slow and shallow at the end (progress 1).
    private void Breathe(float progress)
    {
        float rate = Mathf.Lerp(breathsPerSecondStart, breathsPerSecondEnd, progress);
        float depth = Mathf.Lerp(bobStart, bobEnd, progress);
        breathTime += Time.unscaledDeltaTime;
        breathPhase += rate * Time.unscaledDeltaTime * Mathf.PI * 2f;

        float rise = Mathf.Sin(breathPhase) * depth;
        float sway = Mathf.Sin(breathTime * 0.7f) * swayDegrees;
        float nod = Mathf.Sin(breathPhase + 0.6f) * swayDegrees * 0.5f;

        menuCamera.transform.position = basePosition + baseRotation * Vector3.up * rise;
        menuCamera.transform.rotation = baseRotation * Quaternion.Euler(nod, 0f, sway);
    }

    // 0 = eyes closed (both lids meet in the middle), 1 = fully open.
    private void SetEyes(float openness)
    {
        float h = Mathf.Lerp(closedEyelidHeight, 0f, Mathf.Clamp01(openness));
        eyelidTop.sizeDelta = new Vector2(eyelidTop.sizeDelta.x, h);
        eyelidBottom.sizeDelta = new Vector2(eyelidBottom.sizeDelta.x, h);
    }

    // Fades an overlay from one opacity to another over the duration.
    private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }

    // A crack, close a little, open more, close slightly, open more, then fully open.
    private static AnimationCurve DefaultEyes()
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.7f, 0.22f), new Keyframe(1.2f, 0.08f),
            new Keyframe(2.1f, 0.5f), new Keyframe(2.6f, 0.32f), new Keyframe(3.5f, 0.82f),
            new Keyframe(3.9f, 0.68f), new Keyframe(4.8f, 1f));
        for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
        return curve;
    }
}
