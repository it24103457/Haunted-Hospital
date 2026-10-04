/// <summary>
/// Implement this on any MonoBehaviour the player can interact with
/// (doors, barricades, pickups, switches...). PlayerInteractor finds it by raycast.
/// </summary>
public interface IInteractable
{
    /// <summary>Text shown on screen while the player looks at this object.</summary>
    string GetPrompt();

    /// <summary>Called when the player presses the interact key while looking at this object.</summary>
    void Interact(PlayerInteractor interactor);
}