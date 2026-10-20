using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Test-only keys for trying the game screens without playing through, for the screens test scene only.
 * F5 kills the player (death screen), F6 completes the escape (win screen), F7 deals 25 damage, F8 drains all sanity (sanity screen).
 * Damage goes through PlayerStats' own TakeDamage, sanity through its DrainSanity and the win through ObjectiveState's own CompleteEscape,
 * so the screens react exactly as they would in a real game. Pause is already on Esc.
 * HUD keys: F9 shows the next code digit (a random one) in the HUD's code slots and fills the key icon a quarter, F10 empties them, F11 takes 10 sanity and F12
 * uses 10 rounds of reserve ammo, and F4 gives or takes the exit key. The digits go straight into the HUD, standing in for clue papers being read; sanity and
 * ammo go through PlayerStats like the rest, so their bars move exactly as in a real game; so does the key (SetExitKey).
 * A hint in the top-left corner lists the keys, with the player's health and sanity on the last line, on a faint dark backdrop,
 * clear of the objectives notes line at the top centre. Its size follows the screen height, so it reads the same at any resolution or Game view scale.
 * It is hidden while a screen is open, since OnGUI draws over the screens.
 */
public class ScreensTestDebugKeys : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damageStep = 25f;
    [SerializeField, Min(0f)] private float sanityStep = 10f;
    [SerializeField, Min(0)] private int ammoStep = 10;

    private PlayerStats player;
    private ObjectiveState objectives;
    private PauseMenu pauseMenu;
    private PlayerHud hud;
    private int nextDigitPosition = 1;
    private GUIStyle hintStyle;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerStats>();
        objectives = FindAnyObjectByType<ObjectiveState>();
        hud = FindAnyObjectByType<PlayerHud>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || player == null) return;

        if (keyboard.f5Key.wasPressedThisFrame) player.TakeDamage(new DamageInfo(player.Health, gameObject));
        if (keyboard.f7Key.wasPressedThisFrame) player.TakeDamage(new DamageInfo(damageStep, gameObject));
        if (keyboard.f6Key.wasPressedThisFrame && objectives != null && !player.IsDead) objectives.CompleteEscape();
        if (keyboard.f8Key.wasPressedThisFrame) player.DrainSanity(player.Sanity);
        if (keyboard.f11Key.wasPressedThisFrame) player.DrainSanity(sanityStep);
        if (keyboard.f12Key.wasPressedThisFrame) player.TryUseAmmo(Mathf.Min(ammoStep, player.ReserveAmmo));
        if (keyboard.f4Key.wasPressedThisFrame) player.SetExitKey(!player.HasExitKey);

        if (hud == null) return;
        if (keyboard.f9Key.wasPressedThisFrame && nextDigitPosition <= 4)
        {
            hud.SetDigit(nextDigitPosition, Random.Range(0, 10));
            hud.SetKeyProgress(nextDigitPosition / 4f);
            nextDigitPosition++;
        }
        if (keyboard.f10Key.wasPressedThisFrame)
        {
            hud.ClearDigits();
            hud.SetKeyProgress(0f);
            nextDigitPosition = 1;
        }
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

        // Short lines, so the hint stays clear of the objectives notes line at the top centre.
        string stats = player != null ? $"\nHealth {player.Health:0}   Sanity {player.Sanity:0}" : string.Empty;
        var text = new GUIContent("F5 die   F6 win   F7 -" + damageStep.ToString("0") + " health   F8 lose sanity   Esc pause" +
                                  "\nF9 next code digit   F10 clear digits   F11 -" + sanityStep.ToString("0") + " sanity   F12 -" +
                                  ammoStep + " ammo   F4 exit key" + stats);
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
