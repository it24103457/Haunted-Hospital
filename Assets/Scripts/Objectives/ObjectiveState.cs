using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The one place that knows how far the player is through the objectives:
/// find four clue papers, enter the code on the keypad, collect the exit key, use the key switch.
///
/// Gameplay scripts TELL this component what happened (a clue paper calls CollectClue, the keypad screen
/// calls SetKeypadOpen and AcceptCode, the key switch calls BeginEscape or CompleteEscape).
/// Everything else, including the Environment Director, only READS it through IObjectiveState.
///
/// Put one on an empty object in the scene. To test without the real clue, keypad and switch scripts,
/// right-click this component's header in the Inspector while the game is playing and use the Debug items.
/// Key possession is not tracked here: read PlayerStats.HasExitKey for that.
/// </summary>
public class ObjectiveState : MonoBehaviour, IObjectiveState
{
    [SerializeField, Min(1)] private int totalClues = 4;
    [SerializeField] private bool logChanges = false;

    private readonly HashSet<int> collectedClueIds = new HashSet<int>();
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

    public event Action StateChanged;

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

    // Called by gameplay scripts

    /// <summary>
    /// A clue paper was collected. Pass something unique to that paper (for example gameObject.GetInstanceID()),
    /// so picking up the same paper twice, or two papers showing the same digit, is counted correctly.
    /// Returns true only if this was a new clue.
    /// </summary>
    public bool CollectClue(int clueId)
    {
        if (HasWon || collectedClueIds.Count >= totalClues) return false;
        if (!collectedClueIds.Add(clueId)) return false;

        Notify();
        return true;
    }

    /// <summary>Call with true when the number-entry screen opens, and with false on EVERY way it can close.</summary>
    public void SetKeypadOpen(bool open)
    {
        if (IsKeypadOpen == open) return;

        IsKeypadOpen = open;
        Notify();
    }

    /// <summary>The correct code was submitted. Stays accepted for the rest of the run and closes the keypad.</summary>
    public void AcceptCode()
    {
        if (IsCodeAccepted) return;

        IsCodeAccepted = true;
        IsKeypadOpen = false;
        Notify();
    }

    /// <summary>The final key switch was used, but the ending has not finished yet. Skip this if the ending is instant.</summary>
    public void BeginEscape()
    {
        if (IsEscapeInProgress || HasWon) return;

        IsEscapeInProgress = true;
        Notify();
    }

    /// <summary>The game is won. Works with or without BeginEscape.</summary>
    public void CompleteEscape()
    {
        if (HasWon) return;

        IsEscapeInProgress = false;
        IsKeypadOpen = false;
        HasWon = true;
        Notify();
    }

    /// <summary>Start a fresh run in the same scene.</summary>
    public void ResetRun()
    {
        collectedClueIds.Clear();
        IsCodeAccepted = false;
        IsKeypadOpen = false;
        IsEscapeInProgress = false;
        HasWon = false;
        Notify();
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
    private void DebugCollectClue() => CollectClue(-1000 - collectedClueIds.Count);

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
