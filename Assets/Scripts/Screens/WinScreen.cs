using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
#if UNITY_EDITOR
using UnityEditor;
#endif

/*
 * Shows the win screen when the escape is completed (ObjectiveState.HasWon).
 * The moment the game is won, the objectives HUD is switched off, so its own OnGUI win screen never appears, not even for a frame.
 * The screen then fades to black and the game's sounds fade out with it.
 * If an escape cutscene scene is set, "Loading" with cycling dots shows over the black while that scene loads, and the cutscene
 * takes over; it ends on its own "YOU ESCAPED" screen, whose Play Again returns to the level remembered here.
 * Without a cutscene scene, it plays the optional video cutscene if one is assigned, then fades "YOU ESCAPED" with Play Again
 * and Main Menu in over the black as its sound plays; once the buttons appear, game time and game audio stop.
 * The backdrop opacity is a setting: fully opaque by default, so the game world never shows through after the escape or a cutscene.
 * All fades run on real time. The cutscene scene is picked as a scene asset; in the Editor its current path is always read from it,
 * so moving or renaming the scene never breaks the link; the stored copy of the path is what a built game uses.
 */
public class WinScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private CanvasGroup content;
    [SerializeField] private Image backdrop;
    [Tooltip("How opaque the black backdrop is. 1 hides the game completely, which suits playing a cutscene before the screen.")]
    [SerializeField, Range(0f, 1f)] private float backdropOpacity = 1f;

    [Header("Timing (seconds)")]
    [SerializeField, Min(0f)] private float delay = 0.5f;
    [SerializeField, Min(0.01f)] private float fadeToBlack = 1.5f;
    [SerializeField, Min(0.01f)] private float contentFade = 1.2f;

    [Header("Sound")]
    [Tooltip("Played as the title starts to appear over the black. Keeps playing after the game's audio stops.")]
    [SerializeField] private AudioClip sound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.9f;

    [Header("Escape cutscene scene")]
#if UNITY_EDITOR
    [Tooltip("Loaded after the fade to black instead of showing this screen. Leave empty to show this screen as before.")]
    [SerializeField] private SceneAsset cutsceneScene;
#endif
    [SerializeField, HideInInspector] private string cutsceneScenePath;
    [SerializeField] private TMPro.TMP_Text loadingText;
    [SerializeField, Min(0.05f)] private float loadingDotInterval = 0.4f;
    [Tooltip("Shortest time Loading stays on screen, so a fast load does not just flash it.")]
    [SerializeField, Min(0f)] private float minimumLoadingTime = 1f;

    [Header("Optional video cutscene")]
    [Tooltip("Played full screen after the fade to black and before the title appears. Leave empty for no video.")]
    [SerializeField] private VideoPlayer cutscene;

    private ObjectiveState objectives;

    public bool IsShowing { get; private set; }

    private string CutscenePath
    {
        get
        {
#if UNITY_EDITOR
            if (cutsceneScene != null) return AssetDatabase.GetAssetPath(cutsceneScene);
#endif
            return cutsceneScenePath;
        }
    }

    // Starts with the panel, the cutscene video and the loading text hidden.
    private void Awake()
    {
        panel.gameObject.SetActive(false);
        if (cutscene != null) cutscene.gameObject.SetActive(false);
        if (loadingText != null) loadingText.gameObject.SetActive(false);
    }

    // Listens for the objectives changing, and checks once straight away in case the game is already won.
    private void Start()
    {
        objectives = FindAnyObjectByType<ObjectiveState>();
        if (objectives == null) return;

        objectives.StateChanged += OnStateChanged;
        OnStateChanged();
    }

    // Stops listening, so a destroyed screen is never called by the objectives.
    private void OnDestroy()
    {
        if (objectives != null) objectives.StateChanged -= OnStateChanged;
    }

    // Starts showing the win once, when the objectives report the game won, and hides the old objective messages.
    private void OnStateChanged()
    {
        if (IsShowing || !objectives.HasWon) return;
        IsShowing = true;

        if (ObjectiveHud.Instance != null) ObjectiveHud.Instance.enabled = false;
        StartCoroutine(Show());
    }

    /*
     * Freezes the controls and fades the game out; then either loads the escape cutscene scene, or plays the cutscene video
     * if one is set, and finally fades in the win text and buttons and stops game time and audio.
     */
    private IEnumerator Show()
    {
        PlayerControlLock.Set(true);

        Color c = backdrop.color;
        c.a = backdropOpacity;
        backdrop.color = c;

        panel.gameObject.SetActive(true);
        SetGroup(panel, 0f, false);
        SetGroup(content, 0f, false);
        yield return new WaitForSecondsRealtime(delay);

        bool toCutsceneScene = !string.IsNullOrEmpty(CutscenePath);
        yield return FadeOutGame(toCutsceneScene);

        if (toCutsceneScene)
        {
            yield return LoadCutsceneScene();
            yield break;
        }

        if (cutscene != null) yield return PlayCutscene();

        UIButtonSounds.PlayClip(sound, soundVolume);
        yield return Fade(content, contentFade);
        SetGroup(content, 1f, true);
        SetGroup(panel, 1f, true);

        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    // Fades to black; when a cutscene scene follows, the game's sounds fade out with it and stop.
    private IEnumerator FadeOutGame(bool fadeSounds)
    {
        AudioSource[] sources = fadeSounds ? FindObjectsByType<AudioSource>() : new AudioSource[0];
        var startVolumes = new float[sources.Length];
        for (int i = 0; i < sources.Length; i++) startVolumes[i] = sources[i].volume;

        for (float t = 0f; t < fadeToBlack; t += Time.unscaledDeltaTime)
        {
            float k = t / fadeToBlack;
            panel.alpha = k;
            for (int i = 0; i < sources.Length; i++) if (sources[i] != null) sources[i].volume = startVolumes[i] * (1f - k);
            yield return null;
        }
        panel.alpha = 1f;
        foreach (var s in sources) if (s != null) s.Stop();
    }

    /*
     * Remembers this level for the cutscene's Play Again, then loads the cutscene scene in the background with Loading on screen.
     * The cutscene only takes over once the shortest loading time has passed, so Loading never just flashes.
     */
    private IEnumerator LoadCutsceneScene()
    {
        SceneLoader.LastLevelPath = SceneManager.GetActiveScene().path;
        AsyncOperation load = SceneLoader.LoadAsync(CutscenePath);
        if (load == null) yield break;

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

    // Plays the cutscene video from start to finish, waiting for it to load first, then hides it.
    private IEnumerator PlayCutscene()
    {
        cutscene.gameObject.SetActive(true);
        cutscene.Prepare();
        while (!cutscene.isPrepared) yield return null;

        cutscene.Play();
        while (!cutscene.isPlaying) yield return null;
        while (cutscene.isPlaying) yield return null;

        cutscene.gameObject.SetActive(false);
    }

    // Fades a group in from see-through to fully visible over the duration.
    private static IEnumerator Fade(CanvasGroup group, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / duration;
            yield return null;
        }
        group.alpha = 1f;
    }

    // Sets a group's opacity and whether its buttons can be clicked.
    private static void SetGroup(CanvasGroup group, float alpha, bool clickable)
    {
        group.alpha = alpha;
        group.interactable = clickable;
        group.blocksRaycasts = clickable;
    }

#if UNITY_EDITOR
    // In the Editor, stores the chosen cutscene scene's path, since a scene asset can't be referenced in a build.
    private void OnValidate()
    {
        cutsceneScenePath = cutsceneScene != null ? AssetDatabase.GetAssetPath(cutsceneScene) : string.Empty;
    }
#endif
}
