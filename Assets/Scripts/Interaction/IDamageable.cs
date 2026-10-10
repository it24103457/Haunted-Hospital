using UnityEngine;


// Everything that can be hurt (the player, barricade planks, ...) implements IDamageable.
// Callers (Hunter, Thief, PlayerWeapon) call TakeDamage and never reference the concrete class.

public interface IDamageable
{
    void TakeDamage(DamageInfo info);
}

// Data passed with every hit. It is a struct so new fields (damage type, knockback, ...) can be added later

public struct DamageInfo
{
    public float Amount;

    // Who caused it (Hunter, player weapon, ...). May be null.
    public GameObject Source;

    // World position of the hit. Useful for hit effects and noise.
    public Vector3 HitPoint;

    public DamageInfo(float amount, GameObject source = null, Vector3 hitPoint = default)
    {
        Amount = amount;
        Source = source;
        HitPoint = hitPoint;
    }
}
