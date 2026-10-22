using System.Collections;
using UnityEngine;

/*
 * Shows the death screen when the player dies: "YOU DIED" with Restart and Main Menu.
 * On death the player's controls are frozen and the mouse is freed straight away (through PlayerControlLock),
 * then after a short pause the screen fades in and its sound plays. Once it is fully shown, game time and game audio stop,
 * so nothing keeps moving or making noise behind it. Button sounds still play, since they ignore the audio pause.
 * The fade runs on real time, so it is not affected by the game clock.
 * The objective messages are drawn with OnGUI, which always appears on top of any Canvas,
 * so it is switched off as the screen appears. It comes back on its own when the level is restarted.
 */
public class DeathScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [Tooltip("Seconds between dying and the screen starting to appear, so the moment of death registers first.")]
    [SerializeField, Min(0f)] private float delay = 0.8f;
    [SerializeField, Min(0.01f)] private float fadeTime = 1.2f;
    [Tooltip("Played as the screen starts to appear. Keeps playing after the game's audio stops.")]
    [SerializeField] private AudioClip sound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.9f;

    private PlayerStats player;

    public bool IsShowing { get; private set; }

    // Starts hidden and see-through, so nothing shows until the player dies.
    private void Awake()
    {
        SetVisible(false, 0f);
    }

    // Listens for the player dying; if the player is already dead (a very early death), shows the screen at once.
    private void Start()
    {
        player = FindAnyObjectByType<PlayerStats>();
        if (player == null) return;

        player.Died += OnDied;
        if (player.IsDead) OnDied();
    }

    // Stops listening, so a destroyed screen is never called by the player.
    private void OnDestroy()
    {
        if (player != null) player.Died -= OnDied;
    }

    // Starts showing the screen once, however many times the death is reported.
    private void OnDied()
    {
        if (IsShowing) return;
        IsShowing = true;
        StartCoroutine(Show());
    }

    /*
     * Freezes the controls, waits the short delay, hides the objective messages, plays the sound and fades the screen in.
     * Once fully shown the buttons work, and game time and game audio stop.
     */
    private IEnumerator Show()
    {
        PlayerControlLock.Set(true);
        yield return new WaitForSecondsRealtime(delay);

        if (ObjectiveHud.Instance != null) ObjectiveHud.Instance.enabled = false;
        UIButtonSounds.PlayClip(sound, soundVolume);

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
