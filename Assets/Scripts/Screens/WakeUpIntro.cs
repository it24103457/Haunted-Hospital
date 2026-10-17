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
 * and the panting sound loops in. Once the eyes are open the camera turns to look at the gun, holds,
 * and the screen fades to black, as if the gun has been picked up, before the game scene loads.
 * Space, Esc or a click skips straight to that last fade. A small "SPACE Skip" hint shows in the corner for a few seconds
 * as the eyes start to open. Once the screen is black, "Loading" with cycling dots shows in the centre while the game scene
 * loads in the background. All timing runs on real time.
 * The lying and gun-look camera positions are two marker objects in the scene, so they can be moved in the Scene view.
 */
public class WakeUpIntro : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private Camera menuCamera;
    [SerializeField] private Transform lyingPoint;
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
    [SerializeField, Min(0.01f)] private float turnToGun = 2f;
    [SerializeField, Min(0f)] private float gunHold = 1f;
    [SerializeField, Min(0.01f)] private float finalFade = 1.5f;

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
        StartCoroutine(Run());
    }

    private void Update()
    {
        if (!started || skipping) return;

        bool skip = (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame))
                    || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (skip)
        {
            skipping = true;
            StopAllCoroutines();
            StartCoroutine(FinishAndLoad());
        }
    }

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

        yield return new WaitForSecondsRealtime(blackHold);
        fade.alpha = 0f;
        StartCoroutine(ShowSkipHint());

        float openTime = eyeOpenness.keys[eyeOpenness.length - 1].time;
        for (float t = 0f; t < openTime; t += Time.unscaledDeltaTime)
        {
            SetEyes(eyeOpenness.Evaluate(t));
            if (blurVolume != null) blurVolume.weight = Mathf.Clamp01(blur.Evaluate(t));
            if (panting != null) panting.volume = pantingVolume * Mathf.Clamp01(t / 1.5f);
            Breathe(Mathf.Clamp01(t / (openTime + openHold + turnToGun)));
            yield return null;
        }
        SetEyes(1f);
        eyelidTop.gameObject.SetActive(false);
        eyelidBottom.gameObject.SetActive(false);

        float settle = openTime;
        float total = openTime + openHold + turnToGun;
        for (float t = 0f; t < openHold; t += Time.unscaledDeltaTime)
        {
            if (blurVolume != null) blurVolume.weight = Mathf.Clamp01(blur.Evaluate(settle + t));
            Breathe((settle + t) / total);
            yield return null;
        }

        Quaternion from = baseRotation;
        Vector3 fromPos = basePosition;
        for (float t = 0f; t < turnToGun; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / turnToGun);
            basePosition = Vector3.Lerp(fromPos, gunLookPoint.position, k);
            baseRotation = Quaternion.Slerp(from, gunLookPoint.rotation, k);
            if (blurVolume != null) blurVolume.weight = Mathf.Clamp01(blur.Evaluate(settle + openHold + t));
            Breathe((settle + openHold + t) / total);
            yield return null;
        }

        for (float t = 0f; t < gunHold; t += Time.unscaledDeltaTime)
        {
            Breathe(1f);
            yield return null;
        }

        yield return FinishAndLoad();
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
