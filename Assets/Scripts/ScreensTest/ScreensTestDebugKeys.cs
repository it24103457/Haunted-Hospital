using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Test-only keys for trying the game screens without playing through, for the screens test scene only.
 * F6 completes the escape (win screen), F7 takes all health (death screen), F8 drains all sanity (sanity screen).
 * Damage goes through PlayerStats' own TakeDamage, sanity through its DrainSanity and the win through ObjectiveState's own CompleteEscape,
 * so the screens react exactly as they would in a real game. Pause is already on Esc.
 * Objective keys: F4 gives or takes the exit key (the key icon's pickup animation, shimmer and label), F9 reads the next code digit
 * through ObjectiveState.ReadClue, exactly as a clue paper does (the run's real digit if a code is set, otherwise a random
 * one) and F10 resets the run's objectives (ObjectiveState.ResetRun).
 * Numpad keys change the player's stats by a step each (10 by default), in the same top-to-bottom order as the HUD's bars:
 * 7 / 8 health down / up, 4 / 5 reserve ammo down / up, 1 / 2 sanity down / up; going below 15 shows the low health or
 * low sanity effect. Everything goes through PlayerStats and ObjectiveState, so the HUD reacts exactly as in a real game.
 * A hint in the top-left corner lists the keys in three groups (screens, objectives, numpad), with the player's health, sanity, reserve ammo
 * and whether the key is held on the last line, on a faint dark backdrop. Its size follows the screen height, so it reads
 * the same at any resolution or Game view scale.
 * It is hidden while a screen is open, since OnGUI draws over the screens.
 */
public class ScreensTestDebugKeys : MonoBehaviour
{
    [SerializeField, Min(0f)] private float healthStep = 10f;
    [SerializeField, Min(0f)] private float sanityStep = 10f;
    [SerializeField, Min(0)] private int ammoStep = 10;

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

        if (keyboard.f7Key.wasPressedThisFrame) player.TakeDamage(new DamageInfo(player.Health, gameObject));
        if (keyboard.f6Key.wasPressedThisFrame && objectives != null && !player.IsDead) objectives.CompleteEscape();
        if (keyboard.f8Key.wasPressedThisFrame) player.DrainSanity(player.Sanity);
        if (keyboard.numpad7Key.wasPressedThisFrame) player.TakeDamage(new DamageInfo(Mathf.Min(healthStep, player.Health), gameObject));
        if (keyboard.numpad8Key.wasPressedThisFrame) player.Heal(healthStep);
        if (keyboard.numpad4Key.wasPressedThisFrame) player.TryUseAmmo(Mathf.Min(ammoStep, player.ReserveAmmo));
        if (keyboard.numpad5Key.wasPressedThisFrame) player.AddAmmo(ammoStep);
        if (keyboard.numpad1Key.wasPressedThisFrame) player.DrainSanity(Mathf.Min(sanityStep, player.Sanity));
        if (keyboard.numpad2Key.wasPressedThisFrame) player.AddSanity(sanityStep);
        if (keyboard.f4Key.wasPressedThisFrame) player.SetExitKey(!player.HasExitKey);

        if (objectives == null) return;
        if (keyboard.f9Key.wasPressedThisFrame) ReadNextDigit();
        if (keyboard.f10Key.wasPressedThisFrame) objectives.ResetRun();
    }

    // Reads the first undiscovered position, with the run's real digit when a code is set (ReadClue rejects a wrong one).
    private void ReadNextDigit()
    {
        int position = NextUndiscoveredPosition();
        if (position > 4) return;

        string code = objectives.Code;
        int digit = code.Length == 4 ? code[position - 1] - '0' : Random.Range(0, 10);
        objectives.ReadClue(-2000 - position, position, digit);
    }

    // The first code position (1 to 4) not yet discovered, or 5 when all four are.
    private int NextUndiscoveredPosition()
    {
        int position = 1;
        while (position <= 4 && objectives.TryGetDigit(position, out _)) position++;
        return position;
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

        // One line per group of keys, then the player's current values to compare against the HUD.
        int next = objectives != null ? NextUndiscoveredPosition() : 1;
        string nextDigit = next <= 4 ? $"F9 next code digit ({next}/4)" : "F9 code complete";
        string stats = player != null
            ? $"\nHealth {player.Health:0}   Sanity {player.Sanity:0}   Ammo {player.ReserveAmmo}   Key {(player.HasExitKey ? "held" : "no")}"
            : string.Empty;
        var text = new GUIContent(
            "Screens:  F6 win   F7 health to 0   F8 sanity to 0   Esc pause" +
            "\nObjectives:  F4 give/take key   " + nextDigit + "   F10 reset objectives" +
            "\nNumpad:  7/8 health -/+   4/5 ammo -/+   1/2 sanity -/+   (" + healthStep.ToString("0") + " each)" + stats);
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
