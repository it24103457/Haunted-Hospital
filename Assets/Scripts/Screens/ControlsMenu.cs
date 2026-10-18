using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/*
 * The Controls screen: a heading and a keyboard and mouse picture (ControlsDiagram) with a label for each control.
 * The heading and labels come from a plain text file so anyone can edit them without opening Unity:
 *   "title: Text" sets the heading, and each other line is "Control: Label", where the control is one of
 *   Esc, WASD, E, R, Shift, C, Space, LeftMouse, Mouse. Lines starting with # are notes that are never shown.
 * The picture is built the first time the screen opens. Back (or Esc) closes the screen and raises onBack.
 */
public class ControlsMenu : MonoBehaviour
{
    [SerializeField] private TextAsset content;

    [Header("UI")]
    [Tooltip("The heading at the top of the screen, set from the file's title line.")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("The area the picture fills.")]
    [SerializeField] private RectTransform diagramArea;

    [Header("Label style")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color textColor = new Color(0.82f, 0.82f, 0.78f, 1f);
    [SerializeField, Min(1f)] private float textSize = 34f;

    [SerializeField] private UnityEvent onBack;

    private RectTransform diagram;

    private void OnEnable()
    {
        if (diagram != null) return;

        var labels = Parse(out string title);
        if (titleText != null && !string.IsNullOrEmpty(title)) titleText.text = title;
        diagram = ControlsDiagram.Build(diagramArea, font, textColor, textSize, labels);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Back();
    }

    public void Back()
    {
        gameObject.SetActive(false);
        onBack?.Invoke();
    }

    // Reads the heading and the labels from the text file; see the class comment for the format.
    private Dictionary<string, string> Parse(out string title)
    {
        title = null;
        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (content == null) return labels;

        foreach (string raw in content.text.Replace("\r", "").Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;

            int colon = line.IndexOf(':');
            if (colon <= 0) continue;

            string key = line.Substring(0, colon).Trim();
            string value = line.Substring(colon + 1).Trim();
            if (key.Equals("title", StringComparison.OrdinalIgnoreCase)) title = value;
            else labels[key] = value;
        }

        return labels;
    }
}
