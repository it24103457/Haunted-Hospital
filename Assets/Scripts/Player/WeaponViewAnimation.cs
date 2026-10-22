
using UnityEngine;

public class WeaponViewAnimation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerWeapon weapon;
    [SerializeField] private CharacterController playerController;

    [Header("Shooting Recoil")]
    [SerializeField] private float recoilDistance = 0.10f;
    [SerializeField] private float recoilAngle = 5f;
    [SerializeField] private float recoilRecoverySpeed = 8f;

    [Header("Reload Animation")]
    [SerializeField] private float reloadLowerAmount = 0.18f;
    [SerializeField] private float reloadSmoothSpeed = 8f;

    [Header("Movement Sway")]
    [SerializeField] private float swayAmount = 0.025f;
    [SerializeField] private float swaySmooth = 8f;

    [Header("Walking Bob")]
    [SerializeField] private float bobAmplitude = 0.018f;
    [SerializeField] private float bobFrequency = 8f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private float recoil;
    private float bobTimer;
    private bool reloading;

    private void Awake()
    {
        // Remember the gun's original local transform.
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;

        // Find the weapon component if it wasn't assigned.
        if (weapon == null)
            weapon = GetComponentInParent<PlayerWeapon>();

        // Find the player's movement controller.
        if (playerController == null)
            playerController = GetComponentInParent<CharacterController>();
    }

    private void OnEnable()
    {
        if (weapon == null)
            return;

        weapon.Fired += OnFired;
        weapon.ReloadStarted += OnReloadStarted;
        weapon.ReloadFinished += OnReloadFinished;
    }

    private void OnDisable()
    {
        if (weapon == null)
            return;

        weapon.Fired -= OnFired;
        weapon.ReloadStarted -= OnReloadStarted;
        weapon.ReloadFinished -= OnReloadFinished;
    }

    private void OnFired(Vector3 position)
    {
        // Trigger recoil whenever the weapon fires.
        recoil = 1f;
    }

    private void OnReloadStarted()
    {
        reloading = true;
    }

    private void OnReloadFinished()
    {
        reloading = false;
    }

    private void LateUpdate()
    {
        // Recover recoil smoothly.
        recoil = Mathf.MoveTowards(
            recoil,
            0f,
            recoilRecoverySpeed * Time.deltaTime
        );

        float movementAmount = 0f;
        Vector3 localVelocity = Vector3.zero;

        if (playerController != null)
        {
            // Convert player velocity into the gun parent's local space.
            if (transform.parent != null)
            {
                localVelocity =
                    transform.parent.InverseTransformDirection(
                        playerController.velocity
                    );
            }
            else
            {
                localVelocity = playerController.velocity;
            }

            movementAmount = Mathf.Clamp01(
                new Vector2(
                    localVelocity.x,
                    localVelocity.z
                ).magnitude / 3f
            );
        }

        // Walking bob: oscillate while moving.
        float bobY = 0f;

        if (movementAmount > 0.05f)
        {
            bobTimer += Time.deltaTime * bobFrequency;

            bobY = Mathf.Sin(bobTimer)
                * bobAmplitude
                * movementAmount;
        }
        else
        {
            // Return the bob smoothly toward zero.
            bobTimer = 0f;
        }

        // Horizontal sway responds to sideways movement.
        float swayX = -localVelocity.x
            * swayAmount
            * movementAmount;

        // Combine the visual effects without changing gameplay logic.
        Vector3 targetPosition =
            startPosition
            + Vector3.back * (recoilDistance * recoil)
            + Vector3.down * (reloading ? reloadLowerAmount : 0f)
            + new Vector3(swayX, bobY, 0f);

        // Tilt the weapon backward during recoil.
        Quaternion targetRotation =
            startRotation
            * Quaternion.Euler(-recoilAngle * recoil, 0f, 0f);

        // Smoothly apply the final local position.
        float positionSpeed = Mathf.Max(
            swaySmooth,
            reloadSmoothSpeed
        );

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            positionSpeed * Time.deltaTime
        );

        // Smoothly apply the final local rotation.
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            swaySmooth * Time.deltaTime
        );
    }
}
