using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Test-only keys for trying the game screens without playing through, for the screens test scene only.
 * F5 kills the player (death screen), F6 completes the escape (win screen), F7 deals 25 damage.
 * Damage goes through PlayerStats' own TakeDamage and the win through ObjectiveState's own CompleteEscape,
 * so the screens react exactly as they would in a real game. Pause is already on Esc.
 * A small hint in the bottom-left corner lists the keys; it is hidden while a screen is open, since OnGUI draws over the screens.
 */
public class ScreensTestDebugKeys : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damageStep = 25f;

    private PlayerStats player;
    private ObjectiveState objectives;
    private PauseMenu pauseMenu;
    private GUIStyle hintStyle;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerStats>();
        objectives = FindAnyObjectByType<ObjectiveState>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || player == null) return;

        if (keyboard.f5Key.wasPressedThisFrame) player.TakeDamage(new DamageInfo(player.Health, gameObject));
        if (keyboard.f7Key.wasPressedThisFrame) player.TakeDamage(new DamageInfo(damageStep, gameObject));
        if (keyboard.f6Key.wasPressedThisFrame && objectives != null && !player.IsDead) objectives.CompleteEscape();
    }

    private void OnGUI()
    {
        if (pauseMenu == null) pauseMenu = FindAnyObjectByType<PauseMenu>();
        bool screenOpen = (player != null && player.IsDead) || (objectives != null && objectives.HasWon) || (pauseMenu != null && pauseMenu.IsPaused);
        if (screenOpen) return;

        if (hintStyle == null) hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.6f);

        string health = player != null ? $"   Health {player.Health:0}" : string.Empty;
        GUI.Label(new Rect(12f, Screen.height - 34f, 700f, 26f), "Test keys: F5 die   F6 win   F7 -" + damageStep.ToString("0") + " health   Esc pause" + health, hintStyle);
    }
}
