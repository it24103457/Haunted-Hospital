using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/*
 * The How to Play screen: a set of pages, each with a title, some text and an optional turning 3D model.
 * The pages come from a plain text file so anyone can edit them without opening Unity:
 *   "=== Title" starts a page, an optional "model: Name" line under it picks a model, the rest is the page text,
 *   and lines starting with # are notes that are never shown.
 * A key written in square brackets, such as [W] or [Shift], is drawn as a key cap. A page that uses key caps is laid out
 * as rows, one per line: each line's key caps and words appear in order, and blank lines leave a small gap.
 * Model names are matched to prefabs through the Models list on this component.
 * A page without a model gives its text the full width; a page without text shows a "coming soon" line.
 * Previous/Next (or the arrow keys, A/D) turn the page with a swipe sound; Back (or Esc) closes the screen and raises onBack.
 */
public class HowToPlayMenu : MonoBehaviour
{
    [Serializable]
    private class ModelEntry
    {
        public string name;
        public GameObject prefab;
        [Tooltip("Extra rotation for this model, e.g. to turn its front towards the camera.")]
        public Vector3 rotation;
    }

    private class Page
    {
        public string Title;
        public string Model;
        public string Body;
    }

    [SerializeField] private TextAsset content;
    [SerializeField] private ModelEntry[] models;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text pageCounter;
    [SerializeField] private GameObject modelFrame;
    [SerializeField] private RectTransform textArea;
    [SerializeField] private ModelPreview preview;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Header("Text area layout")]
    [SerializeField] private float textLeftWithModel = 860f;
    [SerializeField] private float textLeftWithoutModel = 160f;

    [Header("Key caps")]
    [SerializeField] private Color keyFill = new Color(0.1f, 0.1f, 0.11f, 0.95f);
    [SerializeField] private Color keyBorder = new Color(0.82f, 0.82f, 0.78f, 0.7f);
    [SerializeField] private Color keyLabel = new Color(0.95f, 0.95f, 0.92f, 1f);
    [Tooltip("Height of each row of keys, as a multiple of the page text size.")]
    [SerializeField, Min(1f)] private float keyRowHeight = 1.9f;

    [Header("Sound")]
    [SerializeField] private AudioClip pageTurn;
    [SerializeField, Range(0f, 1f)] private float pageTurnVolume = 0.7f;

    [SerializeField] private UnityEvent onBack;

    private const string ComingSoon = "<i>More information coming soon.</i>";

    private readonly List<Page> pages = new List<Page>();
    private int index;
    private AudioSource audioSource;
    private RectTransform keyRows;

    // A [Key] written in the page text.
    private static readonly System.Text.RegularExpressions.Regex KeyPattern = new System.Text.RegularExpressions.Regex(@"\[([^\]]+)\]");

    private void Awake()
    {
        Parse();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.ignoreListenerPause = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        index = 0;
        ShowPage();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Next();
        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Previous();
        if (keyboard.escapeKey.wasPressedThisFrame) Back();
    }

    public void Next()
    {
        if (index >= pages.Count - 1) return;
        index++;
        TurnPage();
    }

    public void Previous()
    {
        if (index <= 0) return;
        index--;
        TurnPage();
    }

    public void Back()
    {
        gameObject.SetActive(false);
        onBack?.Invoke();
    }

    private void TurnPage()
    {
        if (pageTurn != null) audioSource.PlayOneShot(pageTurn, pageTurnVolume);
        ShowPage();
    }

    private void ShowPage()
    {
        if (pages.Count == 0)
        {
            titleText.text = "How to Play";
            bodyText.text = ComingSoon;
            pageCounter.text = string.Empty;
            SetModel(null);
            return;
        }

        Page page = pages[index];
        titleText.text = page.Title;
        bodyText.text = string.IsNullOrWhiteSpace(page.Body) ? ComingSoon : page.Body;
        pageCounter.text = (index + 1) + " / " + pages.Count;
        SetModel(FindModel(page.Model));

        // Pages with [Key] caps are drawn as rows of keys and words instead of plain text.
        bool hasKeys = !string.IsNullOrEmpty(page.Body) && KeyPattern.IsMatch(page.Body);
        bodyText.gameObject.SetActive(!hasKeys);
        if (hasKeys) BuildKeyRows(page.Body);
        else if (keyRows != null) keyRows.gameObject.SetActive(false);

        if (previousButton != null) previousButton.interactable = index > 0;
        if (nextButton != null) nextButton.interactable = index < pages.Count - 1;
    }

    private void SetModel(ModelEntry entry)
    {
        bool hasModel = entry != null && entry.prefab != null;
        if (modelFrame != null) modelFrame.SetActive(hasModel);
        if (preview != null) preview.Show(hasModel ? entry.prefab : null, hasModel ? entry.rotation : Vector3.zero);
        if (textArea != null) textArea.offsetMin = new Vector2(hasModel ? textLeftWithModel : textLeftWithoutModel, textArea.offsetMin.y);
    }

    /*
     * Lays the page text out as rows, one per line, in the same area and style as the plain page text.
     * Each [Key] becomes a key cap: a dark box with a light border and the key's name on it.
     * The words between keys are drawn as plain text, and a blank line becomes a half-height gap.
     */
    private void BuildKeyRows(string body)
    {
        if (keyRows == null)
        {
            keyRows = new GameObject("KeyRows", typeof(RectTransform)).GetComponent<RectTransform>();
            keyRows.SetParent(bodyText.transform.parent, false);
            var source = bodyText.rectTransform;
            keyRows.anchorMin = source.anchorMin;
            keyRows.anchorMax = source.anchorMax;
            keyRows.pivot = source.pivot;
            keyRows.offsetMin = source.offsetMin;
            keyRows.offsetMax = source.offsetMax;

            var column = keyRows.gameObject.AddComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.UpperLeft;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;
            column.spacing = bodyText.fontSize * 0.35f;
        }

        keyRows.gameObject.SetActive(true);
        for (int i = keyRows.childCount - 1; i >= 0; i--) Destroy(keyRows.GetChild(i).gameObject);

        float rowHeight = bodyText.fontSize * keyRowHeight;
        foreach (string line in body.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                var gap = new GameObject("Gap", typeof(RectTransform), typeof(LayoutElement));
                gap.transform.SetParent(keyRows, false);
                gap.GetComponent<LayoutElement>().preferredHeight = rowHeight * 0.5f;
                continue;
            }

            var row = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
            row.SetParent(keyRows, false);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.spacing = bodyText.fontSize * 0.4f;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = rowHeight;

            int last = 0;
            foreach (System.Text.RegularExpressions.Match m in KeyPattern.Matches(line))
            {
                AddWords(row, line.Substring(last, m.Index - last));
                AddKeyCap(row, m.Groups[1].Value.Trim(), rowHeight);
                last = m.Index + m.Length;
            }
            AddWords(row, line.Substring(last));
        }
    }

    private void AddWords(RectTransform row, string words)
    {
        words = words.Trim();
        if (words.Length == 0) return;

        var label = new GameObject("Words", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(row, false);
        label.font = bodyText.font;
        label.fontSize = bodyText.fontSize;
        label.color = bodyText.color;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        label.text = words;
    }

    // A key cap sized to its label, never narrower than it is tall, with a border drawn as a slightly larger box behind the fill.
    private void AddKeyCap(RectTransform row, string key, float rowHeight)
    {
        float capHeight = rowHeight * 0.88f;
        float border = Mathf.Max(2f, capHeight * 0.05f);

        var cap = new GameObject("Key " + key, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        cap.transform.SetParent(row, false);
        cap.GetComponent<Image>().color = keyBorder;
        cap.GetComponent<Image>().raycastTarget = false;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        fill.SetParent(cap.transform, false);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = new Vector2(border, border);
        fill.offsetMax = new Vector2(-border, -border);
        fill.GetComponent<Image>().color = keyFill;
        fill.GetComponent<Image>().raycastTarget = false;

        var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.rectTransform.SetParent(cap.transform, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        label.font = bodyText.font;
        label.fontSize = bodyText.fontSize * 0.8f;
        label.fontStyle = FontStyles.Bold;
        label.color = keyLabel;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.text = key;

        var size = cap.GetComponent<LayoutElement>();
        size.preferredHeight = capHeight;
        size.preferredWidth = Mathf.Max(capHeight, label.GetPreferredValues(key).x + capHeight * 0.7f);
    }

    private ModelEntry FindModel(string name)
    {
        if (string.IsNullOrEmpty(name) || models == null) return null;

        foreach (var entry in models)
            if (string.Equals(entry.name, name, StringComparison.OrdinalIgnoreCase)) return entry;

        Debug.LogWarning("How to Play: no model named '" + name + "' in the Models list.", this);
        return null;
    }

    // Reads the pages from the text file; see the class comment for the format.
    private void Parse()
    {
        pages.Clear();
        if (content == null) return;

        Page page = null;
        var body = new StringBuilder();

        foreach (string raw in content.text.Replace("\r", "").Split('\n'))
        {
            string line = raw.TrimEnd();
            if (line.StartsWith("#")) continue;

            if (line.StartsWith("==="))
            {
                Finish(page, body);
                page = new Page { Title = line.Substring(3).Trim() };
                body.Clear();
                continue;
            }

            if (page == null) continue;

            if (body.Length == 0 && page.Model == null && line.StartsWith("model:", StringComparison.OrdinalIgnoreCase))
            {
                page.Model = line.Substring(6).Trim();
                continue;
            }

            body.Append(line).Append('\n');
        }

        Finish(page, body);
    }

    private void Finish(Page page, StringBuilder body)
    {
        if (page == null) return;
        page.Body = body.ToString().Trim();
        pages.Add(page);
    }
}
