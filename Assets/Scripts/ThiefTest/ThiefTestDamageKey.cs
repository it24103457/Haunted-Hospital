using UnityEngine;
using UnityEngine.InputSystem;

/*
 * Test-only stand-in for the player's weapon, used only on the Thief in the Thief test scene.
 * Pressing K deals damage to the Thief through IDamageable, the same way a shot would, so death,
 * dropping a carried item and respawning can be tried quickly without aiming and shooting.
 * Each hit and the remaining health are written to the Console.
 */
[RequireComponent(typeof(ThiefAgent))]
public class ThiefTestDamageKey : MonoBehaviour
{
    [Tooltip("Damage dealt per K press.")]
    [SerializeField] private float damagePerPress = 25f;

    private ThiefAgent thief;
    private IDamageable target;
    private GameObject source;

    // Finds the Thief to damage and the player, who is named as the source of each hit.
    private void Awake()
    {
        thief = GetComponent<ThiefAgent>();
        target = thief;

        PlayerStats player = FindAnyObjectByType<PlayerStats>();
        source = player != null ? player.gameObject : null;
    }

    // Deals one hit per K press and logs the Thief's health and state.
    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.kKey.wasPressedThisFrame) return;

        target.TakeDamage(new DamageInfo(damagePerPress, source, transform.position));
        Debug.Log($"[ThiefTest] K: {damagePerPress} damage to the Thief. Health {thief.Health}, state {thief.State}.", this);
    }
}
