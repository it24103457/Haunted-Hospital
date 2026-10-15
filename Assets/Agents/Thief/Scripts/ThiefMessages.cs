using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// How ThiefMessages presents its messages.
public enum ThiefMessageStyle
{
    // Small, lean, top-right list of recent events that fade one by one, like accessibility subtitles.
    Subtitle,
    // One large bold message at the top centre that replaces the previous one.
    Banner
}

/*
 * In-game HUD messages for what the Thief does. Listens to the scene's one ThiefAgent
 * (OnSpawned, OnVanished, OnItemStolen, OnItemDelivered, OnItemDropped) and shows a short message for each.
 *
 * Subtitle style (default, used in the game): a quiet list in the top-right corner, newest at the bottom,
 *   "Thief appeared", "Thief vanished", "Thief stole Medkit", "Thief stashed Medkit", "Thief dropped Medkit".
 *   Text is pale with a soft top-to-bottom gradient, the item name in its own colour, and a faint dark strip
 *   behind each line. Each line holds, then fades on its own; the oldest drops off past maxLines.
 * Banner style (test scene copy): "The Thief has appeared!", "The Thief has vanished", "The Thief stole a Medkit!"
 *   and so on, large and
 *   bold at the top centre, tinted with the item's colour; a new message replaces the current one.
 *   The vertical position slider moves it up or down the screen.
 *
 * Lives on a screen-space Canvas prefab with one TextMeshPro text: PF_ThiefMessages (Subtitle) and
 * PF_ThiefMessages_Test (Banner), separate from PF_Thief.
 */
public class ThiefMessages : MonoBehaviour
{
    [Tooltip("The text that shows the messages.")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Tooltip("Subtitle: quiet top-right list (game). Banner: large top-centre message (test scene).")]
    [SerializeField] private ThiefMessageStyle style = ThiefMessageStyle.Subtitle;

    [Header("Subtitle style")]
    [Tooltip("Gap from the top-right corner of the screen, in reference pixels (1920x1080).")]
    [SerializeField] private Vector2 subtitleMargin = new Vector2(28f, 28f);
    [SerializeField] private int subtitleFontSize = 22;
    [Tooltip("Most lines shown at once; older ones drop off.")]
    [SerializeField] private int maxLines = 4;
    [Tooltip("Seconds a subtitle line stays fully visible before it starts to fade.")]
    [SerializeField] private float subtitleHoldSeconds = 3f;
    [Tooltip("Seconds a subtitle line takes to fade out.")]
    [SerializeField] private float subtitleFadeSeconds = 1f;
    [Tooltip("Colour of the subtitle wording (the item name uses the item colour).")]
    [SerializeField] private Color subtitleTextColor = new Color(0.9f, 0.88f, 0.84f);
    [Tooltip("Faint strip behind each subtitle line; alpha sets how strong it is.")]
    [SerializeField] private Color subtitleBackdrop = new Color(0f, 0f, 0f, 0.4f);

    [Header("Banner style")]
    [Tooltip("Distance of the banner from the top of the screen, as a fraction of the screen height.")]
    [Range(0f, 1f)]
    [SerializeField] private float verticalPosition = 0.12f;
    [Tooltip("Seconds a banner stays fully visible before it starts to fade.")]
    [SerializeField] private float holdSeconds = 2f;
    [Tooltip("Seconds a banner takes to fade out.")]
    [SerializeField] private float fadeSeconds = 0.5f;
    [SerializeField] private int bannerFontSize = 44;

    [Header("Colours")]
    [SerializeField] private Color medkitColor = new Color(1f, 0.45f, 0.75f);
    [SerializeField] private Color sanityColor = new Color(0.3f, 0.85f, 1f);
    [SerializeField] private Color ammoColor = new Color(1f, 0.9f, 0.2f);
    [Tooltip("Colour used when the Thief appears.")]
    [SerializeField] private Color appearColor = new Color(1f, 0.25f, 0.25f);
    [Tooltip("Colour used when the Thief vanishes.")]
    [SerializeField] private Color vanishColor = new Color(0.65f, 0.6f, 0.75f);

