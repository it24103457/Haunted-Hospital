using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person movement: walk, sprint, jump, hold-to-crouch, mouse look.
/// Uses CharacterController (stable on stairs/slopes, no physics jitter) and
/// pushes loose Rigidbodies (barricades, crates) when walked into.
/// Input actions are created in code, so no Input Actions asset is needed.
/// The capsule is wide when standing (camera stays clear of walls) and slim when
/// crouching (fits through narrow gaps such as the 0.9 m vents).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraRoot;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float jumpHeight = 1.1f;
    [SerializeField] private float gravity = -20f;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float maxPitch = 85f;

    [Header("Body")]
    [SerializeField] private float standRadius = 0.5f;
    [SerializeField] private float crouchRadius = 0.3f;

    [Header("Crouch")]
    [SerializeField] private float standHeight = 1.8f;
    [SerializeField] private float crouchHeight = 0.9f;
    [SerializeField] private float standCameraY = 1.6f;
    [SerializeField] private float crouchCameraY = 0.7f;
    [SerializeField] private float crouchSmoothing = 12f;

    [Header("Pushing Rigidbodies")]
    [Tooltip("Higher = objects are pushed faster. Heavy objects (high mass) move slower.")]
    [SerializeField] private float pushStrength = 3f;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;

    private float pitch;
    private float verticalVelocity;
    private bool isCrouching;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        lookAction = new InputAction("Look", InputActionType.Value, "<Mouse>/delta");
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        sprintAction = new InputAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
        crouchAction = new InputAction("Crouch", InputActionType.Button, "<Keyboard>/c");

        // Pivot at the feet: controller center sits half the height above the transform.
        controller.radius = standRadius;
        controller.height = standHeight;
        controller.center = new Vector3(0f, standHeight * 0.5f, 0f);
    }

    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
        crouchAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.Disable();
        sprintAction.Disable();
        crouchAction.Disable();
    }

    private void OnDestroy()
    {
        moveAction.Dispose();
        lookAction.Dispose();
        jumpAction.Dispose();
        sprintAction.Dispose();
        crouchAction.Dispose();
    }

    private void Start()
    {
        SetCursorLocked(true);
    }

    private void Update()
    {
        HandleCursor();
        HandleLook();
        HandleCrouch();
        HandleMovement();
    }

    private void HandleCursor()
    {
        // Click the game view to grab the mouse again after pressing Esc.
        if (Cursor.lockState != CursorLockMode.Locked &&
            Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SetCursorLocked(true);
        }
    }

    private void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 delta = lookAction.ReadValue<Vector2>() * lookSensitivity;

        transform.Rotate(0f, delta.x, 0f);

        pitch = Mathf.Clamp(pitch - delta.y, -maxPitch, maxPitch);
        cameraRoot.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    private void HandleCrouch()
    {
        bool wantsCrouch = crouchAction.IsPressed();

        // Don't stand up if something is above or beside the player's standing capsule.
        if (!wantsCrouch && isCrouching && !CanStandUp())
            wantsCrouch = true;

        isCrouching = wantsCrouch;

        float t = crouchSmoothing * Time.deltaTime;
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        float targetRadius = isCrouching ? crouchRadius : standRadius;

        controller.height = Mathf.Lerp(controller.height, targetHeight, t);
        controller.radius = Mathf.Lerp(controller.radius, targetRadius, t);
        controller.center = new Vector3(0f, controller.height * 0.5f, 0f);

        float targetCamY = isCrouching ? crouchCameraY : standCameraY;
        Vector3 camPos = cameraRoot.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, targetCamY, t);
        cameraRoot.localPosition = camPos;
    }

    // True if the full standing capsule would fit. Ignores the player's own collider.
    private bool CanStandUp()
    {
        float r = standRadius * 0.95f;
        Vector3 bottom = transform.position + Vector3.up * (standRadius + 0.05f);
        Vector3 top = transform.position + Vector3.up * (standHeight - standRadius);

        Collider[] hits = Physics.OverlapCapsule(bottom, top, r, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit != controller) return false;
        }
        return true;
    }

    private void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 direction = transform.right * input.x + transform.forward * input.y;
        direction = Vector3.ClampMagnitude(direction, 1f);

        float speed = isCrouching ? crouchSpeed
                    : sprintAction.IsPressed() ? sprintSpeed
                    : walkSpeed;

        bool grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // small downward push keeps the controller glued to the ground

        if (grounded && !isCrouching && jumpAction.WasPressedThisFrame())
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = direction * speed + Vector3.up * verticalVelocity;
        controller.Move(motion * Time.deltaTime);
    }

    // CharacterController doesn't push rigidbodies by itself; do it manually.
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) return;
        if (hit.moveDirection.y < -0.3f) return; // standing on top of it, don't push

        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        float strength = pushStrength / Mathf.Max(body.mass, 1f);

        Vector3 v = body.linearVelocity;
        body.linearVelocity = new Vector3(pushDir.x * strength, v.y, pushDir.z * strength);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}