using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles camera-centred interaction and draws the existing IMGUI centre dot.
/// When the weapon's shot ray is aimed at an IDamageable target, green parentheses
/// appear around the dot. No Canvas or second crosshair is created.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField, Min(0.1f)] private float interactRange = 2.5f;
    [Tooltip("Layers the interaction ray can hit. Leave on Everything unless you add a Player layer.")]
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("Aim reticle")]
    [Tooltip("Colour of the parentheses when the weapon ray is aimed at an IDamageable target.")]
    [SerializeField] private Color shootableReticleColor = new Color(0.25f, 1f, 0.45f, 1f);
    [Tooltip("Font size for the target parentheses around the existing centre dot.")]
    [SerializeField, Min(10)] private int shootableReticleFontSize = 22;

    private InputAction interactAction;
    private IInteractable current;
    private PlayerWeapon playerWeapon;
    private bool aimingAtShootableTarget;
    private GUIStyle promptStyle;
    private GUIStyle reticleStyle;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (playerWeapon == null)
            playerWeapon = GetComponent<PlayerWeapon>();
        if (playerWeapon == null)
            playerWeapon = GetComponentInParent<PlayerWeapon>();

        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonWest");
    }

    private void OnEnable()
    {
        if (interactAction != null)
            interactAction.Enable();
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.Disable();
    }

    private void OnDestroy()
    {
        if (interactAction != null)
            interactAction.Dispose();
    }

    private void Update()
    {
        current = FindInteractable();

        // PlayerWeapon owns the firing range and hit mask, so the reticle follows
        // the same ray/target rules as the actual shot.
        aimingAtShootableTarget = playerWeapon != null && playerWeapon.IsAimingAtDamageable;

        if (current != null && interactAction != null && interactAction.WasPressedThisFrame())
            current.Interact(this);
    }

    private IInteractable FindInteractable()
    {
        if (playerCamera == null)
            return null;

        Transform cam = playerCamera.transform;
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, interactRange,
                            interactMask, QueryTriggerInteraction.Ignore))
        {
            // A child collider (e.g. a door mesh) can find IInteractable on its parent.
            return hit.collider.GetComponentInParent<IInteractable>();
        }

        return null;
    }

    private void OnGUI()
    {
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22
            };
            promptStyle.normal.textColor = Color.white;
        }

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        Color previousGuiColor = GUI.color;

        // Keep the original white centre dot at all times.
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(cx - 2f, cy - 2f, 4f, 4f), Texture2D.whiteTexture);

        // Show a target marker only when the weapon's first hit is damageable.
        // The parentheses surround (but do not replace) the existing centre dot.
        if (aimingAtShootableTarget)
        {
            if (reticleStyle == null)
            {
                reticleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = shootableReticleFontSize,
                    fontStyle = FontStyle.Bold,
                    wordWrap = false,
                    clipping = TextClipping.Clip
                };
            }
            reticleStyle.fontSize = shootableReticleFontSize;
            reticleStyle.normal.textColor = shootableReticleColor;

            GUI.Label(new Rect(cx - 16f, cy - 14f, 12f, 28f), "(", reticleStyle);
            GUI.Label(new Rect(cx + 4f, cy - 14f, 12f, 28f), ")", reticleStyle);
        }

        if (current != null)
            GUI.Label(new Rect(cx - 200f, cy + 20f, 400f, 40f), current.GetPrompt(), promptStyle);

        GUI.color = previousGuiColor;
    }
}
