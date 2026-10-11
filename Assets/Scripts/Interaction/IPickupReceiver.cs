// What a pickup gives the player.
public enum PickupType
{
    Health,
    Ammo,
    Sanity,
    ExitKey
}

// Until the team's real player-state script exists, use DebugPickupReceiver for testing.
public interface IPickupReceiver
{
    // Return false if the item is useless right now (e.g. health already full, key already held).
    // The pickup then stays in the world and is NOT consumed.
    bool CanReceive(PickupType type, int amount);

    // Apply the item (add health, add ammo, add sanity, set hasExitKey...).
    void Receive(PickupType type, int amount);
}
