using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * Draws the controls as a picture for the Controls screen: a keyboard cluster on the left and a mouse on the right,
 * with a label beside each control and a thin line running from the label to it.
 * The labels are passed in by control name (Esc, WASD, E, R, Shift, C, Space, LeftMouse, Mouse); a control without one
 * gets no label or line. A label's " / " starts a smaller second line.
 * The keyboard is laid out like a real one: Esc in the corner, W A S D in their usual shape with E and R beside W,
 * a wide Shift with C next to it, and a long Space bar along the bottom. The mouse is a rounded oval.
 * The movement keys and the mouse's left button have a darker face than the other keys, so they stand out.
 * Keys are dark boxes with a light border and a darker edge below, so they read as raised keys.
 * Everything is placed in a fixed 1600 x 560 layout centred in the given area, matching the page area of the
 * menu screens on their 1920 x 1080 canvas; the canvas scaler takes care of other screen sizes.
 */
public static class ControlsDiagram
{
    // The labels being drawn, by control name.
    private static System.Collections.Generic.IReadOnlyDictionary<string, string> labels;
    private const float Unit = 96f;
    private const float LineWidth = 2f;

    private static readonly Color KeyFill = new Color(0.1f, 0.1f, 0.11f, 0.95f);
    private static readonly Color KeyBorder = new Color(0.82f, 0.82f, 0.78f, 0.75f);
    private static readonly Color KeyEdge = new Color(0.03f, 0.03f, 0.035f, 0.95f);
    private static readonly Color KeyText = new Color(0.95f, 0.95f, 0.92f, 1f);
    // A darker face that picks out the movement keys and the mouse's left button.
    private static readonly Color Highlight = new Color(0.035f, 0.035f, 0.04f, 0.98f);

    // Builds the diagram as a child of parent and returns its root, filling the parent's area.
    public static RectTransform Build(RectTransform parent, TMP_FontAsset font, Color textColor, float textSize,
                                      System.Collections.Generic.IReadOnlyDictionary<string, string> controlLabels)
    {
        labels = controlLabels;
        var root = NewRect("ControlsDiagram", parent);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        // The fixed layout, centred in the area; positions below are from its centre.
        var stage = NewRect("Stage", root);
        stage.anchorMin = stage.anchorMax = new Vector2(0.5f, 0.5f);
        stage.sizeDelta = new Vector2(1600f, 560f);

        Color lineColor = new Color(textColor.r, textColor.g, textColor.b, 0.55f);

        // Keyboard cluster.
        Key(stage, font, "Esc", new Vector2(-470f, 230f), Unit, 26f);
        // W A S D sit together; E and R (raised), Shift and C (lowered) and Space (lower still) keep wider gaps from them,
        // so each reads as its own control.
        Key(stage, font, "W", new Vector2(-230f, 150f), Unit, 38f, Highlight);
        Key(stage, font, "E", new Vector2(-92f, 180f), Unit, 38f);
        Key(stage, font, "R", new Vector2(16f, 180f), Unit, 38f);
        Key(stage, font, "A", new Vector2(-338f, 42f), Unit, 38f, Highlight);
        Key(stage, font, "S", new Vector2(-230f, 42f), Unit, 38f, Highlight);
        Key(stage, font, "D", new Vector2(-122f, 42f), Unit, 38f, Highlight);
        Key(stage, font, "Shift", new Vector2(-400f, -96f), 168f, 26f);
        Key(stage, font, "C", new Vector2(-92f, -96f), Unit, 38f);
        Key(stage, font, "Space", new Vector2(-200f, -228f), 420f, 26f);

        // Labels on the left point right, to the left edge of their key.
        Callout(stage, font, textColor, textSize, lineColor, "Esc", new Vector2(-560f, 230f), new Vector2(-518f, 230f), TextAlignmentOptions.MidlineRight);
        Callout(stage, font, textColor, textSize, lineColor, "WASD", new Vector2(-560f, 42f), new Vector2(-386f, 42f), TextAlignmentOptions.MidlineRight);
        Callout(stage, font, textColor, textSize, lineColor, "Shift", new Vector2(-560f, -96f), new Vector2(-484f, -96f), TextAlignmentOptions.MidlineRight);
        Callout(stage, font, textColor, textSize, lineColor, "Space", new Vector2(-560f, -228f), new Vector2(-410f, -228f), TextAlignmentOptions.MidlineRight);

        // Labels on the right point left, to their key.
        Callout(stage, font, textColor, textSize, lineColor, "R", new Vector2(110f, 180f), new Vector2(64f, 180f), TextAlignmentOptions.MidlineLeft);
        Callout(stage, font, textColor, textSize, lineColor, "C", new Vector2(110f, -96f), new Vector2(-44f, -96f), TextAlignmentOptions.MidlineLeft);
        Callout(stage, font, textColor, textSize, lineColor, "E", new Vector2(90f, 280f), new Vector2(-92f, 228f), TextAlignmentOptions.MidlineLeft);

        // Mouse, with its left button picked out.
        Mouse(stage, new Vector2(520f, -20f));
        Callout(stage, font, textColor, textSize, lineColor, "LeftMouse", new Vector2(467f, 232f), new Vector2(467f, 112f), TextAlignmentOptions.Bottom);
        Callout(stage, font, textColor, textSize, lineColor, "Mouse", new Vector2(520f, -250f), new Vector2(520f, -170f), TextAlignmentOptions.Top);

        return root;
    }

