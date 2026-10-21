using System.Collections;
using UnityEngine;

/*
 * Shows the sanity screen when the player's sanity runs out: "YOU LOST YOUR MIND" in blue with Restart and Main Menu, and a scream.
 * It watches PlayerStats' sanity and reacts the moment it reaches 0; PlayerStats itself does not end the run at 0 sanity.
 * The player's controls are frozen and the mouse is freed straight away (through PlayerControlLock), then after a short pause
 * the screen fades in and the scream plays. Once it is fully shown, game time and game audio stop.
 * If the player dies during that short pause, this screen gives way and the death screen shows instead.
 * The objectives HUD (drawn with OnGUI, always on top of any Canvas) is switched off as the screen appears; it comes back
 * on its own when the level is restarted. The fade runs on real time.
 */
public class SanityScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [Tooltip("Seconds between sanity running out and the screen starting to appear.")]
    [SerializeField, Min(0f)] private float delay = 0.8f;
    [SerializeField, Min(0.01f)] private float fadeTime = 1.2f;
    [Tooltip("Played as the screen starts to appear. Keeps playing after the game's audio stops.")]
    [SerializeField] private AudioClip sound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.9f;
    [Tooltip("Seconds skipped at the start of the sound.")]
    [SerializeField, Min(0f)] private float soundStartFrom = 1f;

    private PlayerStats player;

    public bool IsShowing { get; private set; }

    // Starts hidden and see-through, so nothing shows until sanity runs out.
    private void Awake()
    {
        SetVisible(false, 0f);
    }

    // Listens for the player's sanity changing.
    private void Start()
    {
        player = FindAnyObjectByType<PlayerStats>();
        if (player == null) return;

        player.SanityChanged += OnSanityChanged;
    }

    // Stops listening, so a destroyed screen is never called by the player.
    private void OnDestroy()
    {
        if (player != null) player.SanityChanged -= OnSanityChanged;
    }

    // Starts showing the screen once, when sanity reaches 0 while the player is still alive.
    private void OnSanityChanged(float current, float max)
    {
        if (IsShowing || current > 0f || player.IsDead) return;
        IsShowing = true;
        StartCoroutine(Show());
    }

    /*
     * Freezes the controls and waits the short delay; if the player died meanwhile the death screen is left to show instead.
     * Otherwise hides the objective messages, plays the scream and fades the screen in, then stops game time and audio.
     */
    private IEnumerator Show()
    {
        PlayerControlLock.Set(true);
        yield return new WaitForSecondsRealtime(delay);

        // Dying during the pause means the death screen is the one to show.
        if (player.IsDead)
        {
            IsShowing = false;
            yield break;
        }

        if (ObjectiveHud.Instance != null) ObjectiveHud.Instance.enabled = false;
        UIButtonSounds.PlayClip(sound, soundVolume, soundStartFrom);

        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            SetVisible(true, t / fadeTime);
            yield return null;
        }

        SetVisible(true, 1f);
        panel.interactable = true;
        panel.blocksRaycasts = true;

        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    // Shows the panel at the given opacity; buttons only become clickable once the fade has finished.
    private void SetVisible(bool visible, float alpha)
    {
        panel.gameObject.SetActive(visible);
        panel.alpha = alpha;
        panel.interactable = false;
        panel.blocksRaycasts = false;
    }
}
