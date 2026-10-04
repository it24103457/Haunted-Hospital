using UnityEngine;

// Casts a ray from the camera each frame and shows a crosshair plus the name of
// whatever the player is looking at within range.
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactRange = 2.5f;
    [Tooltip("Layers the interaction ray can hit. Leave on Everything unless you add a Player layer.")]
    [SerializeField] private LayerMask interactMask = ~0;

    private Collider currentTarget;
    private GUIStyle promptStyle;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        Transform cam = playerCamera.transform;

        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, interactRange,
                            interactMask, QueryTriggerInteraction.Ignore))
        {
            currentTarget = hit.collider;
        }
        else
        {
            currentTarget = null;
        }
    }

    // Simple IMGUI crosshair + target name. Swap for a Canvas/TextMeshPro UI later.
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

        if (currentTarget != null)
            GUI.Label(new Rect(cx - 200f, cy + 20f, 400f, 40f), currentTarget.name, promptStyle);
    }
}