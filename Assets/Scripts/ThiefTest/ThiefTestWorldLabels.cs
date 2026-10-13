using System.Collections.Generic;
using UnityEngine;

/*
 * Test-only overlay for the top-down Thief test scene.
 * Draws the name and current state of every door and barricade on top of the object itself, as seen
 * through this camera, so all of them stay visible while testing. States match what the agents read:
 * doors show Locked, Open (Door.IsOpen, also true while still swinging) or Closed;
 * barricades show Intact or Broken (deactivated).
 * Items are labelled in a colour per type (Medkit pink, Ammo yellow, Sanity cyan), with Claimed or Carried
 * added once someone has taken them, and Dropped once a Thief has let go of a stolen item until it is taken
 * again. Items come and go, so their list is refreshed a few times a second.
 */
[RequireComponent(typeof(Camera))]
public class ThiefTestWorldLabels : MonoBehaviour
{
    [Tooltip("Height above each door or barricade where its label is anchored.")]
    [SerializeField] private float labelHeight = 2.5f;
    [Tooltip("Height above each item where its label is anchored.")]
    [SerializeField] private float itemLabelHeight = 1f;
    [SerializeField] private int fontSize = 18;
    [Tooltip("Seconds between refreshes of the item list.")]
    [SerializeField] private float itemRefreshInterval = 0.25f;

    private static readonly Color MedkitColor = new Color(1f, 0.45f, 0.75f);
    private static readonly Color AmmoColor = new Color(1f, 0.9f, 0.2f);
    private static readonly Color SanityColor = new Color(0.3f, 0.85f, 1f);

    private Camera view;
    private Door[] doors;
    private ThiefTestBreakableBarricade[] barricades;
    private PickupInteractable[] items = new PickupInteractable[0];
    private readonly HashSet<PickupInteractable> dropped = new HashSet<PickupInteractable>();
    private float nextItemRefresh;
    private GUIStyle style;

    private void Awake()
    {
        view = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        PickupInteractable.AnyReleased += MarkDropped;
        PickupInteractable.AnyStolen += ClearDropped;
        PickupInteractable.AnyCollected += ClearDropped;
    }

    private void OnDisable()
    {
        PickupInteractable.AnyReleased -= MarkDropped;
        PickupInteractable.AnyStolen -= ClearDropped;
        PickupInteractable.AnyCollected -= ClearDropped;
    }

    private void MarkDropped(PickupInteractable item) => dropped.Add(item);
    private void ClearDropped(PickupInteractable item) => dropped.Remove(item);

    // Start runs after every Awake, so the breakable barricades added by ThiefTestProximityInteractor already exist.
    private void Start()
    {
        doors = FindObjectsByType<Door>(FindObjectsInactive.Exclude);
        barricades = FindObjectsByType<ThiefTestBreakableBarricade>(FindObjectsInactive.Exclude);
    }

    private void Update()
    {
        if (Time.time < nextItemRefresh) return;

        nextItemRefresh = Time.time + itemRefreshInterval;
        items = FindObjectsByType<PickupInteractable>(FindObjectsInactive.Exclude);
    }

    private void OnGUI()
    {
        if (doors == null) return;

        // A standalone style rather than a copy of the shared skin, so recolouring it per label affects nothing else.
        if (style == null) style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;

        foreach (Door door in doors)
        {
            if (door == null) continue;

            string state;
            Color color;
            if (door.IsLocked) { state = "Locked"; color = new Color(1f, 0.3f, 0.3f); }
            else if (door.IsOpen) { state = "Open"; color = new Color(0.3f, 1f, 0.3f); }
            else { state = "Closed"; color = Color.white; }

            DrawLabel(door.transform.position, $"{door.name}\n{state}", color, labelHeight);
        }

        foreach (ThiefTestBreakableBarricade barricade in barricades)
        {
            if (barricade == null) continue;

            bool intact = barricade.gameObject.activeInHierarchy;
            DrawLabel(barricade.transform.position, $"{barricade.name}\n{(intact ? "Intact" : "Broken")}",
                      intact ? new Color(1f, 0.6f, 0.1f) : Color.gray, labelHeight);
        }

        foreach (PickupInteractable item in items)
        {
            if (item == null) continue;

            string state = item.WasStolen ? "\nCarried"
                         : item.IsCollected ? "\nClaimed"
                         : dropped.Contains(item) ? "\nDropped"
                         : "";
            DrawLabel(item.transform.position, ItemName(item.Type) + state, ItemColor(item.Type), itemLabelHeight);
        }
    }

    // Display name per item type, shared with the other Thief test views.
    public static string ItemName(PickupType type)
    {
        switch (type)
        {
            case PickupType.Health: return "Medkit";
            case PickupType.Ammo: return "Ammo";
            case PickupType.Sanity: return "Sanity";
            default: return type.ToString();
        }
    }

    // Label colour per item type, shared with the other Thief test views.
    public static Color ItemColor(PickupType type)
    {
        switch (type)
        {
            case PickupType.Health: return MedkitColor;
            case PickupType.Ammo: return AmmoColor;
            case PickupType.Sanity: return SanityColor;
            default: return Color.white;
        }
    }

    // Text centred on the object's screen position, with a dark offset copy behind it for contrast on light floors.
    private void DrawLabel(Vector3 worldPosition, string text, Color color, float height)
    {
        Vector3 screen = view.WorldToScreenPoint(worldPosition + Vector3.up * height);
        if (screen.z < 0f) return;

        Rect rect = new Rect(screen.x - 150f, Screen.height - screen.y - 30f, 300f, 60f);

        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);

        style.normal.textColor = color;
        GUI.Label(rect, text, style);
    }
}
