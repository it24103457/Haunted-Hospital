using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * The in-game HUD in the bottom-left corner. On top: a large key icon and the four code slots beside it. Below: a health,
 * an ammo and a sanity bar, each with its icon (heart, bullet, bottle) in the bar's colour; the ammo bar is the reserve
 * the player carries. To the right of the bars, "MAG" over the rounds loaded in the gun as a stacked fraction (loaded over
 * the magazine size), spanning from the top of the health bar to the bottom of the sanity bar.
 * The key icon is a drawn key outline that fills with off-white from the bottom up as the code digits are found, a quarter
 * per digit, so it is solid once the whole code is known; a faint copy of the whole key shows inside the outline, so the
 * unfilled part reads as empty at a glance. When the exit key itself is picked up it stays solid and flashes.
 * It only draws what it is told through its public methods and knows nothing about the game; PlayerHudBinder feeds it.
 * The pieces are built in code when the HUD starts, inside the game screens' canvas, which scales from a 1080p layout,
 * so the HUD keeps the same proportions at any resolution. The HUD is put first in the canvas, so the pause, death,
 * sanity and win screens draw over it.
 * Code slots are semi-transparent boxes with an off-white border and off-white digits (the screens' text colour); an
 * empty slot is blank. Bars have the same off-white border around a semi-transparent background. A newly found digit
 * makes its slot flash and fade back over a moment.
 */
public class PlayerHud : MonoBehaviour
{
    private const int SlotCount = 4;

    [Header("Look")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color healthColor = new Color(0.85f, 0.1f, 0.08f);
    [SerializeField] private Color ammoColor = new Color(1f, 0.9f, 0.2f);
    [SerializeField] private Color sanityColor = new Color(0.34f, 0.74f, 0.91f);
    [SerializeField] private Color borderColor = new Color(0.82f, 0.82f, 0.78f, 1f);
    [SerializeField] private Color slotFill = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color barBackground = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField] private Color digitColor = new Color(0.82f, 0.82f, 0.78f, 1f);
    [SerializeField] private Color magazineTextColor = new Color(0.82f, 0.82f, 0.78f, 1f);

    [Header("Key icon")]
    [SerializeField] private Color keyColor = new Color(0.82f, 0.82f, 0.78f, 1f);
    [Tooltip("The colour the key flashes from when the exit key is picked up.")]
    [SerializeField] private Color keyFlashColor = Color.white;
    [Tooltip("Opacity of the faint whole key shown inside the outline before it fills.")]
    [SerializeField, Range(0f, 1f)] private float keyGhostOpacity = 0.18f;

    [Header("Layout (1080p units)")]
    [SerializeField] private Vector2 margin = new Vector2(28f, 28f);
    [SerializeField] private float iconSize = 30f;
    [SerializeField] private float barWidth = 320f;
    [SerializeField] private float barHeight = 22f;
    [SerializeField] private float border = 3f;
    [SerializeField] private float rowGap = 14f;
    [SerializeField] private float slotGap = 8f;
    [SerializeField] private float slotsToBarsGap = 24f;
    [SerializeField] private float digitSize = 34f;
    [SerializeField] private float keySlotSize = 116f;
    [SerializeField] private float magazineGap = 22f;
    [SerializeField] private float magazineLabelSize = 22f;
    [SerializeField] private float magazineNumberSize = 32f;

    [Header("New digit flash")]
    [SerializeField] private Color flashFill = new Color(0.82f, 0.82f, 0.78f, 0.55f);
    [SerializeField, Min(0.01f)] private float flashSeconds = 1.5f;

    private RectTransform healthFill;
    private RectTransform ammoFill;
    private TextMeshProUGUI magazineLoaded;
    private TextMeshProUGUI magazineSize;
    private Image keyFill;
    private bool keyHeld;
    private float keyProgress;
    private float keyFlashStarted = float.NegativeInfinity;
    private RectTransform sanityFill;
    private readonly Image[] slotFills = new Image[SlotCount];
    private readonly TextMeshProUGUI[] slotDigits = new TextMeshProUGUI[SlotCount];
    private readonly float[] flashStarted = new float[SlotCount];
    private float slotSize;
    private bool built;

    private void Awake()
    {
        Build();
    }

    private void Update()
    {
        if (!built) return;

        // Each flashing slot fades from the flash colour back to its normal fill.
        for (int i = 0; i < SlotCount; i++)
        {
            float t = (Time.unscaledTime - flashStarted[i]) / flashSeconds;
            if (t < 0f || t > 1f) continue;
            slotFills[i].color = Color.Lerp(flashFill, slotFill, t);
        }

        // The key fades from its flash colour back to its normal colour after being picked up.
        float k = (Time.unscaledTime - keyFlashStarted) / flashSeconds;
        if (k >= 0f && k <= 1f) keyFill.color = Color.Lerp(keyFlashColor, keyColor, k);
    }

    // Bars: current against its maximum; values outside 0 to max are clamped.

    public void SetHealth(float current, float max) => SetBar(healthFill, current, max);
    public void SetAmmo(float current, float max) => SetBar(ammoFill, current, max);
    public void SetSanity(float current, float max) => SetBar(sanityFill, current, max);

    // The rounds loaded in the gun against the magazine size, shown as a stacked fraction beside the bars.
    public void SetMagazine(int loaded, int size)
    {
        if (magazineLoaded == null) return;
        magazineLoaded.text = loaded.ToString();
        magazineSize.text = size.ToString();
    }

    // How much of the key outline is filled, from 0 (outline only) to 1 (solid): the share of code digits found.
    public void SetKeyProgress(float share)
    {
        keyProgress = Mathf.Clamp01(share);
        UpdateKeyFill();
    }

    // Holding the exit key keeps the icon solid; picking it up makes it flash.
    public void SetKey(bool held)
    {
        if (!built || keyHeld == held) return;

        keyHeld = held;
        if (held) keyFlashStarted = Time.unscaledTime;
        else keyFill.color = keyColor;
        UpdateKeyFill();
    }

    private void UpdateKeyFill()
    {
        if (keyFill != null) keyFill.fillAmount = keyHeld ? 1f : keyProgress;
    }

    // Code slots: position 1 to 4. A newly shown digit makes its slot flash.
    public void SetDigit(int positionInCode, int digit)
    {
        int i = positionInCode - 1;
        if (!built || i < 0 || i >= SlotCount) return;

        string text = Mathf.Clamp(digit, 0, 9).ToString();
        if (slotDigits[i].text == text) return;

        slotDigits[i].text = text;
        flashStarted[i] = Time.unscaledTime;
    }

    // Empties every code slot and stops any flash.
    public void ClearDigits()
    {
        if (!built) return;

        for (int i = 0; i < SlotCount; i++)
        {
            slotDigits[i].text = string.Empty;
            slotFills[i].color = slotFill;
            flashStarted[i] = float.NegativeInfinity;
        }
    }

    // Sizes a bar's fill to the share of current over max, from the left.
    private static void SetBar(RectTransform fill, float current, float max)
    {
        if (fill == null) return;

        float share = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        fill.anchorMax = new Vector2(share, 1f);
    }

    /*
     * Builds the HUD under its own child of the canvas, anchored to the bottom-left corner. Rows are placed from the
     * bottom up: sanity, ammo, health, with the magazine count beside them, then the key icon and code slots above.
     */
    private void Build()
    {
        if (built) return;
        if (font == null) font = TMP_Settings.defaultFontAsset;

        RectTransform root = NewRect("PlayerHud", transform);
        root.SetAsFirstSibling();
        root.anchorMin = root.anchorMax = root.pivot = Vector2.zero;
        root.anchoredPosition = margin;
        root.sizeDelta = Vector2.zero;

        float rowHeight = Mathf.Max(iconSize, barHeight);
        sanityFill = BarRow(root, "Sanity", HudIcons.Bottle, sanityColor, 0f);
        ammoFill = BarRow(root, "Ammo", HudIcons.Bullet, ammoColor, rowHeight + rowGap);
        healthFill = BarRow(root, "Health", HudIcons.Heart, healthColor, 2f * (rowHeight + rowGap));

        float barsHeight = 3f * rowHeight + 2f * rowGap;
        MagazineColumn(root, new Vector2(iconSize + 14f + barWidth + magazineGap, 0f), barsHeight);

        /*
         * The key icon, with the code slots beside it, lined up along its bottom edge. The slots are sized so the fourth
         * one ends exactly where the bars end, whatever the bar width and key size are set to.
         */
        float slotsY = barsHeight + slotsToBarsGap;
        KeyIcon(root, new Vector2(0f, slotsY));
        float codeX = keySlotSize + slotGap * 2f;
        float barsRight = iconSize + 14f + barWidth;
        slotSize = Mathf.Max(16f, (barsRight - codeX - (SlotCount - 1) * slotGap) / SlotCount);
        for (int i = 0; i < SlotCount; i++)
            Slot(root, i, new Vector2(codeX + i * (slotSize + slotGap), slotsY));

        for (int i = 0; i < SlotCount; i++) flashStarted[i] = float.NegativeInfinity;
        built = true;
    }

    // One bar row at the given height: the icon on the left, then a bordered bar. Returns the bar's fill.
    private RectTransform BarRow(RectTransform root, string name, Sprite icon, Color color, float y)
    {
        float rowHeight = Mathf.Max(iconSize, barHeight);

        Image iconImage = NewImage(name + "Icon", root, color);
        iconImage.sprite = icon;
        iconImage.preserveAspect = true;
        Place(iconImage.rectTransform, new Vector2(0f, y + (rowHeight - iconSize) * 0.5f), new Vector2(iconSize, iconSize));

        Vector2 barPos = new Vector2(iconSize + 14f, y + (rowHeight - barHeight) * 0.5f);
        Image background = Framed(name + "Bar", root, barPos, new Vector2(barWidth, barHeight), barBackground);

        Image fill = NewImage("Level", background.transform, color);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
        return fillRect;
    }

    /*
     * A column as tall as the three bars: "MAG" at the top, level with the top of the health bar, then the loaded rounds,
     * a short divider and the magazine size, the last level with the bottom of the sanity bar. The space left over is
     * shared evenly between the pieces, and everything is centred on one line.
     */
    private void MagazineColumn(RectTransform root, Vector2 bottomLeft, float height)
    {
        const float width = 70f;
        const float dividerHeight = 2f;
        RectTransform column = NewRect("Magazine", root);
        Place(column, bottomLeft, new Vector2(width, height));

        float labelHeight = magazineLabelSize;
        float numberHeight = magazineNumberSize;
        float gap = Mathf.Max(0f, (height - labelHeight - 2f * numberHeight - dividerHeight) / 3f);

        magazineSize = NewText("Size", column, string.Empty, magazineNumberSize, FontStyles.Normal, TextAlignmentOptions.Center);
        Place(magazineSize.rectTransform, Vector2.zero, new Vector2(width, numberHeight));

        float y = numberHeight + gap;
        Image divider = NewImage("Divider", column, magazineTextColor);
        Place(divider.rectTransform, new Vector2(width * 0.25f, y), new Vector2(width * 0.5f, dividerHeight));

        y += dividerHeight + gap * 0.5f;
        magazineLoaded = NewText("Loaded", column, string.Empty, magazineNumberSize, FontStyles.Normal, TextAlignmentOptions.Center);
        Place(magazineLoaded.rectTransform, new Vector2(0f, y), new Vector2(width, numberHeight));

        y += numberHeight + gap * 1.5f;
        TextMeshProUGUI label = NewText("Label", column, "MAG", magazineLabelSize, FontStyles.Normal, TextAlignmentOptions.Center);
        Place(label.rectTransform, new Vector2(0f, y), new Vector2(width, labelHeight));
    }

    /*
     * The key icon: the drawn key outline, a faint copy of the whole key inside it, and the solid key over both filling
     * from the bottom up (a filled image), so the outline always shows and the fill rises as digits are found.
     */
    private void KeyIcon(RectTransform root, Vector2 position)
    {
        Image outline = NewImage("KeyOutline", root, keyColor);
        outline.sprite = HudIcons.KeyOutline;
        outline.preserveAspect = true;
        Place(outline.rectTransform, position, new Vector2(keySlotSize, keySlotSize));

        Image ghost = NewImage("KeyGhost", root, new Color(keyColor.r, keyColor.g, keyColor.b, keyColor.a * keyGhostOpacity));
        ghost.sprite = HudIcons.Key;
        ghost.preserveAspect = true;
        Place(ghost.rectTransform, position, new Vector2(keySlotSize, keySlotSize));

        keyFill = NewImage("KeyFill", root, keyColor);
        keyFill.sprite = HudIcons.Key;
        keyFill.preserveAspect = true;
        keyFill.type = Image.Type.Filled;
        keyFill.fillMethod = Image.FillMethod.Vertical;
        keyFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        keyFill.fillAmount = 0f;
        Place(keyFill.rectTransform, position, new Vector2(keySlotSize, keySlotSize));
    }

    // One code slot: an off-white border around a semi-transparent box, with its digit centred.
    private void Slot(RectTransform root, int index, Vector2 position)
    {
        Image fill = Framed("CodeSlot" + (index + 1), root, position, new Vector2(slotSize, slotSize), slotFill);
        slotFills[index] = fill;

        TextMeshProUGUI digit = NewText("Digit", fill.transform.parent, string.Empty, digitSize, FontStyles.Bold, TextAlignmentOptions.Center);
        digit.color = digitColor;
        Inset(digit.rectTransform, 0f);
        slotDigits[index] = digit;
    }

    // A text in the HUD's font and the magazine text colour; callers place it.
    private TextMeshProUGUI NewText(string name, Transform parent, string text, float size, FontStyles style,
                                    TextAlignmentOptions alignment)
    {
        var t = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = magazineTextColor;
        t.alignment = alignment;
        t.raycastTarget = false;
        t.text = text;
        return t;
    }

    /*
     * A box with a border: four thin edge strips in the border colour around an inner fill in the given colour. The
     * border is only the edges, so a see-through fill shows what is behind the HUD rather than a solid square.
     * Returns the inner fill.
     */
    private Image Framed(string name, RectTransform root, Vector2 bottomLeft, Vector2 size, Color fillColor)
    {
        RectTransform box = NewRect(name, root);
        Place(box, bottomLeft, size);

        Image fill = NewImage("Fill", box, fillColor);
        Inset(fill.rectTransform, border);

        Edge(box, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -border), Vector2.zero, borderColor);
        Edge(box, "Bottom", Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, border), borderColor);
        Edge(box, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, border), new Vector2(border, -border), borderColor);
        Edge(box, "Right", new Vector2(1f, 0f), Vector2.one, new Vector2(-border, border), new Vector2(0f, -border), borderColor);
        return fill;
    }

    private void Edge(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin,
                      Vector2 offsetMax, Color color)
    {
        RectTransform r = NewImage(name, parent, color).rectTransform;
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = offsetMin;
        r.offsetMax = offsetMax;
    }

    // Places a rect by its bottom-left corner inside the HUD root.
    private static void Place(RectTransform r, Vector2 bottomLeft, Vector2 size)
    {
        r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
        r.anchoredPosition = bottomLeft;
        r.sizeDelta = size;
    }

    // Stretches a rect to fill its parent, minus the given inset on every side.
    private static void Inset(RectTransform r, float inset)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(inset, inset);
        r.offsetMax = new Vector2(-inset, -inset);
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        Image image = NewRect(name, parent).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var r = (RectTransform)go.transform;
        r.SetParent(parent, false);
        return r;
    }
}
