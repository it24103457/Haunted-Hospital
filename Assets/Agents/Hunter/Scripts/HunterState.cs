/*
 * What the Sound Hunter is doing right now. Published through HunterBrain.State so the mover,
 * animation, the debug view and the Director's adapter can all read it.
 *
 * Only HunterBrain changes it. Not every state is entered yet: the build order adds them one at a time
 * (Patrol first, then Investigate and Search, then Chase and Attack).
 */
public enum HunterState
{
    Inactive,     // not in the level yet (before it spawns)
    Patrol,       // walking between points, listening
    Investigate,  // walking to the best noise it heard
    Search,       // looking around the place it just arrived at
    Chase,        // after the player, because of a loud player noise or being very close by path
    Attack,       // close enough to hurt the player
    Dead          // health reached 0
}
