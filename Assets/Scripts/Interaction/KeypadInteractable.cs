using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// The elevator keypad. Press E to open an on-screen number pad, type four digits (keyboard or mouse),
/// Enter to submit, Esc to cancel. A correct code tells ObjectiveState and opens the elevator doors.
///
/// The correct code is ObjectiveState.Code, which the objective spawner generates for each run.
/// In a test scene with no spawner (so no generated code), it is the four digits the player has read from papers.
/// The player must have read all four papers before any code is accepted, so guessing cannot skip them.
/// 'Code Override' is for testing only and must stay empty in the real game.
/// While the pad is open the player's movement, interaction and weapon are switched off.
/// </summary>
public class KeypadInteractable : MonoBehaviour, IInteractable
{
    private const int CodeLength = 4;

    private static readonly Key[] DigitKeys =
    {
        Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };

    private static readonly Key[] NumpadKeys =
    {
        Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
        Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9
    };

    [Tooltip("TEST ONLY. Leave empty in the real game. Four digits here replace the generated code and skip the " +
             "'find all four papers' check, so the keypad can be tried without the papers.")]
    [SerializeField] private string codeOverride = "";

    [Tooltip("The elevator's Animator controller name. The animator with this controller gets Open set to true.")]
    [SerializeField] private string elevatorControllerName = "AC_ElevatorDoor";

    [SerializeField] private string openParameter = "Open";

    [Tooltip("Optional extra things to do when the code is accepted.")]
    [SerializeField] private UnityEvent onCodeAccepted;

    private ObjectiveState objectives;
    private PlayerStats player;

    private bool isOpen;
    private bool unlocked;
    private string entered = "";
    private string feedback = "";

    private GUIStyle displayStyle;
    private GUIStyle feedbackStyle;
    private GUIStyle buttonStyle;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerStats>();
        if (player != null) player.Died += OnPlayerDied;
    }

    private void OnDestroy()
    {
        if (player != null) player.Died -= OnPlayerDied;
    }

    public string GetPrompt()
    {
        return unlocked ? "The keypad is unlocked" : "Press E to use the keypad";
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (unlocked || isOpen) return;

        if (objectives == null) objectives = FindAnyObjectByType<ObjectiveState>();

        isOpen = true;
        entered = "";
        feedback = "";

        PlayerControlLock.Set(true);
        if (objectives != null) objectives.SetKeypadOpen(true);
    }

    private void Update()
    {
        if (!isOpen) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        for (int digit = 0; digit <= 9; digit++)
        {
            if (keyboard[DigitKeys[digit]].wasPressedThisFrame || keyboard[NumpadKeys[digit]].wasPressedThisFrame)
                AddDigit(digit);
        }

        if (keyboard.backspaceKey.wasPressedThisFrame) RemoveDigit();
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) Submit();
        if (keyboard.escapeKey.wasPressedThisFrame) Close(true);
    }

    private void AddDigit(int digit)
    {
        if (entered.Length >= CodeLength) return;

        entered += digit.ToString();
        feedback = "";
    }

    private void RemoveDigit()
    {
        if (entered.Length == 0) return;

        entered = entered.Substring(0, entered.Length - 1);
        feedback = "";
    }

    private void Submit()
    {
        if (entered.Length != CodeLength)
        {
            feedback = "Enter four digits";
            return;
        }

        bool testOverride = !string.IsNullOrEmpty(codeOverride);

        // Nobody gets through by guessing: every paper must have been read first.
        if (!testOverride && objectives != null && !AllDigitsFound())
        {
            feedback = "Find all four code papers first.";
            entered = "";
            return;
        }

        string expected = ExpectedCode();
        if (expected.Length != CodeLength)
        {
            feedback = "The keypad is not working";
            return;
        }

        if (entered != expected)
        {
            feedback = "Wrong code";
            entered = "";
            return;
        }

        Accept();
    }

    private void Accept()
    {
        unlocked = true;

        if (objectives != null) objectives.AcceptCode();
        Close(true);

        OpenElevator();
        onCodeAccepted.Invoke();

        // No message here: the objective spawner announces that the elevator is unlocked.
    }

    private void OpenElevator()
    {
        foreach (Animator animator in FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            if (animator.runtimeAnimatorController == null ||
                animator.runtimeAnimatorController.name != elevatorControllerName) continue;

            // AC_ElevatorDoor's "Open" is a Trigger, so use the matching call for whichever type the parameter is.
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name != openParameter) continue;

                if (p.type == AnimatorControllerParameterType.Trigger) animator.SetTrigger(openParameter);
                else if (p.type == AnimatorControllerParameterType.Bool) animator.SetBool(openParameter, true);
            }
        }
    }

    // True when the player has discovered all four code positions.
    private bool AllDigitsFound()
    {
        if (objectives == null) return false;

        for (int position = 1; position <= CodeLength; position++)
        {
            if (!objectives.TryGetDigit(position, out _)) return false;
        }

        return true;
    }

    // The test override if there is one, otherwise the generated code, otherwise (a test scene with no spawner)
    // the four digits the player has discovered. Never read from paper text.
    private string ExpectedCode()
    {
        if (!string.IsNullOrEmpty(codeOverride)) return codeOverride;
        if (objectives == null) return "";

        if (!string.IsNullOrEmpty(objectives.Code)) return objectives.Code;

        string code = "";
        for (int position = 1; position <= CodeLength; position++)
        {
            if (!objectives.TryGetDigit(position, out int digit)) return "";
            code += digit.ToString();
        }

        return code;
    }

    private void OnPlayerDied()
    {
        if (isOpen) Close(false);
    }

    private void Close(bool restoreControls)
    {
        isOpen = false;

        if (restoreControls) PlayerControlLock.Set(false);
        if (objectives != null) objectives.SetKeypadOpen(false);
    }

    private void OnGUI()
    {
        if (!isOpen) return;

        EnsureStyles();

        const float boxW = 300f;
        const float boxH = 420f;
        float x = (Screen.width - boxW) * 0.5f;
        float y = (Screen.height - boxH) * 0.5f;

        GUI.Box(new Rect(x, y, boxW, boxH), "KEYPAD");

        string shown = entered.PadRight(CodeLength, '_');
        GUI.Label(new Rect(x, y + 35f, boxW, 50f), string.Join(" ", shown.ToCharArray()), displayStyle);
        GUI.Label(new Rect(x, y + 85f, boxW, 28f), feedback, feedbackStyle);

        // 3 x 4 grid: 1 2 3 / 4 5 6 / 7 8 9 / back 0 enter
        string[] labels = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "<", "0", "OK" };
        for (int i = 0; i < labels.Length; i++)
        {
            float bx = x + 20f + (i % 3) * 90f;
            float by = y + 120f + (i / 3) * 62f;

            if (!GUI.Button(new Rect(bx, by, 80f, 54f), labels[i], buttonStyle)) continue;

            if (labels[i] == "<") RemoveDigit();
            else if (labels[i] == "OK") Submit();
            else AddDigit(labels[i][0] - '0');
        }

        if (GUI.Button(new Rect(x + 20f, y + boxH - 44f, boxW - 40f, 32f), "Cancel (Esc)"))
            Close(true);
    }

    private void EnsureStyles()
    {
        if (displayStyle != null) return;

        displayStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 36 };
        feedbackStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 22 };
    }
}
