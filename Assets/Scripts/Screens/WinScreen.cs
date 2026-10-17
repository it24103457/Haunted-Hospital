using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/*
 * Shows the win screen when the escape is completed (ObjectiveState.HasWon): "YOU ESCAPED" with Play Again and Main Menu.
 * The moment the game is won, the objectives HUD is switched off, so its own OnGUI win screen never appears, not even for a frame.
 * The screen then fades to black, plays the optional cutscene if one is assigned, and fades the title and buttons in over the black.
 * The backdrop opacity is a setting: fully opaque by default, so the game world never shows through after the escape or a cutscene.
 * Once the buttons appear, game time and game audio stop and the mouse is freed. All fades run on real time.
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

    [Header("Optional cutscene")]
    [Tooltip("Played full screen after the fade to black and before the title appears. Leave empty for no cutscene.")]
    [SerializeField] private VideoPlayer cutscene;

    private ObjectiveState objectives;

    public bool IsShowing { get; private set; }

    private void Awake()
    {
        panel.gameObject.SetActive(false);
        if (cutscene != null) cutscene.gameObject.SetActive(false);
    }

    private void Start()
    {
        objectives = FindAnyObjectByType<ObjectiveState>();
        if (objectives == null) return;

        objectives.StateChanged += OnStateChanged;
        OnStateChanged();
    }

    private void OnDestroy()
    {
        if (objectives != null) objectives.StateChanged -= OnStateChanged;
    }

    private void OnStateChanged()
    {
        if (IsShowing || !objectives.HasWon) return;
        IsShowing = true;

        if (ObjectiveHud.Instance != null) ObjectiveHud.Instance.enabled = false;
        StartCoroutine(Show());
    }

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

        yield return Fade(panel, fadeToBlack);

        if (cutscene != null) yield return PlayCutscene();

        yield return Fade(content, contentFade);
        SetGroup(content, 1f, true);
        SetGroup(panel, 1f, true);

        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

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

    private static IEnumerator Fade(CanvasGroup group, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / duration;
            yield return null;
        }
        group.alpha = 1f;
    }

    private static void SetGroup(CanvasGroup group, float alpha, bool clickable)
    {
        group.alpha = alpha;
        group.interactable = clickable;
        group.blocksRaycasts = clickable;
    }
}
