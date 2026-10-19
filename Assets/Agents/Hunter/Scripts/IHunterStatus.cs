using System;

/*
 * What the rest of the game is allowed to know about the Hunter. Read-only on purpose: the Environment
 * Director's adapter (a DirectorHunterSignalSource) reads this and maps it onto Unavailable / Inactive / Active,
 * and it never changes the Hunter.
 */
public interface IHunterStatus
{
    // Setup is finished and every required reference was found, so the values below are real.
    bool IsReady { get; }

    // The Hunter is in play: spawned, alive and doing something. False before it spawns and after it dies.
    bool IsActive { get; }

    // The Hunter has health left. False once it is Dead.
    bool IsAlive { get; }

    // True while the Hunter is in its Chase state.
    bool IsChasingPlayer { get; }

    // True while the Hunter is in its Attack state (close enough to hurt the player).
    bool IsAttackingPlayer { get; }

    // Raised after any value above changes and the new values are readable.
    event Action StateChanged;
}
