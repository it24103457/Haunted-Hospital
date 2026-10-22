using System;


// Read-only view of how far the player is through the game's objectives.
// The Environment Director and the HUD read this; they never change it.
// Read a fresh snapshot of the properties each time StateChanged fires.

public interface IObjectiveState
{
    // Setup is finished, so the values below are real and not defaults.
    bool IsReady { get; }

    // Distinct clue papers collected, 0 to 4. Counts papers, not digit values.
    int CollectedClueCount { get; }

    // True from the first correct code submission for the rest of the run.
    bool IsCodeAccepted { get; }

    // True the whole time the number-entry screen is open.
    bool IsKeypadOpen { get; }

    // True from the final key-switch being used until the game is won.
    bool IsEscapeInProgress { get; }

    // True once the final key use has completed the game.
    bool HasWon { get; }

    // True while the game is paused.
    bool IsPaused { get; }

  
    ///The generated four-character code for this run ("0427", leading zeroes are valid), or an empty string
    // before a spawner has set one. Do NOT show this to the player: it contains undiscovered digits.
    // Use TryGetDigit for anything the player can see.
 
    string Code { get; }


    /// Gets the digit for a code position (1 to 4), but only if the player has already discovered it.
    /// Returns false for undiscovered positions and for positions outside 1 to 4.

    bool TryGetDigit(int positionInCode, out int digit);

    // Raised when a code position is discovered for the first time: (positionInCode 1 to 4, digit 0 to 9).
    event Action<int, int> DigitFound;

    // Raised after any value above changes and the new values are readable.
    event Action StateChanged;
}
