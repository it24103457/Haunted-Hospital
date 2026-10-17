using UnityEngine;

/// <summary>
/// Freezes or restores the player's controls and the mouse cursor.
/// Used while a full-screen interaction is open (the keypad) and when the game is won.
/// It only switches existing player components on and off; it adds no new player logic.
/// </summary>
public static class PlayerControlLock
{
    public static void Set(bool locked)
    {
        PlayerStats player = Object.FindAnyObjectByType<PlayerStats>();

        if (player != null)
        {
            SetEnabled<PlayerController>(player, !locked);
            SetEnabled<PlayerInteractor>(player, !locked);
            SetEnabled<PlayerWeapon>(player, !locked);
        }

        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = locked;
    }

    private static void SetEnabled<T>(PlayerStats player, bool on) where T : Behaviour
    {
        T component = player.GetComponent<T>();
        if (component != null) component.enabled = on;
    }
}
