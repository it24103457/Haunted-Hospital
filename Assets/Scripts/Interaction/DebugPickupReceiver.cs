using UnityEngine;


// TEMPORARY test stand-in for the team's real player-state script (health / ammo / sanity / key).

public class DebugPickupReceiver : MonoBehaviour, IPickupReceiver
{
    [Header("Current values (visible in the Inspector while playing)")]
    [SerializeField] private int health = 50;
    [SerializeField] private int ammo = 0;
    [SerializeField] private int sanity = 50;
    [SerializeField] private bool hasExitKey;

    [Header("Limits")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int maxSanity = 100;

    public bool CanReceive(PickupType type, int amount)
    {
        switch (type)
        {
            case PickupType.Health: return health < maxHealth;
            case PickupType.Sanity: return sanity < maxSanity;
            case PickupType.ExitKey: return !hasExitKey;
            default: return true; // ammo
        }
    }

    public void Receive(PickupType type, int amount)
    {
        switch (type)
        {
            case PickupType.Health: health = Mathf.Min(maxHealth, health + amount); break;
            case PickupType.Ammo: ammo += amount; break;
            case PickupType.Sanity: sanity = Mathf.Min(maxSanity, sanity + amount); break;
            case PickupType.ExitKey: hasExitKey = true; break;
        }

        Debug.Log($"Picked up {type} ({amount}). Health {health}, Ammo {ammo}, Sanity {sanity}, ExitKey {hasExitKey}");
    }
}
