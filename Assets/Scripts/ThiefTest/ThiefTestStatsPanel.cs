using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Test-only on-screen panel for the Thief test scene.
 * Shows the player's health, sanity and reserve ammo with a minus and a plus button on either side of each,
 * so the Thief's need-based decisions can be tried against different player states.
 * Each player stat is shown in the colour of the item that restores it (Medkit pink, Sanity cyan, Ammo yellow),
 * matching the item labels in the scene.
 * A top row shows the Thief's health in the Thief's own colour, changed with numpad 3 (down) and 6 (up);
 * taking it to 0 kills the Thief as a real hit would.
 * Every button also has a numpad key that works at any time, without a cursor. The player keys follow the
 * panel's layout from bottom to top: 1/2 ammo, 4/5 sanity, 7/8 health.
 * Player changes go through PlayerStats' own public methods. Player health never drops below 1 from the panel,
 * because PlayerStats can't heal a dead player.
 * PlayerController locks the mouse on every click, so Tab switches to an edit mode that pauses the
 * controller and keeps the mouse free for the buttons; Tab again hands control back.
 */
[RequireComponent(typeof(PlayerStats))]
public class ThiefTestStatsPanel : MonoBehaviour
{
    [SerializeField] private float healthStep = 10f;
    [SerializeField] private float sanityStep = 10f;
    [SerializeField] private int ammoStep = 10;
    [SerializeField] private float thiefHealthStep = 25f;

    // Same magenta as the Thief's path line and state label, so its row reads as the Thief's.
    private static readonly Color ThiefColor = new Color(1f, 0.2f, 1f);

    private PlayerStats stats;
    private PlayerController controller;
    private ThiefAgent thief;
    private bool editing;
    private GUIStyle valueStyle;
    private GUIStyle buttonStyle;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        controller = GetComponent<PlayerController>();
        thief = FindAnyObjectByType<ThiefAgent>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.tabKey.wasPressedThisFrame)
        {
            editing = !editing;
            if (controller != null) controller.enabled = !editing;
        }

        if (keyboard.numpad7Key.wasPressedThisFrame) LowerHealth();
        if (keyboard.numpad8Key.wasPressedThisFrame) RaiseHealth();
        if (keyboard.numpad4Key.wasPressedThisFrame) LowerSanity();
        if (keyboard.numpad5Key.wasPressedThisFrame) RaiseSanity();
        if (keyboard.numpad1Key.wasPressedThisFrame) LowerAmmo();
        if (keyboard.numpad2Key.wasPressedThisFrame) RaiseAmmo();
        if (keyboard.numpad3Key.wasPressedThisFrame) LowerThiefHealth();
        if (keyboard.numpad6Key.wasPressedThisFrame) RaiseThiefHealth();
    }

    // Re-applied every frame in edit mode, after anything else that may have locked the mouse again.
    private void LateUpdate()
    {
        if (!editing) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnGUI()
    {
        if (valueStyle == null) valueStyle = new GUIStyle();
        valueStyle.alignment = TextAnchor.MiddleCenter;
        valueStyle.fontSize = 20;
        valueStyle.fontStyle = FontStyle.Bold;
        valueStyle.normal.textColor = Color.white;

        if (buttonStyle == null) buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 18;
        buttonStyle.fontStyle = FontStyle.Bold;

        const float x = 20f, rowHeight = 40f, buttonWidth = 56f, valueWidth = 230f;
        float y = Screen.height - 4 * rowHeight - 30f;

        GUI.Box(new Rect(x - 10f, y - 40f, valueWidth + 2 * buttonWidth + 40f, 4 * rowHeight + 50f), GUIContent.none);
        GUI.Label(new Rect(x, y - 36f, valueWidth + 2 * buttonWidth + 20f, 28f),
                  editing ? "Editing stats (Tab to play)" : "Numpad keys, or Tab to click", valueStyle);

        string thiefText = thief == null ? "Thief: none in scene"
                         : $"Thief {thief.Health:0}/{thief.MaxHealth:0}{(thief.IsAlive ? "" : " DEAD")}";
        Row(x, y, buttonWidth, valueWidth, rowHeight, "3  -", "+  6", thiefText, ThiefColor, LowerThiefHealth, RaiseThiefHealth);
        // Each player stat uses the colour of the item that restores it, matching the item labels in the scene.
        Row(x, y + rowHeight, buttonWidth, valueWidth, rowHeight, "7  -", "+  8",
            $"Health {stats.Health:0}/{stats.MaxHealth:0}{(stats.IsDead ? " DEAD" : "")}",
            ThiefTestWorldLabels.ItemColor(PickupType.Health), LowerHealth, RaiseHealth);
        Row(x, y + 2 * rowHeight, buttonWidth, valueWidth, rowHeight, "4  -", "+  5",
            $"Sanity {stats.Sanity:0}/{stats.MaxSanity:0}",
            ThiefTestWorldLabels.ItemColor(PickupType.Sanity), LowerSanity, RaiseSanity);
        Row(x, y + 3 * rowHeight, buttonWidth, valueWidth, rowHeight, "1  -", "+  2",
            $"Ammo {stats.ReserveAmmo}/{stats.MaxReserveAmmo}",
            ThiefTestWorldLabels.ItemColor(PickupType.Ammo), LowerAmmo, RaiseAmmo);
    }

    // One line of the panel: minus button, value text in the given colour, plus button, each button labelled with its key.
    private void Row(float x, float y, float buttonWidth, float valueWidth, float height, string minusLabel,
                     string plusLabel, string text, Color color, System.Action minus, System.Action plus)
    {
        valueStyle.normal.textColor = color;

        if (GUI.Button(new Rect(x, y, buttonWidth, height - 6f), minusLabel, buttonStyle)) minus();
        GUI.Label(new Rect(x + buttonWidth + 10f, y, valueWidth, height - 6f), text, valueStyle);
        if (GUI.Button(new Rect(x + buttonWidth + valueWidth + 20f, y, buttonWidth, height - 6f), plusLabel, buttonStyle)) plus();
    }

    // Damage by one step, stopping at 1 health so the player stays alive and can still be healed.
    private void LowerHealth()
    {
        float amount = Mathf.Min(healthStep, stats.Health - 1f);
        if (amount > 0f) stats.TakeDamage(new DamageInfo(amount, gameObject));
    }

    private void RaiseHealth() => stats.Heal(healthStep);
    private void LowerSanity() => stats.DrainSanity(sanityStep);
    private void RaiseSanity() => stats.AddSanity(sanityStep);
    private void LowerAmmo() => stats.TryUseAmmo(Mathf.Min(ammoStep, stats.ReserveAmmo));
    private void RaiseAmmo() => stats.AddAmmo(ammoStep);

    // Lowering uses the Thief's normal TakeDamage, so reaching 0 kills it exactly as a hit would.
    private void LowerThiefHealth()
    {
        if (thief != null) thief.TakeDamage(new DamageInfo(thiefHealthStep, gameObject));
    }

    private void RaiseThiefHealth()
    {
        if (thief != null) thief.Heal(thiefHealthStep);
    }
}
