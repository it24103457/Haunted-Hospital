using TMPro;
using UnityEngine;

/// <summary>
/// A clue paper. Press E to read it: the digit is noted on screen and counted once toward the four clues.
/// The digit is read from the paper's own text (the DigitText object on PF_DigitPaper), so whoever sets that
/// text (the World Builder, or a spawn system through Configure) decides what the paper says.
/// The paper stays in the world, so the player can read it again.
/// </summary>
public class ClueInteractable : MonoBehaviour, IInteractable
{
    private static int nextId;

    [Tooltip("Which digit of the four-digit code this paper gives: 1 = first digit ... 4 = last digit.")]
    [SerializeField, Range(1, 4)] private int positionInCode = 1;

    [Tooltip("The text showing the digit. Found automatically if left empty.")]
    [SerializeField] private TMP_Text digitText;

    [Tooltip("Only used if the paper has no text object.")]
    [SerializeField, Range(0, 9)] private int fallbackDigit;

    private ObjectiveState objectives;
    private int id;

    public int PositionInCode => positionInCode;
    public bool IsCollected { get; private set; }

    /// <summary>The digit on this paper: the first digit character in its text, or the fallback digit.</summary>
    public int Digit
    {
        get
        {
            if (digitText != null)
            {
                foreach (char c in digitText.text)
                {
                    if (char.IsDigit(c)) return c - '0';
                }
            }

            return fallbackDigit;
        }
    }

    private void Awake()
    {
        id = ++nextId; // unique for this paper, so reading it twice is never counted twice

        if (digitText == null) digitText = GetComponentInChildren<TMP_Text>();
    }

    /// <summary>For a spawn system: set which digit this paper shows and where it goes in the code.</summary>
    public void Configure(int newPositionInCode, int digit)
    {
        positionInCode = Mathf.Clamp(newPositionInCode, 1, 4);
        fallbackDigit = Mathf.Clamp(digit, 0, 9);

        if (digitText != null) digitText.text = fallbackDigit.ToString();
    }

    public string GetPrompt()
    {
        return IsCollected ? "Press E to read the note again" : "Press E to pick up the note";
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (objectives == null) objectives = FindAnyObjectByType<ObjectiveState>();

        bool isNew = objectives != null && objectives.CollectClue(id);
        IsCollected = true;

        ObjectiveHud hud = ObjectiveHud.Instance;
        if (hud == null) return;

        hud.AddNote(positionInCode, Digit);
        hud.ShowMessage(isNew ? $"You found a note: {Digit}" : $"The note reads: {Digit}");
    }
}
