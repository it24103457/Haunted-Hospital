using UnityEngine;

/*
 * Makes the player's footsteps audible to the AI: one noise per stride, louder the faster the player moves
 * (crouch, walk, sprint). It reads the CharacterController's speed, so PlayerController needs no change.
 * Put it on the Player, next to PlayerNoiseEmitter.
 */
[RequireComponent(typeof(CharacterController))]
public class PlayerFootstepNoise : MonoBehaviour
{
    [Header("Speed bands (metres per second)")]
    [Tooltip("Slower than this counts as standing still: no noise.")]
    [SerializeField, Min(0f)] private float minMoveSpeed = 0.5f;
    [Tooltip("Only used if no PlayerController is found: up to this speed counts as crouching (crouch speed is 2). Normally the real crouch state is used.")]
    [SerializeField, Min(0f)] private float crouchMaxSpeed = 3f;
    [Tooltip("Up to this speed counts as walking (walk speed is 4). Faster is sprinting (sprint speed is 6.5).")]
    [SerializeField, Min(0f)] private float walkMaxSpeed = 5.25f;

    [Header("Loudness")]
    [SerializeField, Min(0f)] private float crouchLoudness = NoiseLevels.CrouchStep;
    [SerializeField, Min(0f)] private float walkLoudness = NoiseLevels.WalkStep;
    [SerializeField, Min(0f)] private float sprintLoudness = NoiseLevels.SprintStep;

    [Header("Stride (metres walked between steps)")]
    [SerializeField, Min(0.1f)] private float crouchStride = 1f;
    [SerializeField, Min(0.1f)] private float walkStride = 1.6f;
    [SerializeField, Min(0.1f)] private float sprintStride = 2.2f;

    private CharacterController controller;
    private PlayerController playerController;
    private float walked;
    private bool warnedNoController;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();

        if (playerController == null && !warnedNoController)
        {
            Debug.LogWarning("PlayerFootstepNoise: no PlayerController on this object, so crouching is guessed from speed.", this);
            warnedNoController = true;
        }
    }

    private void Update()
    {
        Vector3 velocity = controller.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        // Standing still, or in the air: no footsteps, and the next step starts a fresh stride.
        if (speed < minMoveSpeed || !controller.isGrounded)
        {
            walked = 0f;
            return;
        }

        float stride, loudness;
        // The real crouch state decides crouching. Speed only separates walking from sprinting,
        // and stands in for the crouch state only when there is no PlayerController.
        bool crouching = playerController != null ? playerController.IsCrouching : speed <= crouchMaxSpeed;

        if (crouching) { stride = crouchStride; loudness = crouchLoudness; }
        else if (speed <= walkMaxSpeed) { stride = walkStride; loudness = walkLoudness; }
        else { stride = sprintStride; loudness = sprintLoudness; }

        walked += speed * Time.deltaTime;
        if (walked < stride) return;

        walked = 0f;
        Noise.Emit(transform.position, loudness, NoiseType.Footstep, gameObject);
    }
}