    // One subtitle line: its wording before and after the coloured word, the coloured word, and when it appeared.
    private struct Line
    {
        public string Before;
        public string Highlight;
        public string After;
        public Color HighlightColor;
        public float ShownAt;
    }

    private readonly List<Line> lines = new List<Line>();
    private readonly StringBuilder builder = new StringBuilder();
    private ThiefAgent thief;
    private float shownAt = float.NegativeInfinity;
    private Color shownColor;

    // Subscribes to the Thief in Start, after every Awake, so the one ThiefAgent in the scene exists by then.
    private void Start()
    {
        ApplyLayout();
        if (messageText != null) messageText.text = string.Empty;
        SetBannerAlpha(0f);

        thief = FindAnyObjectByType<ThiefAgent>();
        if (thief == null)
        {
            Debug.LogWarning("ThiefMessages found no ThiefAgent in the scene, so no messages will show.", this);
            return;
        }

        thief.OnSpawned += ShowAppeared;
        thief.OnVanished += ShowVanished;
        thief.OnItemStolen += ShowStolen;
        thief.OnItemDelivered += ShowDelivered;
        thief.OnItemDropped += ShowDropped;
    }

    private void OnDestroy()
    {
        if (thief == null) return;

        thief.OnSpawned -= ShowAppeared;
        thief.OnVanished -= ShowVanished;
        thief.OnItemStolen -= ShowStolen;
        thief.OnItemDelivered -= ShowDelivered;
        thief.OnItemDropped -= ShowDropped;
    }

#if UNITY_EDITOR
    /*
     * Keeps the text where the settings put it when they are changed in the Inspector. The change happens
     * just after validation, since Unity doesn't allow layout changes during OnValidate itself.
     */
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) ApplyLayout();
        };
    }
