using UnityEngine;

/*
 * Feeds the HUD from the game. Health, ammo and sanity come from PlayerStats' change events, and the starting values are
 * shown straight away. Ammo is the reserve the player carries against the most they can carry; the magazine comes from the
 * player's PlayerWeapon (rounds loaded against the magazine size), so it drops with each shot and refills on reload.
 * The key icon fills a quarter for each code digit found, and stays solid while PlayerStats says the player holds the
 * exit key (ExitKeyChanged, starting from whether the key is already held).
 * Code digits: whenever ObjectiveState reports a change (a clue paper being read is one), every paper that has been read
 * puts its digit into the slot for its position in the code. This only reads public members that already exist on the
 * papers and ObjectiveState, so it works without any changes to them.
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
        if (objectives != null) objectives.StateChanged += ShowReadClues;

        hud.ClearDigits();
        ShowReadClues();
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
        if (objectives != null) objectives.StateChanged -= ShowReadClues;
    }

    private void OnHealthChanged(float current, float max) => hud.SetHealth(current, max);
    private void OnSanityChanged(float current, float max) => hud.SetSanity(current, max);
    private void OnAmmoChanged(int reserve) => hud.SetAmmo(reserve, player.MaxReserveAmmo);
    private void OnMagazineChanged(int loaded, int size) => hud.SetMagazine(loaded, size);
    private void OnExitKeyChanged(bool held) => hud.SetKey(held);

    /*
     * Puts the digit of every clue paper the player has read into its slot (slots already showing that digit don't flash
     * again), and fills the key icon by the share of the four positions found.
     */
    private void ShowReadClues()
    {
        var found = new System.Collections.Generic.HashSet<int>();
        foreach (ClueInteractable clue in FindObjectsByType<ClueInteractable>(FindObjectsInactive.Exclude))
        {
            if (!clue.IsCollected) continue;
            hud.SetDigit(clue.PositionInCode, clue.Digit);
            found.Add(clue.PositionInCode);
        }
        hud.SetKeyProgress(found.Count / 4f);
    }
}
