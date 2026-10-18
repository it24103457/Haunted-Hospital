using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Test-only keys for trying the game screens without playing through, for the screens test scene only.
 * F5 kills the player (death screen), F6 completes the escape (win screen), F7 deals 25 damage, F8 drains all sanity (sanity screen).
 * Damage goes through PlayerStats' own TakeDamage, sanity through its DrainSanity and the win through ObjectiveState's own CompleteEscape,
 * so the screens react exactly as they would in a real game. Pause is already on Esc.
 * A hint in the top-left corner lists the keys, with the player's health and sanity on a second line, on a faint dark backdrop,
 * clear of the objectives notes line at the top centre. Its size follows the screen height, so it reads the same at any resolution or Game view scale.
 * It is hidden while a screen is open, since OnGUI draws over the screens.
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
        if (keyboard.f8Key.wasPressedThisFrame) player.DrainSanity(player.Sanity);
    }

    private void OnGUI()
    {
        if (pauseMenu == null) pauseMenu = FindAnyObjectByType<PauseMenu>();
        bool screenOpen = (player != null && (player.IsDead || player.Sanity <= 0f)) || (objectives != null && objectives.HasWon)
                          || (pauseMenu != null && pauseMenu.IsPaused);
        if (screenOpen) return;

        // Sized for a 1080p screen and scaled to the actual height.
        float scale = Screen.height / 1080f;
        if (hintStyle == null) hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = Mathf.RoundToInt(22f * scale);
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.85f);

        // Two short lines, so the hint stays clear of the objectives notes line at the top centre.
        string stats = player != null ? $"\nHealth {player.Health:0}   Sanity {player.Sanity:0}" : string.Empty;
        var text = new GUIContent("F5 die   F6 win   F7 -" + damageStep.ToString("0") + " health   F8 lose sanity   Esc pause" + stats);
        Vector2 size = hintStyle.CalcSize(text);
        float pad = 8f * scale;
        var box = new Rect(16f * scale, 16f * scale, size.x + pad * 2f, size.y + pad);

        Color before = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.5f);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = before;
        GUI.Label(new Rect(box.x + pad, box.y + pad * 0.5f, size.x, size.y), text, hintStyle);
    }
}
