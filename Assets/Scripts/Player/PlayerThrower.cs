using UnityEngine;
using UnityEngine.InputSystem;

// Lets the player carry one ThrowableItem.
//   E          pick up (done by the item itself: PlayerInteractor finds ThrowableItem as an IInteractable)
//   Right mouse throw the held item
//   G          drop the held item gently (quiet, no damage)
// Put this on the same object as PlayerInteractor (the Player).
// Left mouse stays the gun's fire button and is not touched by this script, so the gun still works while
// an item is held.
public class PlayerThrower : MonoBehaviour
{
    [Header("References (found automatically if left empty)")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Where a held item sits. Created under the camera if left empty.")]
    [SerializeField] private Transform holdAnchor;

    [Header("Throw")]
    [SerializeField, Min(1f)] private float throwSpeed = 15f;
    [Tooltip("Aims the throw slightly upward so it arcs instead of dropping at once.")]
    [SerializeField, Range(0f, 0.5f)] private float upwardBias = 0.12f;
    [SerializeField] private string throwBinding = "<Mouse>/rightButton";
    [SerializeField] private string dropBinding = "<Keyboard>/g";

    [Header("Hold position (relative to the camera)")]
    [Tooltip("Lower Y pushes the item further down so only part of it shows, like the gun. " +
             "At the default 60 degree FOV the bottom screen edge is about -0.29 at Z 0.5, so about -0.30 puts the item centre just past the edge and shows about half.")]
    [SerializeField] private Vector3 holdOffset = new Vector3(-0.26f, -0.28f, 0.50f);

    [Header("Hold animation (same feel as the gun)")]
    [Tooltip("Used to read walking speed. Found automatically if left empty.")]
    [SerializeField] private CharacterController playerController;
    [SerializeField] private float bobAmplitude = 0.018f;
    [SerializeField] private float bobFrequency = 8f;
    [SerializeField] private float swayAmount = 0.025f;
    [SerializeField] private float swaySmooth = 8f;

    private InputAction throwAction;
    private InputAction dropAction;
    private ThrowableItem held;
    private GUIStyle hudStyle;
    private Vector3 anchorBasePosition;
    private bool anchorWasCreatedHere;
    private float bobTimer;

    public bool IsHolding => held != null;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerController == null) playerController = GetComponentInParent<CharacterController>();
        if (playerController == null) playerController = GetComponentInChildren<CharacterController>();

        if (holdAnchor == null && playerCamera != null)
        {
            var anchor = new GameObject("HeldItemAnchor");
            anchor.transform.SetParent(playerCamera.transform, false);
            anchor.transform.localPosition = holdOffset;
            holdAnchor = anchor.transform;
            anchorWasCreatedHere = true;
        }

        // The animation moves the anchor around this resting position.
        if (holdAnchor != null) anchorBasePosition = holdAnchor.localPosition;

