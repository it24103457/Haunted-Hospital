using UnityEngine;

/*
 * Test-only stand-in for barricade breaking, so a barricade can be broken instantly while testing the Thief.
 * Makes a barricade usable with E in the Thief test scene: breaking it deactivates the whole barricade,
 * which removes its collider and its NavMeshObstacle, so WaypointNode.IsBarricadeIntact reports it as down.
 * Added at runtime by ThiefTestProximityInteractor, so no barricade in any scene or prefab is changed.
 */
public class ThiefTestBreakableBarricade : MonoBehaviour, IInteractable
{
    // The prompt shown when the player looks at the barricade.
    public string GetPrompt() => "Press E to break";

    // Breaks the barricade by switching it off, so the Thief finds it down the next time it rechecks.
    public void Interact(PlayerInteractor interactor)
    {
        gameObject.SetActive(false);
    }
}
