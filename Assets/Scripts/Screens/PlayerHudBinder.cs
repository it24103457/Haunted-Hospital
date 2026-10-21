using UnityEngine;

/*
 * Feeds the HUD from the game. Health, ammo and sanity come from PlayerStats' change events, and the starting values are
 * shown straight away. Ammo is the reserve the player carries against the most they can carry; the magazine comes from the
 * player's PlayerWeapon (rounds loaded against the magazine size), so it drops with each shot and refills on reload.
 * The key icon's outline draws in a quarter for each code digit found; it fills (with its pickup animation) once
 * PlayerStats says the player holds the exit key (ExitKeyChanged, starting from whether the key is already held).
 * Code digits come from ObjectiveState: each DigitFound puts the digit into its slot and draws another quarter of the key
 * outline. On start, and whenever the state changes (for example a reset of the run), the slots are brought back in line
 * with what TryGetDigit reports, so they only ever show digits the player has actually discovered, never the code itself.
 * The HUD itself never looks at the game, so swapping where a value comes from only changes this script.
 */
[RequireComponent(typeof(PlayerHud))]
public class PlayerHudBinder : MonoBehaviour
{
    private PlayerHud hud;
    private PlayerStats player;
    private PlayerWeapon weapon;
    private ObjectiveState objectives;

    private void Awake()
    {
        hud = GetComponent<PlayerHud>();
    }

    // Start runs after every Awake, so the player and the objectives exist by now, and the HUD has been built.
    private void Start()
    {
        player = FindAnyObjectByType<PlayerStats>();
        if (player != null)
        {
            player.HealthChanged += OnHealthChanged;
            player.SanityChanged += OnSanityChanged;
            player.AmmoChanged += OnAmmoChanged;
            player.ExitKeyChanged += OnExitKeyChanged;

            OnHealthChanged(player.Health, player.MaxHealth);
            OnSanityChanged(player.Sanity, player.MaxSanity);
            OnAmmoChanged(player.ReserveAmmo);
            OnExitKeyChanged(player.HasExitKey);
        }

        weapon = FindAnyObjectByType<PlayerWeapon>();
        if (weapon != null)
        {
            weapon.MagazineChanged += OnMagazineChanged;
            OnMagazineChanged(weapon.Magazine, weapon.MagazineSize);
        }

        objectives = FindAnyObjectByType<ObjectiveState>();
        if (objectives != null)
        {
            objectives.DigitFound += OnDigitFound;
            objectives.StateChanged += SyncDigits;
        }

        hud.ClearDigits();
        SyncDigits();
    }

    // Stops listening, so a destroyed HUD is never called by the player, the weapon or the objectives.
    private void OnDestroy()
    {
        if (player != null)
        {
            player.HealthChanged -= OnHealthChanged;
            player.SanityChanged -= OnSanityChanged;
            player.AmmoChanged -= OnAmmoChanged;
            player.ExitKeyChanged -= OnExitKeyChanged;
        }

        if (weapon != null) weapon.MagazineChanged -= OnMagazineChanged;
        if (objectives != null)
        {
            objectives.DigitFound -= OnDigitFound;
            objectives.StateChanged -= SyncDigits;
        }
    }

    private void OnHealthChanged(float current, float max) => hud.SetHealth(current, max);
    private void OnSanityChanged(float current, float max) => hud.SetSanity(current, max);
    private void OnAmmoChanged(int reserve) => hud.SetAmmo(reserve, player.MaxReserveAmmo);
    private void OnMagazineChanged(int loaded, int size) => hud.SetMagazine(loaded, size);
    private void OnExitKeyChanged(bool held) => hud.SetKey(held);

    // A newly discovered digit: shown in its slot (which flashes), and the key outline grows to match.
    private void OnDigitFound(int positionInCode, int digit)
    {
        hud.SetDigit(positionInCode, digit);
        SyncDigits();
    }

    /*
     * Shows exactly the digits ObjectiveState reports as discovered (slots already showing the same digit don't flash
     * again; a run with none discovered, such as after a reset, empties them) and draws the key outline by their share.
     */
    private void SyncDigits()
    {
        if (objectives == null) return;

        int found = 0;
        for (int position = 1; position <= 4; position++)
        {
            if (!objectives.TryGetDigit(position, out int digit)) continue;
            hud.SetDigit(position, digit);
            found++;
        }

        if (found == 0) hud.ClearDigits();
        hud.SetKeyProgress(found / 4f);
    }
}
