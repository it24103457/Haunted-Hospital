using System;

/// <summary>
/// Read-only view of how far the player is through the game's objectives.
/// The Environment Director reads this; it never changes it.
/// Read a fresh snapshot of the properties each time StateChanged fires.
/// </summary>
public interface IObjectiveState
{
    /// <summary>Setup is finished, so the values below are real and not defaults.</summary>
    bool IsReady { get; }

    /// <summary>Distinct clue papers collected, 0 to 4. Counts papers, not digit values.</summary>
    int CollectedClueCount { get; }

    /// <summary>True from the first correct code submission for the rest of the run.</summary>
    bool IsCodeAccepted { get; }

    /// <summary>True the whole time the number-entry screen is open.</summary>
    bool IsKeypadOpen { get; }

    /// <summary>True from the final key-switch being used until the game is won.</summary>
    bool IsEscapeInProgress { get; }

    /// <summary>True once the final key use has completed the game.</summary>
    bool HasWon { get; }

    /// <summary>True while the game is paused.</summary>
    bool IsPaused { get; }

    /// <summary>Raised after any value above changes and the new values are readable.</summary>
    event Action StateChanged;
}