        throwAction = new InputAction("Throw", InputActionType.Button, throwBinding);
        dropAction = new InputAction("Drop", InputActionType.Button, dropBinding);
    }

    private void OnEnable()
    {
        if (throwAction != null) throwAction.Enable();
        if (dropAction != null) dropAction.Enable();
    }

    private void OnDisable()
    {
        if (throwAction != null) throwAction.Disable();
        if (dropAction != null) dropAction.Disable();
    }

    private void OnDestroy()
    {
        if (throwAction != null) throwAction.Dispose();
        if (dropAction != null) dropAction.Dispose();
    }

    private void Update()
    {
        if (held == null || throwAction == null) return;

        // Ignore the keys while a menu is open or the game is paused.
        if (Cursor.lockState != CursorLockMode.Locked || Time.timeScale <= 0f) return;

        if (throwAction.WasPressedThisFrame())
            ReleaseHeld(throwing: true);
        else if (dropAction.WasPressedThisFrame())
            ReleaseHeld(throwing: false);
    }

    // Walking bob and sideways sway for the held item, using the same maths as WeaponViewAnimation so the item
    // moves together with the gun. The item is a child of the anchor, so moving the anchor moves the item.
    private void LateUpdate()
    {
        if (holdAnchor == null) return;

        // An anchor made by this script follows holdOffset live, so it can be tuned in the Inspector during play.
        Vector3 target = anchorWasCreatedHere ? holdOffset : anchorBasePosition;

        if (held != null && playerController != null)
        {
            Vector3 localVelocity = transform.InverseTransformDirection(playerController.velocity);
            float movementAmount = Mathf.Clamp01(new Vector2(localVelocity.x, localVelocity.z).magnitude / 3f);

            float bobY = 0f;
            if (movementAmount > 0.05f)
            {
                bobTimer += Time.deltaTime * bobFrequency;
                bobY = Mathf.Sin(bobTimer) * bobAmplitude * movementAmount;
            }
            else
            {
                bobTimer = 0f;
            }

            float swayX = -localVelocity.x * swayAmount * movementAmount;
            target += new Vector3(swayX, bobY, 0f);
        }

        holdAnchor.localPosition = Vector3.Lerp(holdAnchor.localPosition, target, swaySmooth * Time.deltaTime);
    }

    // Called by ThrowableItem when the player interacts with it. False if the hands are full.
    public bool TryPickUp(ThrowableItem item)
    {
        if (item == null || held != null || holdAnchor == null) return false;

        held = item;
        item.Hold(holdAnchor);
        return true;
    }

    private void ReleaseHeld(bool throwing)
    {
        if (held == null || playerCamera == null || holdAnchor == null) return;

        Transform cam = playerCamera.transform;

        // If the player is nose-to-wall, the hold point is inside the wall. Start just in front of it instead,
        // or the physics engine would eject the item violently.
        Vector3 start = holdAnchor.position;
        Vector3 toAnchor = start - cam.position;
        float distance = toAnchor.magnitude;
        if (distance > 0.001f &&
            Physics.Raycast(cam.position, toAnchor / distance, out RaycastHit hit, distance,
                            ~0, QueryTriggerInteraction.Ignore))
        {
            start = hit.point - (toAnchor / distance) * 0.15f;
        }

        Vector3 velocity;
        if (throwing)
        {
            Vector3 direction = (cam.forward + Vector3.up * upwardBias).normalized;
            velocity = direction * throwSpeed;
        }
        else
        {
            // A drop: a small nudge forward so it does not land on the player's feet.
            Vector3 flatForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            velocity = flatForward * 1.2f;
        }

        ThrowableItem item = held;
        held = null;
        item.Release(start, velocity, dropped: !throwing);
    }

    // Stops the item colliding with the player's own colliders, so a throw cannot hit the thrower and a
    // resting item never blocks or gets pushed by the player. Raycasts (interact, weapon) are not affected.
    public void IgnorePlayerCollisions(ThrowableItem item)
    {
        if (item == null) return;

        PlayerStats stats = GetComponentInParent<PlayerStats>();
        Transform root = stats != null ? stats.transform : transform;
        Collider[] playerColliders = root.GetComponentsInChildren<Collider>(true);
        Collider[] itemColliders = item.Colliders;
        if (itemColliders == null) return;

        foreach (Collider playerCollider in playerColliders)
        {
            // The held item is parented under the player's camera; do not pair it with itself.
            if (playerCollider == null || playerCollider.GetComponentInParent<ThrowableItem>() != null) continue;

            foreach (Collider itemCollider in itemColliders)
            {
                if (itemCollider != null)
                    Physics.IgnoreCollision(itemCollider, playerCollider, true);
            }
        }
    }

    private void OnGUI()
    {
        if (held == null) return;

        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.LowerRight,
                fontSize = 16,
                wordWrap = false
            };
            hudStyle.normal.textColor = Color.white;
        }

        // Bottom-right corner, two lines, so it stays clear of the middle of the screen.
        const float width = 360f;
        const float height = 52f;
        const float margin = 20f;
        GUI.Label(new Rect(Screen.width - width - margin, Screen.height - height - margin, width, height),
                  $"Holding {held.DisplayName}\nRight click: throw   G: drop", hudStyle);
    }
}