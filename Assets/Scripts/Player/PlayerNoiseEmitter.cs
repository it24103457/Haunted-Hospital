using UnityEngine;

/// <summary>
/// Makes the player's actions audible to the AI. For now: every shot is a gunshot noise.
/// Lives next to PlayerWeapon and listens to its Fired event, so the weapon never needs to know
/// the noise system exists.
/// </summary>
[RequireComponent(typeof(PlayerWeapon))]
public class PlayerNoiseEmitter : MonoBehaviour
{
    [SerializeField, Min(0f)] private float gunshotLoudness = NoiseLevels.Gunshot;

    private PlayerWeapon weapon;

    private void Awake()
    {
        weapon = GetComponent<PlayerWeapon>();
    }

    private void OnEnable()
    {
        weapon.Fired += OnFired;
    }

    private void OnDisable()
    {
        weapon.Fired -= OnFired;
    }

    private void OnFired(Vector3 position)
    {
        Noise.Emit(position, gunshotLoudness, NoiseType.Gunshot, gameObject);
    }
}
