using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/*
 * Test-only interaction for the top-down Thief test scene, where the player's crosshair can't be aimed.
 * Picks the closest IInteractable (door, barricade, pickup) within range of the player instead of the one
 * under the crosshair, shows its prompt at the top of the screen and calls Interact() when E is pressed.
 * The normal PlayerInteractor is switched off while this component is active, so one E press never
 * interacts twice; it is passed along to Interact() so pickups still reach the player's stats.
 */
[RequireComponent(typeof(PlayerInteractor))]
public class ThiefTestProximityInteractor : MonoBehaviour
{
    [Tooltip("Distance from the player's chest to an object's collider within which it can be used.")]
    [SerializeField] private float interactRadius = 2f;
    [Tooltip("Layers searched for interactables.")]
    [SerializeField] private LayerMask interactMask = ~0;

    private PlayerInteractor playerInteractor;
    private InputAction interactAction;
    private IInteractable current;
    private GUIStyle promptStyle;

    // Sets up its own E key binding and makes every barricade breakable before play starts.
    private void Awake()
    {
        playerInteractor = GetComponent<PlayerInteractor>();
        interactAction = new InputAction("TestInteract", InputActionType.Button, "<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonWest");

        MakeBarricadesBreakable();
    }

    // Every NavMeshObstacle that isn't part of a door is a barricade, and gets the test break interaction.
    private static void MakeBarricadesBreakable()
    {
        foreach (NavMeshObstacle obstacle in FindObjectsByType<NavMeshObstacle>(FindObjectsInactive.Exclude))
        {
            if (obstacle.GetComponentInParent<Door>() != null) continue;
            if (obstacle.GetComponent<ThiefTestBreakableBarricade>() != null) continue;

            obstacle.gameObject.AddComponent<ThiefTestBreakableBarricade>();
        }
    }

    // Takes over from the normal PlayerInteractor, so one E press never interacts twice.
    private void OnEnable()
    {
        playerInteractor.enabled = false;
        interactAction.Enable();
    }

    // Hands interaction back to the normal PlayerInteractor.
    private void OnDisable()
    {
        interactAction.Disable();
        if (playerInteractor != null) playerInteractor.enabled = true;
    }

    // Frees the input action, because actions created in code are not cleaned up automatically.
    private void OnDestroy() => interactAction.Dispose();

    // Picks the closest interactable each frame and uses it when E is pressed.
    private void Update()
    {
        current = FindClosest();

        if (current != null && interactAction.WasPressedThisFrame())
            current.Interact(playerInteractor);
    }

    // The interactable whose collider is nearest to the player's chest, ignoring ones with nothing to show.
    private IInteractable FindClosest()
    {
        Vector3 origin = transform.position + Vector3.up;
        Collider[] hits = Physics.OverlapSphere(origin, interactRadius, interactMask, QueryTriggerInteraction.Ignore);

        IInteractable best = null;
        float bestDistance = float.MaxValue;

        foreach (Collider c in hits)
        {
            IInteractable candidate = c.GetComponentInParent<IInteractable>();
            if (candidate == null || string.IsNullOrEmpty(candidate.GetPrompt())) continue;

            float distance = (c.bounds.ClosestPoint(origin) - origin).sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            best = candidate;
        }

        return best;
    }

    // Large prompt at the top centre of the screen, readable from the top-down camera.
    private void OnGUI()
    {
        if (current == null) return;

        if (promptStyle == null) promptStyle = new GUIStyle();

        // Set on every draw, so other test overlays that style their own text can't change the prompt's look.
        promptStyle.alignment = TextAnchor.MiddleCenter;
        promptStyle.fontSize = 34;
        promptStyle.fontStyle = FontStyle.Bold;
        promptStyle.normal.textColor = Color.yellow;

        // The object's name is shown with its prompt, so it's clear which of two nearby doors will react.
        string label = current is Component component ? $"{component.name}: {current.GetPrompt()}" : current.GetPrompt();
        GUI.Label(new Rect(0f, 20f, Screen.width, 60f), label, promptStyle);
    }
}