#endif

    private void Update()
    {
        if (style == ThiefMessageStyle.Subtitle) UpdateSubtitles();
        else UpdateBanner();
    }

    // Messages

    private void ShowAppeared()
    {
        if (style == ThiefMessageStyle.Subtitle) AddLine("Thief ", "appeared", "", appearColor);
        else ShowBanner("The Thief has appeared!", appearColor);
    }

    private void ShowVanished()
    {
        if (style == ThiefMessageStyle.Subtitle) AddLine("Thief ", "vanished", "", vanishColor);
        else ShowBanner("The Thief has vanished", vanishColor);
    }

    private void ShowStolen(PickupInteractable item) => ShowItem("stole", "The Thief stole a {0}!", item);
    private void ShowDelivered(PickupInteractable item) => ShowItem("stashed", "The Thief stashed a {0}", item);
    private void ShowDropped(PickupInteractable item) => ShowItem("dropped", "The Thief dropped a {0}", item);

    private void ShowItem(string verb, string bannerFormat, PickupInteractable item)
    {
        string name = ItemName(item.Type);
        Color color = ItemColor(item.Type);

        if (style == ThiefMessageStyle.Subtitle) AddLine($"Thief {verb} ", name, "", color);
        else ShowBanner(string.Format(bannerFormat, name), color);
    }

    // Subtitle style

    private void AddLine(string before, string highlight, string after, Color highlightColor)
    {
        lines.Add(new Line { Before = before, Highlight = highlight, After = after, HighlightColor = highlightColor, ShownAt = Time.time });
        while (lines.Count > Mathf.Max(1, maxLines)) lines.RemoveAt(0);
    }

    // Drops finished lines and redraws the rest, newest at the bottom, each faded by its own age.
    private void UpdateSubtitles()
    {
        if (messageText == null) return;

        lines.RemoveAll(l => Time.time - l.ShownAt > subtitleHoldSeconds + subtitleFadeSeconds);

        builder.Clear();
        foreach (Line line in lines)
        {
            float alpha = FadeAlpha(Time.time - line.ShownAt, subtitleHoldSeconds, subtitleFadeSeconds);

            builder.Append("<mark=#").Append(Hex(subtitleBackdrop, subtitleBackdrop.a * alpha)).Append(">");
            builder.Append("<color=#").Append(Hex(subtitleTextColor, alpha)).Append(">  ").Append(line.Before);
            builder.Append("<color=#").Append(Hex(line.HighlightColor, alpha)).Append(">").Append(line.Highlight);
            builder.Append("<color=#").Append(Hex(subtitleTextColor, alpha)).Append(">").Append(line.After).Append("  ");
            builder.Append("</mark>\n");
        }

        messageText.text = builder.ToString();
    }

    // Banner style

    // Replaces the current banner with a new one in the given colour and restarts its timer.
    private void ShowBanner(string text, Color color)
    {
        if (messageText == null) return;

        messageText.text = text;
        shownColor = color;
        shownAt = Time.time;
        SetBannerAlpha(1f);
    }

    private void UpdateBanner()
    {
        SetBannerAlpha(FadeAlpha(Time.time - shownAt, holdSeconds, fadeSeconds));
    }

    private void SetBannerAlpha(float alpha)
    {
        if (messageText == null || style != ThiefMessageStyle.Banner) return;

        Color c = shownColor;
        c.a = Mathf.Clamp01(alpha);
        messageText.color = c;
    }

    // Layout

    /*
     * Positions and styles the text for the chosen style. Subtitle: anchored to the top-right corner, right-aligned,
     * regular weight, slight letter spacing, a soft vertical gradient and no outline. Banner: anchored at the top
     * centre, verticalPosition of the screen height down, large, bold, centred and outlined.
     */
    private void ApplyLayout()
    {
        if (messageText == null) return;

        RectTransform rect = messageText.rectTransform;

        if (style == ThiefMessageStyle.Subtitle)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-subtitleMargin.x, -subtitleMargin.y);
            rect.sizeDelta = new Vector2(640f, (subtitleFontSize + 14f) * Mathf.Max(1, maxLines));

            messageText.fontSize = subtitleFontSize;
            messageText.fontStyle = FontStyles.Normal;
            messageText.characterSpacing = 2f;
            messageText.lineSpacing = 12f;
            messageText.alignment = TextAlignmentOptions.TopRight;
            messageText.color = Color.white;
            messageText.enableVertexGradient = true;
            messageText.colorGradient = new VertexGradient(Color.white, Color.white,
                                                           new Color(0.78f, 0.76f, 0.74f), new Color(0.78f, 0.76f, 0.74f));
            if (Application.isPlaying) messageText.outlineWidth = 0f;
        }
        else
        {
            float y = 1f - verticalPosition;
            rect.anchorMin = new Vector2(0.5f, y);
            rect.anchorMax = new Vector2(0.5f, y);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1400f, 80f);

            messageText.fontSize = bannerFontSize;
            messageText.fontStyle = FontStyles.Bold;
            messageText.characterSpacing = 0f;
            messageText.lineSpacing = 0f;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.enableVertexGradient = false;
            if (Application.isPlaying)
            {
                messageText.outlineWidth = 0.2f;
                messageText.outlineColor = new Color32(0, 0, 0, 255);
            }
        }
    }

    // Helpers

    // 1 while holding, then down to 0 across the fade.
    private static float FadeAlpha(float age, float hold, float fade)
    {
        if (age <= hold) return 1f;
        return Mathf.Clamp01(1f - (age - hold) / Mathf.Max(fade, 0.01f));
    }

    private static string Hex(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return ColorUtility.ToHtmlStringRGBA(color);
    }

    private static string ItemName(PickupType type)
    {
        switch (type)
        {
            case PickupType.Health: return "Medkit";
            case PickupType.Sanity: return "Sanity Bottle";
            case PickupType.Ammo: return "Ammo Box";
            default: return "supply";
        }
    }

    private Color ItemColor(PickupType type)
    {
        switch (type)
        {
            case PickupType.Health: return medkitColor;
            case PickupType.Sanity: return sanityColor;
            case PickupType.Ammo: return ammoColor;
            default: return Color.white;
        }
    }
}
