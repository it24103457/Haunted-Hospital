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

    [Header("Sound")]
    [SerializeField] private AudioClip pageTurn;
    [SerializeField, Range(0f, 1f)] private float pageTurnVolume = 0.7f;

    [SerializeField] private UnityEvent onBack;

    private const string ComingSoon = "<i>More information coming soon.</i>";

    private readonly List<Page> pages = new List<Page>();
    private int index;
    private AudioSource audioSource;

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
