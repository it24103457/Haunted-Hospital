using System;
using System.Collections.Generic;
using UnityEngine;


/// The one place that knows how far the player is through the objectives:
/// find four clue papers, enter the code on the keypad, collect the exit key, use the key switch.
///
/// Gameplay scripts TELL this component what happened (a clue paper calls ReadClue, the keypad screen
/// calls SetKeypadOpen and AcceptCode, the key switch calls BeginEscape or CompleteEscape, and the
/// objective spawner calls SetCode once at the start of a run).
/// Everything else, including the Environment Director and the HUD, only READS it through IObjectiveState.
///
/// Put one on an empty object in the scene. To test without the real clue, keypad and switch scripts,
/// right-click this component's header in the Inspector while the game is playing and use the Debug items.
/// Key possession is not tracked here: read PlayerStats.HasExitKey for that.
///
/// A fresh run is a scene reload. ResetRun() clears only this component, so it is not a full restart on its own.

public class ObjectiveState : MonoBehaviour, IObjectiveState
{
    private const int CodeLength = 4;

    [SerializeField, Min(1)] private int totalClues = 4;
    [SerializeField] private bool logChanges = false;

    private readonly HashSet<int> collectedClueIds = new HashSet<int>();

    // Digit found for each code position (index 0 = position 1), or -1 if not discovered yet.
    private readonly int[] discoveredDigits = { -1, -1, -1, -1 };

    private PlayerStats player;

    public bool IsReady { get; private set; }
    public int CollectedClueCount => collectedClueIds.Count;
    public int TotalClues => totalClues;
    public bool IsCodeAccepted { get; private set; }
    public bool IsKeypadOpen { get; private set; }
    public bool IsEscapeInProgress { get; private set; }
    public bool HasWon { get; private set; }

    // There is no pause feature, so this simply reports whether the game clock is stopped.
    public bool IsPaused => Time.timeScale <= 0f;

    /// <summary>The generated code for this run, or an empty string if none has been set. Never show it to the player.</summary>
    public string Code { get; private set; } = string.Empty;

    public event Action StateChanged;
    public event Action<int, int> DigitFound;

    private void Start()
    {
        // If the player dies with the keypad open, the screen must not stay "open" forever.
        player = FindAnyObjectByType<PlayerStats>();
        if (player != null) player.Died += OnPlayerDied;

        IsReady = true;
        Notify();
    }

    private void OnDestroy()
    {
        if (player != null) player.Died -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        SetKeypadOpen(false);
    }

    // Code


    // Sets the code for this run. Needs exactly four ASCII digits, such as "0427". It can be set once per run:
    // setting the same code again does nothing, and trying to replace it with a different code is rejected.

    public void SetCode(string fourDigits)
    {
        if (!IsValidCode(fourDigits))
        {
            Debug.LogWarning($"[ObjectiveState] SetCode needs exactly four digits (0-9), got '{fourDigits}'. Ignored.", this);
            return;
        }

        if (Code.Length > 0)
        {
            if (Code != fourDigits)
                Debug.LogWarning("[ObjectiveState] The code for this run is already set and cannot be replaced. Ignored.", this);
            return;
        }

        Code = fourDigits;
        Notify();
    }

    // Gets the digit for a position (1 to 4), only if the player has discovered it.
    public bool TryGetDigit(int positionInCode, out int digit)
    {
        digit = 0;
        if (positionInCode < 1 || positionInCode > CodeLength) return false;

        int stored = discoveredDigits[positionInCode - 1];
        if (stored < 0) return false;

        digit = stored;
        return true;
    }

    // Called by gameplay scripts


    // A clue paper was read. Pass something unique to that paper (for example its instance id), the position it
    // gives (1 to 4) and the digit on it (0 to 9). Returns true only if this position was newly discovered.
    // Papers placed by hand in a test scene still work with no code set. If a code IS set, the digit must match it.
    // A position is recorded once, so a second paper for the same position, or reading the same paper again,
    // returns false and changes nothing.

