using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/*
 * The How to Play screen: pages grouped into sections, so a player can jump straight to the part they want instead of
 * paging through everything in order. The sections are listed as blocks down the left side; clicking one shows its first
 * page. Previous/Next turn the pages of a section and carry on into the neighbouring section at either end, so the whole
 * guide can still be read straight through. The page counter counts within a section. The screen always opens on the
 * first section.
 * Each page has a title, some text and an optional picture: a turning 3D model, several models taking turns, or a still
 * image. A page can also show a HUD icon (health, ammo or sanity) in the top-right corner of its picture, so the page
 * links to the matching bar on the HUD.
 * The pages come from a plain text file so anyone can edit them without opening Unity:
 *   "[Name]" starts a section and "=== Title" starts a page in it.
 *   Right under a page's title, "model: Name" picks a model ("model: A, B, C" shows several, taking turns),
 *   "image: Name" picks a still image instead, and "icon: Health", "icon: Ammo" or "icon: Sanity" adds the HUD icon.
 *   The rest is the page text, and lines starting with # are notes that are never shown.
 * Model and image names are matched to assets through the Models and Images lists on this component.
 * The section blocks are made in the scene (Section Buttons) and filled in order; spare blocks are hidden.
 * A page without a picture gives its text the full width; a page without text shows a "coming soon" line.
 * Previous/Next (or the arrow keys, A/D) turn the page with a swipe sound, up/down (or W/S) change section, and
 * Back (or Esc) closes the screen and raises onBack.
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

    [Serializable]
    private class ImageEntry
    {
        public string name;
        public Texture texture;
    }

    private class Page
    {
        public string Title;
        public readonly List<string> Models = new List<string>();
        public string Image;
        public string Icon;
        public string Body;
    }

    private class Section
    {
        public string Name;
        public readonly List<Page> Pages = new List<Page>();
    }

    [SerializeField] private TextAsset content;
    [SerializeField] private ModelEntry[] models;
    [SerializeField] private ImageEntry[] images;
    [Tooltip("On a page with several models, how long each one shows before the next takes its turn.")]
    [SerializeField, Min(0.5f)] private float modelTurnSeconds = 3f;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text pageCounter;
    [SerializeField] private GameObject modelFrame;
    [SerializeField] private RectTransform textArea;
    [SerializeField] private ModelPreview preview;
    [SerializeField] private RawImage modelView;
    [SerializeField] private RawImage imageView;
    [SerializeField] private Image iconView;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button[] sectionButtons;

    [Header("Text area layout")]
    [SerializeField] private float textLeftWithModel = 1080f;
    [SerializeField] private float textLeftWithoutModel = 480f;

    [Header("Section blocks")]
    [SerializeField] private Color sectionColor = new Color(1f, 1f, 1f, 0.05f);
    [SerializeField] private Color sectionSelectedColor = new Color(1f, 1f, 1f, 0.14f);

    [Header("Icons (same colours as the HUD bars)")]
    [SerializeField] private Color healthIconColor = new Color(0.78f, 0.09f, 0.07f);
    [SerializeField] private Color ammoIconColor = new Color(0.85f, 0.66f, 0.2f);
    [SerializeField] private Color sanityIconColor = new Color(0.3f, 0.66f, 0.84f);

    [Header("Sound")]
    [SerializeField] private AudioClip pageTurn;
    [SerializeField, Range(0f, 1f)] private float pageTurnVolume = 0.7f;

    [SerializeField] private UnityEvent onBack;

    private const string ComingSoon = "<i>More information coming soon.</i>";

    private readonly List<Section> sections = new List<Section>();
    private int sectionIndex;
    private int index;
    private int modelTurn;
    private float nextModelTurn;
    private AudioSource audioSource;

    /*
     * Reads the sections from the text file, links each section block to its section, and makes the page-turn sound's
     * source, which plays even while the game is paused.
     */
    private void Awake()
    {
        Parse();
        SetUpSectionButtons();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.ignoreListenerPause = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    // Opens on the first page of the first section every time.
    private void OnEnable()
    {
        sectionIndex = 0;
        index = 0;
        ShowPage();
    }

    /*
     * Right arrow or D turns forward, left arrow or A back, up arrow or W and down arrow or S change section, and Esc
     * closes the screen. On a page with several models, the next one takes its turn once the current one has had its time.
     */
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) Next();
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame) Previous();
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) StepSection(1);
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) StepSection(-1);
            if (keyboard.escapeKey.wasPressedThisFrame) Back();
        }

        Page page = CurrentPage();
        if (page != null && page.Image == null && page.Models.Count > 1 && Time.unscaledTime >= nextModelTurn)
        {
            modelTurn = (modelTurn + 1) % page.Models.Count;
            ShowModel(page);
        }
    }

    // Turns to the next page; after a section's last page, to the first page of the next section. Nothing after the very last page.
    public void Next()
    {
        Section section = CurrentSection();
        if (section == null) return;

        if (index < section.Pages.Count - 1) index++;
        else if (sectionIndex < sections.Count - 1)
        {
            sectionIndex++;
            index = 0;
        }
        else return;

        TurnPage();
    }

    // Turns to the previous page; before a section's first page, to the last page of the previous section. Nothing before the very first page.
    public void Previous()
    {
        if (index > 0) index--;
        else if (sectionIndex > 0)
        {
            sectionIndex--;
            index = sections[sectionIndex].Pages.Count - 1;
        }
        else return;

        TurnPage();
    }

    // Shows the first page of the given section; used by the section blocks.
    public void OpenSection(int section)
    {
        if (section < 0 || section >= sections.Count || section == sectionIndex) return;
        sectionIndex = section;
        index = 0;
        ShowPage();
    }

    // Closes the screen and raises onBack, so the main menu shows its buttons again.
    public void Back()
    {
        gameObject.SetActive(false);
        onBack?.Invoke();
    }

    // Moves to the section above or below with the page-turn sound, since a key press has no click sound of its own.
    private void StepSection(int step)
    {
        int target = sectionIndex + step;
        if (target < 0 || target >= sections.Count) return;
        if (pageTurn != null) audioSource.PlayOneShot(pageTurn, pageTurnVolume);
        OpenSection(target);
    }

    // Plays the page-turn sound and shows the new page.
    private void TurnPage()
    {
        if (pageTurn != null) audioSource.PlayOneShot(pageTurn, pageTurnVolume);
        ShowPage();
    }

    // The section on screen, or nothing when the file has no pages.
    private Section CurrentSection() => sectionIndex < sections.Count ? sections[sectionIndex] : null;

    // The page on screen, or nothing when the file has no pages.
    private Page CurrentPage()
    {
        Section section = CurrentSection();
        return section != null && index < section.Pages.Count ? section.Pages[index] : null;
    }

    /*
     * Fills in the current page's title, text, page counter (counted within its section), picture, icon and arrow
     * buttons, and highlights its section's block; with no pages it shows the coming-soon text.
     */
    private void ShowPage()
    {
        HighlightSection();

        Page page = CurrentPage();
        if (page == null)
        {
            titleText.text = "How to Play";
            bodyText.text = ComingSoon;
            pageCounter.text = string.Empty;
            SetPicture(null);
            return;
        }

        int count = sections[sectionIndex].Pages.Count;
        titleText.text = page.Title;
        bodyText.text = string.IsNullOrWhiteSpace(page.Body) ? ComingSoon : page.Body;
        pageCounter.text = (index + 1) + " / " + count;
        SetPicture(page);

        if (previousButton != null) previousButton.interactable = index > 0 || sectionIndex > 0;
        if (nextButton != null) nextButton.interactable = index < count - 1 || sectionIndex < sections.Count - 1;
    }

    /*
     * Shows the page's picture: its still image if it names one, otherwise its first model. With neither, the frame is
     * hidden and the text gets the full width. The HUD icon is shown on top when the page asks for one.
     */
    private void SetPicture(Page page)
    {
        Texture image = page != null ? FindImage(page.Image) : null;
        bool hasModel = page != null && image == null && FindModel(page.Models.Count > 0 ? page.Models[0] : null) != null;
        bool hasPicture = image != null || hasModel;

        if (modelFrame != null) modelFrame.SetActive(hasPicture);
        if (imageView != null)
        {
            imageView.gameObject.SetActive(image != null);
            imageView.texture = image;
        }
        if (modelView != null) modelView.gameObject.SetActive(hasModel);

        modelTurn = 0;
        if (hasModel) ShowModel(page);
        else if (preview != null) preview.Show(null, Vector3.zero);

        SetIcon(page != null ? page.Icon : null);
        if (textArea != null) textArea.offsetMin = new Vector2(hasPicture ? textLeftWithModel : textLeftWithoutModel, textArea.offsetMin.y);
    }

    // Shows the page's model whose turn it is, and times when the next one takes over.
    private void ShowModel(Page page)
    {
        ModelEntry entry = FindModel(page.Models[modelTurn]);
        if (preview != null) preview.Show(entry != null ? entry.prefab : null, entry != null ? entry.rotation : Vector3.zero);
        nextModelTurn = Time.unscaledTime + modelTurnSeconds;
    }

    // Shows the HUD icon for health, ammo or sanity in its bar's colour, or hides it for any other page.
    private void SetIcon(string icon)
    {
        if (iconView == null) return;

        Sprite sprite = null;
        Color color = Color.white;
        if (string.Equals(icon, "Health", StringComparison.OrdinalIgnoreCase)) { sprite = HudIcons.Heart; color = healthIconColor; }
        else if (string.Equals(icon, "Ammo", StringComparison.OrdinalIgnoreCase)) { sprite = HudIcons.Bullet; color = ammoIconColor; }
        else if (string.Equals(icon, "Sanity", StringComparison.OrdinalIgnoreCase)) { sprite = HudIcons.Bottle; color = sanityIconColor; }
        else if (!string.IsNullOrEmpty(icon)) Debug.LogWarning("How to Play: no icon named '" + icon + "' (use Health, Ammo or Sanity).", this);

        iconView.gameObject.SetActive(sprite != null);
        iconView.sprite = sprite;
        iconView.color = color;
    }

    /*
     * Gives each section block its section's name and a click that opens it, in the order the sections are written.
     * Blocks beyond the number of sections are hidden; sections beyond the number of blocks are only reachable with the keys.
     */
    private void SetUpSectionButtons()
    {
        if (sectionButtons == null) return;

        for (int i = 0; i < sectionButtons.Length; i++)
        {
            Button button = sectionButtons[i];
            if (button == null) continue;

            bool used = i < sections.Count;
            button.gameObject.SetActive(used);
            if (!used) continue;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = sections[i].Name;
            int section = i;
            button.onClick.AddListener(() => OpenSection(section));
        }

        if (sections.Count > sectionButtons.Length)
            Debug.LogWarning("How to Play: " + sections.Count + " sections but only " + sectionButtons.Length + " section blocks.", this);
    }

    // Lights up the current section's block (brighter background and its accent stripe) and dims the rest.
    private void HighlightSection()
    {
        if (sectionButtons == null) return;

        for (int i = 0; i < sectionButtons.Length; i++)
        {
            Button button = sectionButtons[i];
            if (button == null) continue;

            bool selected = i == sectionIndex;
            Image block = button.GetComponent<Image>();
            if (block != null) block.color = selected ? sectionSelectedColor : sectionColor;
            Transform accent = button.transform.Find("Accent");
            if (accent != null) accent.gameObject.SetActive(selected);
        }
    }

    // The Models list entry with the given name (ignoring case), or nothing, with a warning, when no entry matches.
    private ModelEntry FindModel(string name)
    {
        if (string.IsNullOrEmpty(name) || models == null) return null;

        foreach (var entry in models)
            if (string.Equals(entry.name, name, StringComparison.OrdinalIgnoreCase)) return entry.prefab != null ? entry : null;

        Debug.LogWarning("How to Play: no model named '" + name + "' in the Models list.", this);
        return null;
    }

    // The Images list texture with the given name (ignoring case), or nothing, with a warning, when no entry matches.
    private Texture FindImage(string name)
    {
        if (string.IsNullOrEmpty(name) || images == null) return null;

        foreach (var entry in images)
            if (string.Equals(entry.name, name, StringComparison.OrdinalIgnoreCase)) return entry.texture;

        Debug.LogWarning("How to Play: no image named '" + name + "' in the Images list.", this);
        return null;
    }

    /*
     * Reads the sections and pages from the text file; see the class comment for the format.
     * Pages written before the first section line go into an unnamed first section, so an old file without sections
     * still works.
     */
    private void Parse()
    {
        sections.Clear();
        if (content == null) return;

        Section section = null;
        Page page = null;
        var body = new StringBuilder();

        foreach (string raw in content.text.Replace("\r", "").Split('\n'))
        {
            string line = raw.TrimEnd();
            if (line.StartsWith("#")) continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                Finish(section, page, body);
                page = null;
                section = new Section { Name = line.Substring(1, line.Length - 2).Trim() };
                sections.Add(section);
                continue;
            }

            if (line.StartsWith("==="))
            {
                Finish(section, page, body);
                if (section == null)
                {
                    section = new Section { Name = "How to Play" };
                    sections.Add(section);
                }
                page = new Page { Title = line.Substring(3).Trim() };
                body.Clear();
                continue;
            }

            if (page == null) continue;

            if (body.Length == 0 && ReadSetting(page, line)) continue;

            body.Append(line).Append('\n');
        }

        Finish(section, page, body);
        sections.RemoveAll(s => s.Pages.Count == 0);
    }

    // Reads a "model:", "image:" or "icon:" line under a page's title; false for any other line, which is page text.
    private static bool ReadSetting(Page page, string line)
    {
        if (line.StartsWith("model:", StringComparison.OrdinalIgnoreCase))
        {
            foreach (string name in line.Substring(6).Split(','))
                if (name.Trim().Length > 0) page.Models.Add(name.Trim());
            return true;
        }
        if (line.StartsWith("image:", StringComparison.OrdinalIgnoreCase))
        {
            page.Image = line.Substring(6).Trim();
            return true;
        }
        if (line.StartsWith("icon:", StringComparison.OrdinalIgnoreCase))
        {
            page.Icon = line.Substring(5).Trim();
            return true;
        }
        return false;
    }

    // Stores a page in its section once all its text lines have been read.
    private static void Finish(Section section, Page page, StringBuilder body)
    {
        if (section == null || page == null) return;
        page.Body = body.ToString().Trim();
        section.Pages.Add(page);
    }
}
