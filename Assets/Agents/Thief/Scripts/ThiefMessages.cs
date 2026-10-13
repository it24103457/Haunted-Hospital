using TMPro;
using UnityEngine;

/*
 * In-game HUD message for what the Thief does, shown at the top centre of the screen:
 *   "The Thief stole a Medkit!", "The Thief stashed a Medkit", "The Thief dropped a Medkit".
 * Listens to the scene's one ThiefAgent (OnItemStolen, OnItemDelivered, OnItemDropped), tints each message
 * with the item's colour, holds it for a moment and fades it out. A new message replaces the current one.
 * Lives on the PF_ThiefMessages prefab (a screen-space Canvas with one TextMeshPro text), separate from PF_Thief.
 * The vertical position slider moves the message up or down the screen without editing the Canvas.
 */
public class ThiefMessages : MonoBehaviour
{
    [Tooltip("The text that shows the messages.")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Tooltip("Distance of the message from the top of the screen, as a fraction of the screen height.")]
    [Range(0f, 1f)]
    [SerializeField] private float verticalPosition = 0.12f;

    [Tooltip("Seconds a message stays fully visible before it starts to fade.")]
    [SerializeField] private float holdSeconds = 2f;
    [Tooltip("Seconds a message takes to fade out.")]
    [SerializeField] private float fadeSeconds = 0.5f;

    [Header("Item colours")]
    [SerializeField] private Color medkitColor = new Color(1f, 0.45f, 0.75f);
    [SerializeField] private Color sanityColor = new Color(0.3f, 0.85f, 1f);
    [SerializeField] private Color ammoColor = new Color(1f, 0.9f, 0.2f);

    private ThiefAgent thief;
    private float shownAt = float.NegativeInfinity;
    private Color shownColor;

    // Subscribes to the Thief in Start, after every Awake, so the one ThiefAgent in the scene exists by then.
    private void Start()
    {
        ApplyPosition();
        SetAlpha(0f);

        thief = FindAnyObjectByType<ThiefAgent>();
        if (thief == null)
        {
            Debug.LogWarning("ThiefMessages found no ThiefAgent in the scene, so no messages will show.", this);
            return;
        }

        thief.OnItemStolen += ShowStolen;
        thief.OnItemDelivered += ShowDelivered;
        thief.OnItemDropped += ShowDropped;
    }

    private void OnDestroy()
    {
        if (thief == null) return;

        thief.OnItemStolen -= ShowStolen;
        thief.OnItemDelivered -= ShowDelivered;
        thief.OnItemDropped -= ShowDropped;
    }

#if UNITY_EDITOR
    /*
     * Keeps the message where the slider puts it when the value is changed in the Inspector. The move happens
     * just after validation, since Unity doesn't allow layout changes during OnValidate itself.
     */
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) ApplyPosition();
        };
    }
#endif

    private void Update()
    {
        float age = Time.time - shownAt;
        if (age > holdSeconds + fadeSeconds) { SetAlpha(0f); return; }

        float alpha = age <= holdSeconds ? 1f : 1f - (age - holdSeconds) / Mathf.Max(fadeSeconds, 0.01f);
        SetAlpha(alpha);
    }

    private void ShowStolen(PickupInteractable item) => Show($"The Thief stole a {ItemName(item.Type)}!", item.Type);
    private void ShowDelivered(PickupInteractable item) => Show($"The Thief stashed a {ItemName(item.Type)}", item.Type);
    private void ShowDropped(PickupInteractable item) => Show($"The Thief dropped a {ItemName(item.Type)}", item.Type);

    // Replaces the current message with a new one in the item's colour and restarts its timer.
    private void Show(string text, PickupType type)
    {
        if (messageText == null) return;

        messageText.text = text;
        shownColor = ItemColor(type);
        shownAt = Time.time;
        SetAlpha(1f);
    }

    private void SetAlpha(float alpha)
    {
        if (messageText == null) return;

        Color c = shownColor;
        c.a = Mathf.Clamp01(alpha);
        messageText.color = c;
    }

    // Anchors the text to the top centre, verticalPosition of the screen height down from the top.
    private void ApplyPosition()
    {
        if (messageText == null) return;

        RectTransform rect = messageText.rectTransform;
        float y = 1f - verticalPosition;
        rect.anchorMin = new Vector2(0.5f, y);
        rect.anchorMax = new Vector2(0.5f, y);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
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
