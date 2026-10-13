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

    private void OnEnable()
    {
        playerInteractor.enabled = false;
        interactAction.Enable();
    }

    private void OnDisable()
    {
        interactAction.Disable();
        if (playerInteractor != null) playerInteractor.enabled = true;
    }

    private void OnDestroy() => interactAction.Dispose();

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

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
            promptStyle.normal.textColor = Color.yellow;
        }

        // The object's name is shown with its prompt, so it's clear which of two nearby doors will react.
        string label = current is Component component ? $"{component.name}: {current.GetPrompt()}" : current.GetPrompt();
        GUI.Label(new Rect(0f, 20f, Screen.width, 50f), label, promptStyle);
    }
}
