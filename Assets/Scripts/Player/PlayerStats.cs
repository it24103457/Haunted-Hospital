using System;
using UnityEngine;


// The player's shared state: health, sanity, reserve ammo and the exit key.
// Other scripts should ONLY the public methods, properties and events below, never fields:
//  - Hunter / Thief / weapons hurt the player through IDamageable.TakeDamage(DamageInfo).
//  - Pickups deliver items through IPickupReceiver.
//  - UI and effects listen to the events.
// What happens on death (respawn, game over screen) is NOT handled here; listen to the Died event.

public class PlayerStats : MonoBehaviour, IPickupReceiver, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(1f)] private float startingHealth = 100f;

    [Header("Sanity")]
    [SerializeField, Min(1f)] private float maxSanity = 100f;
    [SerializeField, Min(0f)] private float startingSanity = 100f;

    [Header("Reserve ammo (rounds carried; the weapon manages its own magazine)")]
    [SerializeField, Min(0)] private int startingReserveAmmo = 40;
    [SerializeField, Min(0)] private int maxReserveAmmo = 120;

    [Header("Debug")]
    [SerializeField] private bool logChanges = false;

    // Read-only state

    public float Health { get; private set; }
    public float MaxHealth => maxHealth;
    public float Sanity { get; private set; }
    public float MaxSanity => maxSanity;
    public int ReserveAmmo { get; private set; }
    public bool HasExitKey { get; private set; }
    public bool IsDead { get; private set; }

    // Events

    // (current, max)
    public event Action<float, float> HealthChanged;

    // (current, max)
    public event Action<float, float> SanityChanged;

    // (current reserve rounds)
    public event Action<int> AmmoChanged;

    public event Action<bool> ExitKeyChanged;

    // Raised for every hit that actually hurts the player.
    public event Action<DamageInfo> Damaged;

    public event Action Died;

    private void Awake()
    {
        Health = Mathf.Min(startingHealth, maxHealth);
        Sanity = Mathf.Min(startingSanity, maxSanity);
        ReserveAmmo = Mathf.Min(startingReserveAmmo, maxReserveAmmo);
        HasExitKey = false;
        IsDead = false;
    }

    private void Start()
    {
        // Let UI that subscribed in Awake/OnEnable show the starting values.
        HealthChanged?.Invoke(Health, maxHealth);
        SanityChanged?.Invoke(Sanity, maxSanity);
        AmmoChanged?.Invoke(ReserveAmmo);
        ExitKeyChanged?.Invoke(HasExitKey);
    }

    private void OnValidate()
    {
        startingHealth = Mathf.Min(startingHealth, maxHealth);
        startingSanity = Mathf.Min(startingSanity, maxSanity);
        startingReserveAmmo = Mathf.Min(startingReserveAmmo, maxReserveAmmo);
    }

    // IDamageable

    public void TakeDamage(DamageInfo info)
    {
        if (IsDead || info.Amount <= 0f) return;

        Health = Mathf.Max(0f, Health - info.Amount);
        Log($"Took {info.Amount} damage from {(info.Source != null ? info.Source.name : "unknown")}. Health {Health}/{maxHealth}");

        Damaged?.Invoke(info);
        HealthChanged?.Invoke(Health, maxHealth);

        if (Health <= 0f)
        {
            IsDead = true;
            Log("Player died.");
            Died?.Invoke();
        }
    }

    // Public API

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        Health = Mathf.Min(maxHealth, Health + amount);
        HealthChanged?.Invoke(Health, maxHealth);
        Log($"Healed {amount}. Health {Health}/{maxHealth}");
    }

    public void AddSanity(float amount)
    {
        if (amount <= 0f) return;
        Sanity = Mathf.Min(maxSanity, Sanity + amount);
        SanityChanged?.Invoke(Sanity, maxSanity);
        Log($"Sanity +{amount}. Sanity {Sanity}/{maxSanity}");
    }

    public void DrainSanity(float amount)
    {
        if (amount <= 0f) return;
        Sanity = Mathf.Max(0f, Sanity - amount);
        SanityChanged?.Invoke(Sanity, maxSanity);
        Log($"Sanity -{amount}. Sanity {Sanity}/{maxSanity}");
    }

    public void AddAmmo(int rounds)
    {
        if (rounds <= 0) return;
        ReserveAmmo = Mathf.Min(maxReserveAmmo, ReserveAmmo + rounds);
        AmmoChanged?.Invoke(ReserveAmmo);
        Log($"Ammo +{rounds}. Reserve {ReserveAmmo}/{maxReserveAmmo}");
    }

    // Take rounds from the reserve (for example when reloading). Returns false if there are not enough.
    public bool TryUseAmmo(int rounds)
    {
        if (rounds <= 0 || ReserveAmmo < rounds) return false;
        ReserveAmmo -= rounds;
        AmmoChanged?.Invoke(ReserveAmmo);
        return true;
    }

    public void SetExitKey(bool value)
    {
        if (HasExitKey == value) return;
        HasExitKey = value;
        ExitKeyChanged?.Invoke(HasExitKey);
        Log($"Exit key: {HasExitKey}");
    }

    // IPickupReceiver

    public bool CanReceive(PickupType type, int amount)
    {
        switch (type)
        {
            case PickupType.Health: return !IsDead && Health < maxHealth;
            case PickupType.Sanity: return Sanity < maxSanity;
            case PickupType.Ammo: return ReserveAmmo < maxReserveAmmo;
            case PickupType.ExitKey: return !HasExitKey;
            default: return false;
        }
    }

    public void Receive(PickupType type, int amount)
    {
        switch (type)
        {
            case PickupType.Health: Heal(amount); break;
            case PickupType.Sanity: AddSanity(amount); break;
            case PickupType.Ammo: AddAmmo(amount); break;
            case PickupType.ExitKey: SetExitKey(true); break;
        }
    }

    // Testing helpers (right-click the component header in the Inspector)

    [ContextMenu("Debug: Take 30 damage")]
    private void DebugTakeDamage() => TakeDamage(new DamageInfo(30f, gameObject, transform.position));

    [ContextMenu("Debug: Drain 30 sanity")]
    private void DebugDrainSanity() => DrainSanity(30f);

    [ContextMenu("Debug: Use 10 ammo")]
    private void DebugUseAmmo() => TryUseAmmo(10);

    private void Log(string message)
    {
        if (logChanges) Debug.Log($"[PlayerStats] {message}", this);
    }
}