    public bool ReadClue(int clueId, int positionInCode, int digit)
    {
        if (positionInCode < 1 || positionInCode > CodeLength)
        {
            Debug.LogWarning($"[ObjectiveState] ReadClue: position {positionInCode} is not 1 to {CodeLength}. Ignored.", this);
            return false;
        }

        if (digit < 0 || digit > 9)
        {
            Debug.LogWarning($"[ObjectiveState] ReadClue: digit {digit} is not 0 to 9. Ignored.", this);
            return false;
        }

        if (HasWon) return false;

        if (Code.Length == CodeLength && Code[positionInCode - 1] - '0' != digit)
        {
            Debug.LogWarning($"[ObjectiveState] ReadClue: position {positionInCode} shows {digit} but the code says " +
                             $"{Code[positionInCode - 1]}. Ignored.", this);
            return false;
        }

        if (discoveredDigits[positionInCode - 1] >= 0) return false;
        if (collectedClueIds.Count >= totalClues) return false;

        // Update everything first, so subscribers reading the state from either event see the new values.
        // The id is remembered too, so the older CollectClue(id) cannot count the same paper again.
        discoveredDigits[positionInCode - 1] = digit;
        collectedClueIds.Add(clueId);

        DigitFound?.Invoke(positionInCode, digit);
        Notify();
        return true;
    }


    // Older way to count a clue paper that does not know its position. Kept so existing callers still work.
    // New code should use ReadClue. Returns true only if this was a new clue.

    public bool CollectClue(int clueId)
    {
        if (HasWon || collectedClueIds.Count >= totalClues) return false;
        if (!collectedClueIds.Add(clueId)) return false;

        Notify();
        return true;
    }

    // Call with true when the number-entry screen opens, and with false on EVERY way it can close.
    public void SetKeypadOpen(bool open)
    {
        if (IsKeypadOpen == open) return;

        IsKeypadOpen = open;
        Notify();
    }

    // The correct code was submitted. Stays accepted for the rest of the run and closes the keypad.
    public void AcceptCode()
    {
        if (IsCodeAccepted) return;

        IsCodeAccepted = true;
        IsKeypadOpen = false;
        Notify();
    }

    // The final key switch was used, but the ending has not finished yet. Skip this if the ending is instant.
    public void BeginEscape()
    {
        if (IsEscapeInProgress || HasWon) return;

        IsEscapeInProgress = true;
        Notify();
    }

    // The game is won. Works with or without BeginEscape.
    public void CompleteEscape()
    {
        if (HasWon) return;

        IsEscapeInProgress = false;
        IsKeypadOpen = false;
        HasWon = true;
        Notify();
    }


    // Clears this component only: the code, discovered digits and progress flags.
    // A real fresh run is a scene reload, which also resets spawned items, the spawner, the key, the keypad and the switch.

    public void ResetRun()
    {
        collectedClueIds.Clear();
        for (int i = 0; i < discoveredDigits.Length; i++) discoveredDigits[i] = -1;

        Code = string.Empty;
        IsCodeAccepted = false;
        IsKeypadOpen = false;
        IsEscapeInProgress = false;
        HasWon = false;
        Notify();
    }

    private static bool IsValidCode(string value)
    {
        if (value == null || value.Length != CodeLength) return false;

        foreach (char c in value)
        {
            if (c < '0' || c > '9') return false; // ASCII digits only, not other scripts' digit characters
        }

        return true;
    }

    private void Notify()
    {
        if (logChanges)
        {
            Debug.Log($"[ObjectiveState] clues {CollectedClueCount}/{totalClues}, keypad open {IsKeypadOpen}, " +
                      $"code accepted {IsCodeAccepted}, escape in progress {IsEscapeInProgress}, won {HasWon}", this);
        }

        StateChanged?.Invoke();
    }

    // Testing helpers: right-click the component header in the Inspector while playing.

    [ContextMenu("Debug: Collect a clue")]
    private void DebugCollectClue()
    {
        for (int position = 1; position <= CodeLength; position++)
        {
            if (discoveredDigits[position - 1] >= 0) continue;

            int digit = Code.Length == CodeLength ? Code[position - 1] - '0' : UnityEngine.Random.Range(0, 10);
            ReadClue(-1000 - position, position, digit);
            return;
        }
    }

    [ContextMenu("Debug: Toggle keypad open")]
    private void DebugToggleKeypad() => SetKeypadOpen(!IsKeypadOpen);

    [ContextMenu("Debug: Accept code")]
    private void DebugAcceptCode() => AcceptCode();

    [ContextMenu("Debug: Begin escape")]
    private void DebugBeginEscape() => BeginEscape();

    [ContextMenu("Debug: Complete escape (win)")]
    private void DebugWin() => CompleteEscape();

    [ContextMenu("Debug: Reset run")]
    private void DebugReset() => ResetRun();
}
