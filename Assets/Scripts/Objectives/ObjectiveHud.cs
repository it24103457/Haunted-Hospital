using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Shows the objective feedback on screen: the digits found so far, short messages, and the win screen.
/// Put it on the same empty object as ObjectiveState (it adds one automatically).
/// Other scripts reach it through ObjectiveHud.Instance and must check it for null.
/// </summary>
[RequireComponent(typeof(ObjectiveState))]
public class ObjectiveHud : MonoBehaviour
{
    public static ObjectiveHud Instance { get; private set; }

    private ObjectiveState objectives;

    // position in the code (1-based) -> digit found on that note
    private readonly Dictionary<int, int> notes = new Dictionary<int, int>();

    private string message = "";
    private float messageUntil;

    private GUIStyle notesStyle;
    private GUIStyle messageStyle;
    private GUIStyle titleStyle;

    private void Awake()
    {
        Instance = this;
        objectives = GetComponent<ObjectiveState>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Remember the digit written on a note, in the slot of the code it belongs to.</summary>
    public void AddNote(int positionInCode, int digit)
    {
        notes[positionInCode] = digit;
    }

    /// <summary>Show a line of text in the lower middle of the screen for a few seconds.</summary>
    public void ShowMessage(string text, float seconds = 3f)
    {
        message = text;
        messageUntil = Time.unscaledTime + seconds;
    }

    private void OnGUI()
    {
        EnsureStyles();

        float w = Screen.width;
        float h = Screen.height;

        if (notes.Count > 0)
            GUI.Label(new Rect(w * 0.5f - 200f, 12f, 400f, 30f), NotesLine(), notesStyle);

        if (Time.unscaledTime < messageUntil)
            GUI.Label(new Rect(w * 0.5f - 300f, h * 0.68f, 600f, 40f), message, messageStyle);

        if (objectives != null && objectives.HasWon)
            DrawWinScreen(w, h);
    }

    private string NotesLine()
    {
        int slots = objectives != null ? objectives.TotalClues : 4;
        string[] parts = new string[slots];

        for (int i = 0; i < slots; i++)
            parts[i] = notes.TryGetValue(i + 1, out int digit) ? digit.ToString() : "_";

        return "Notes:  " + string.Join("  ", parts);
    }

    private void DrawWinScreen(float w, float h)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0f, h * 0.38f, w, 60f), "YOU ESCAPED", titleStyle);

        // LoadScene only works for scenes in the Build Settings, so only offer it when that is true.
        if (SceneManager.GetActiveScene().buildIndex >= 0)
        {
            if (GUI.Button(new Rect(w * 0.5f - 80f, h * 0.38f + 80f, 160f, 40f), "Play again"))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void EnsureStyles()
    {
        if (notesStyle != null) return;

        notesStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
        notesStyle.normal.textColor = new Color(0.9f, 0.85f, 0.6f);

        messageStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22 };
        messageStyle.normal.textColor = Color.white;

        titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 48 };
        titleStyle.normal.textColor = Color.white;
    }
}
