using TMPro;
using UnityEngine;
using UnityEngine.Serialization;


// A clue paper. Press E to read it: the digit is recorded in ObjectiveState (once per code position).
// The paper stays in the world, so the player can read it again; rereading never changes progress.

// The digit is a plain number stored on this component. It is the only source of truth for the digit:
// the text objects on the paper are display only and are never read back. Set the digit and position in the
// Inspector for a hand-placed test paper, or let a spawn system call Configure.

// Messages: the first successful read shows no message here. The objective spawner owns the progress
// message. This script only shows feedback when a paper whose position is already known is read again.

public class ClueInteractable : MonoBehaviour, IInteractable
{
    private const string Heading = "ELEVATOR ACCESS";

    private static int nextId;

    [Tooltip("Which digit of the four-digit code this paper gives: 1 = first digit ... 4 = last digit.")]
    [SerializeField, Range(1, 4)] private int positionInCode = 1;

    [Tooltip("The digit on this paper (0 to 9). This number is what the game uses; the text is only for show.")]
    [FormerlySerializedAs("fallbackDigit")]
    [SerializeField, Range(0, 9)] private int digit;

    [Tooltip("Display only: the heading / position label, for example 'DIGIT 2 OF 4'. Optional.")]
    [SerializeField] private TMP_Text positionText;

    [Tooltip("Display only: the text showing just the digit. Found automatically if left empty.")]
    [SerializeField] private TMP_Text digitText;

    private ObjectiveState objectives;
    private int id;
    private bool warnedNoObjectives;

    public int PositionInCode => positionInCode;

    // True once ObjectiveState confirms this paper's position is discovered.
    public bool IsCollected { get; private set; }

    // The digit on this paper. Always the stored number, never parsed from text.
    public int Digit => digit;

    private void Awake()
    {
        id = ++nextId; // unique for this paper

        if (digitText == null && positionText == null) digitText = GetComponentInChildren<TMP_Text>();

        // Keep the display in step with the numbers. Only when the heading and the digit are separate text
        // objects: with one combined text object, rewriting it would lose the heading.
        if (positionText != null && digitText != null && positionText != digitText) RefreshDisplay();
    }

    // For a spawn system: set where this paper goes in the code and which digit it shows.
    public void Configure(int newPositionInCode, int newDigit)
    {
        positionInCode = Mathf.Clamp(newPositionInCode, 1, 4);
        digit = Mathf.Clamp(newDigit, 0, 9);

        RefreshDisplay();
    }

    public string GetPrompt()
    {
        return IsCollected ? "Press E to read the note again" : "Press E to pick up the note";
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (objectives == null) objectives = FindAnyObjectByType<ObjectiveState>();
        if (objectives == null)
        {
            if (!warnedNoObjectives)
            {
                warnedNoObjectives = true;
                Debug.LogWarning($"ClueInteractable '{name}': no ObjectiveState in the scene, so the note cannot be recorded.", this);
            }
            return;
        }

        // A new position: record it. The spawner shows the progress message, so nothing more to do here.
        if (objectives.ReadClue(id, positionInCode, digit))
        {
            IsCollected = true;
            return;
        }

        // Not new. Only call it a reread if the game really knows this position already.
        if (!objectives.TryGetDigit(positionInCode, out _))
        {
            // Rejected (for example the digit does not match the generated code): not collected, not a reread.
            Debug.LogWarning($"ClueInteractable '{name}': ObjectiveState rejected position {positionInCode}, digit {digit}.", this);
            return;
        }

        IsCollected = true;

        ObjectiveHud hud = ObjectiveHud.Instance;
        if (hud != null) hud.ShowMessage($"The note reads: {digit}");
    }

    private void RefreshDisplay()
    {
        if (positionText != null) positionText.text = $"{Heading}\nDIGIT {positionInCode} OF 4";
        if (digitText != null) digitText.text = digit.ToString();
    }
}