    // A key: a darker edge peeking out below, a light border, a dark face (the standard one unless another is given) and the key's name.
    private static void Key(RectTransform stage, TMP_FontAsset font, string name, Vector2 centre, float width, float fontSize, Color? face = null)
    {
        var size = new Vector2(width, Unit);
        Box(stage, "Edge " + name, centre + new Vector2(0f, -6f), size, KeyEdge);
        Box(stage, "Key " + name, centre, size, KeyBorder);
        Box(stage, "Face " + name, centre, size - new Vector2(6f, 6f), face ?? KeyFill);

        var label = Text(stage, font, name, centre, size, fontSize, KeyText, TextAlignmentOptions.Center);
        label.fontStyle = FontStyles.Bold;
    }

    /*
     * A mouse seen from above: a rounded oval body with a light border and a darker edge below, and two buttons across
     * its top part, split down the middle, with a small scroll wheel between them.
     * The body clips everything inside it to its oval, so the darker left button follows the mouse's curved outline.
     */
    private static void Mouse(RectTransform stage, Vector2 centre)
    {
        var body = new Vector2(210f, 300f);
        Oval(stage, "Mouse Edge", centre + new Vector2(0f, -6f), body, KeyEdge);
        Oval(stage, "Mouse Border", centre, body, KeyBorder);
        var face = Oval(stage, "Mouse Body", centre, body - new Vector2(6f, 6f), KeyFill);
        face.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        // Inside the body, positions are from the body's centre.
        float buttonHeight = 120f;
        float top = body.y * 0.5f;
        Box(face, "Left Button", new Vector2(-body.x * 0.25f - 2f, top - buttonHeight * 0.5f), new Vector2(body.x * 0.5f - 4f, buttonHeight), Highlight);
        Box(face, "Mouse Split", new Vector2(0f, top - buttonHeight * 0.5f), new Vector2(4f, buttonHeight), KeyBorder);
        Box(face, "Mouse Base Line", new Vector2(0f, top - buttonHeight), new Vector2(body.x, 3f), KeyBorder);
        Box(face, "Scroll Wheel", new Vector2(0f, top - buttonHeight * 0.5f), new Vector2(14f, 40f), KeyBorder);
    }

    private static RectTransform Oval(RectTransform stage, string name, Vector2 centre, Vector2 size, Color color)
    {
        var rt = Box(stage, name, centre, size, color);
        rt.GetComponent<Image>().sprite = OvalSprite();
        return rt;
    }

    private static Sprite ovalSprite;

    // A white filled circle with a soft edge, made once; stretched to a box it becomes an oval of that box's shape.
    private static Sprite OvalSprite()
    {
        if (ovalSprite != null) return ovalSprite;

        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float r = size * 0.5f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r));
                byte a = (byte)(Mathf.Clamp01(r - d) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(pixels);
        tex.Apply();
        ovalSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return ovalSprite;
    }

    /*
     * The given control's label with a thin line from the label's anchor point to the control.
     * Nothing is drawn when the control has no label; a " / " in the label starts a smaller second line.
     */
    private static void Callout(RectTransform stage, TMP_FontAsset font, Color textColor, float textSize, Color lineColor,
                                string control, Vector2 labelAt, Vector2 pointAt, TextAlignmentOptions align)
    {
        if (labels == null || !labels.TryGetValue(control, out string text) || string.IsNullOrWhiteSpace(text)) return;
        int split = text.IndexOf(" / ", System.StringComparison.Ordinal);
        if (split >= 0) text = text.Substring(0, split) + "\n<size=70%>" + text.Substring(split + 3) + "</size>";

        var labelSize = new Vector2(420f, textSize * 2.6f);
        Vector2 labelCentre = labelAt;
        Vector2 lineStart = labelAt;

        // The label box sits on the side of its anchor point away from the control, so the line meets the text's edge.
        if (align == TextAlignmentOptions.MidlineRight) { labelCentre.x -= labelSize.x * 0.5f + 8f; }
        else if (align == TextAlignmentOptions.MidlineLeft) { labelCentre.x += labelSize.x * 0.5f + 8f; }
        else if (align == TextAlignmentOptions.Bottom) { labelCentre.y += labelSize.y * 0.5f; }
        else if (align == TextAlignmentOptions.Top) { labelCentre.y -= labelSize.y * 0.5f; }

        Text(stage, font, text, labelCentre, labelSize, textSize, textColor, align);
        Line(stage, lineStart, pointAt, lineColor);
    }

    private static void Line(RectTransform stage, Vector2 from, Vector2 to, Color color)
    {
        Vector2 d = to - from;
        var line = Box(stage, "Line", (from + to) * 0.5f, new Vector2(d.magnitude, LineWidth), color);
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    private static RectTransform Box(RectTransform stage, string name, Vector2 centre, Vector2 size, Color color)
    {
        var rt = NewRect(name, stage);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = centre;
        rt.sizeDelta = size;
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rt;
    }

    private static TextMeshProUGUI Text(RectTransform stage, TMP_FontAsset font, string text, Vector2 centre, Vector2 size,
                                        float fontSize, Color color, TextAlignmentOptions align)
    {
        var rt = NewRect("Text", stage);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = centre;
        rt.sizeDelta = size;
        var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = align;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }
}
