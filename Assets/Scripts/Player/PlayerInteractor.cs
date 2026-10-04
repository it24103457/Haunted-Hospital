using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Casts a ray from the camera each frame. If it hits an IInteractable within range,
/// shows its prompt and calls Interact() when the player presses E.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactRange = 2.5f;
    [Tooltip("Layers the interaction ray can hit. Leave on Everything unless you add a Player layer.")]
    [SerializeField] private LayerMask interactMask = ~0;

    private InputAction interactAction;
    private IInteractable current;
    private GUIStyle promptStyle;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonWest");
    }

    private void OnEnable() => interactAction.Enable();
    private void OnDisable() => interactAction.Disable();
    private void OnDestroy() => interactAction.Dispose();

    private void Update()
    {
        current = FindInteractable();

        if (current != null && interactAction.WasPressedThisFrame())
            current.Interact(this);
    }

    private IInteractable FindInteractable()
    {
        Transform cam = playerCamera.transform;

        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, interactRange,
                            interactMask, QueryTriggerInteraction.Ignore))
        {
            // GetComponentInParent so a child collider (e.g. a door's mesh) finds the script on its parent.
            return hit.collider.GetComponentInParent<IInteractable>();
        }

        return null;
    }

    // Simple IMGUI crosshair + prompt. Swap for a Canvas/TextMeshPro UI later.
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

        GUI.DrawTexture(new Rect(cx - 2f, cy - 2f, 4f, 4f), Texture2D.whiteTexture);

        if (current != null)
            GUI.Label(new Rect(cx - 200f, cy + 20f, 400f, 40f), current.GetPrompt(), promptStyle);
    }
}