using UnityEngine;
using UnityEngine.InputSystem;

/*
 * The pause menu: Esc opens it with Resume, Restart and Main Menu, and Esc or Resume closes it.
 * While paused, game time and game audio stop, the player's controls are frozen and the mouse is freed (through PlayerControlLock),
 * and the objectives HUD is hidden, since its OnGUI text always draws on top of any Canvas. Resuming puts all of that back.
 * Pausing is not allowed while the death or sanity screen is up, after the game is won, or while the keypad screen is open,
 * because those already control the mouse. The keypad also closes on Esc, so a press that closes it never opens the pause menu.
 * Restart and Main Menu go through ScreenNavigation, which sets time and audio back to normal before loading.
 */
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private DeathScreen deathScreen;
    [SerializeField] private SanityScreen sanityScreen;

    private ObjectiveState objectives;
    private PlayerStats player;
    private bool keypadWasOpen;
    private bool hudWasEnabled;

    public bool IsPaused { get; private set; }

    private void Start()
    {
        panel.SetActive(false);
        objectives = FindAnyObjectByType<ObjectiveState>();
        player = FindAnyObjectByType<PlayerStats>();
    }

    private void Update()
    {
        bool keypadOpen = objectives != null && objectives.IsKeypadOpen;
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !keypadOpen && !keypadWasOpen)
        {
            if (IsPaused) Resume();
            else if (CanPause()) Pause();
        }

        keypadWasOpen = keypadOpen;
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        panel.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        PlayerControlLock.Set(true);

        if (ObjectiveHud.Instance != null)
        {
            hudWasEnabled = ObjectiveHud.Instance.enabled;
            ObjectiveHud.Instance.enabled = false;
        }
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        panel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        PlayerControlLock.Set(false);

        if (ObjectiveHud.Instance != null) ObjectiveHud.Instance.enabled = hudWasEnabled;
    }

    private bool CanPause()
    {
        if (player != null && player.IsDead) return false;
        if (deathScreen != null && deathScreen.IsShowing) return false;
        if (sanityScreen != null && sanityScreen.IsShowing) return false;
        if (objectives != null && objectives.HasWon) return false;
        return true;
    }
}
