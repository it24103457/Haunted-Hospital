using UnityEngine;

/*
 * Test-only stand-in for barricade breaking, which the game doesn't have yet.
 * Makes a barricade usable with E in the Thief test scene: breaking it deactivates the whole barricade,
 * which removes its collider and its NavMeshObstacle, so WaypointNode.IsBarricadeIntact reports it as down.
 * Added at runtime by ThiefTestProximityInteractor, so no barricade in any scene or prefab is changed.
 */
public class ThiefTestBreakableBarricade : MonoBehaviour, IInteractable
{
    public string GetPrompt() => "Press E to break";

    public void Interact(PlayerInteractor interactor)
    {
        gameObject.SetActive(false);
    }
}
