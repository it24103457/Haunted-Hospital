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
 * Each Thief stash is labelled with its number and how it fared in the Thief's most recent stash choice: its chance
 * of being drawn, No route, or Skipped (last used). The stash the Thief is carrying an item to shows Target in the
 * Thief's magenta, and the stash of its last delivery shows Last used.
 */
[RequireComponent(typeof(Camera))]
public class ThiefTestWorldLabels : MonoBehaviour
{
    [Tooltip("Height above each door or barricade where its label is anchored.")]
    [SerializeField] private float labelHeight = 2.5f;
    [Tooltip("Height above each item where its label is anchored.")]
    [SerializeField] private float itemLabelHeight = 1f;
    [Tooltip("Height above each stash drop point where its label is anchored.")]
    [SerializeField] private float stashLabelHeight = 1.5f;
    [SerializeField] private int fontSize = 18;
    [Tooltip("Seconds between refreshes of the item list.")]
    [SerializeField] private float itemRefreshInterval = 0.25f;

    private static readonly Color MedkitColor = new Color(1f, 0.45f, 0.75f);
    private static readonly Color AmmoColor = new Color(1f, 0.9f, 0.2f);
    private static readonly Color SanityColor = new Color(0.3f, 0.85f, 1f);

    // Same magenta as the Thief's path line, state label and panel row.
    private static readonly Color ThiefColor = new Color(1f, 0.2f, 1f);

    private Camera view;
    private ThiefAgent thief;
    private Door[] doors;
    private ThiefTestBreakableBarricade[] barricades;
    private PickupInteractable[] items = new PickupInteractable[0];
    private readonly HashSet<PickupInteractable> dropped = new HashSet<PickupInteractable>();
    private float nextItemRefresh;
    private GUIStyle style;

    // The camera these labels are drawn through.
    private void Awake()
    {
        view = GetComponent<Camera>();
    }

    // Tracks dropped items through the pickup events, because an item doesn't remember that it was dropped.
    private void OnEnable()
    {
        PickupInteractable.AnyReleased += MarkDropped;
        PickupInteractable.AnyStolen += ClearDropped;
        PickupInteractable.AnyCollected += ClearDropped;
    }

    // Stops tracking, so a disabled overlay is never called by the pickup events.
    private void OnDisable()
    {
        PickupInteractable.AnyReleased -= MarkDropped;
        PickupInteractable.AnyStolen -= ClearDropped;
        PickupInteractable.AnyCollected -= ClearDropped;
    }

    // A dropped item keeps its Dropped label until it is stolen or collected again.
    private void MarkDropped(PickupInteractable item) => dropped.Add(item);
    private void ClearDropped(PickupInteractable item) => dropped.Remove(item);

    // Start runs after every Awake, so the breakable barricades added by ThiefTestProximityInteractor already exist.
    private void Start()
    {
        doors = FindObjectsByType<Door>(FindObjectsInactive.Exclude);
        barricades = FindObjectsByType<ThiefTestBreakableBarricade>(FindObjectsInactive.Exclude);
        thief = FindAnyObjectByType<ThiefAgent>();
    }

    // Refreshes the item list a few times a second instead of every frame, to keep the search cheap.
    private void Update()
    {
        if (Time.time < nextItemRefresh) return;

        nextItemRefresh = Time.time + itemRefreshInterval;
        items = FindObjectsByType<PickupInteractable>(FindObjectsInactive.Exclude);
    }

    // Draws a label over every door, barricade and item, coloured by its current state or type.
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

        DrawStashLabels();
    }

    /*
     * One label per Thief stash: "Stash" and its number, then its result in the Thief's most recent stash choice,
     * then Last used for the stash of its last delivery. The stash it is carrying an item to reads Target with its
     * chance, in magenta; a stash with no route is red and the last used one grey.
     */
    private void DrawStashLabels()
    {
        if (thief == null) return;

        bool heading = thief.IsCarrying && (thief.State == ThiefState.GoToStash || thief.State == ThiefState.WaitAtDoor);

        for (int i = 0; i < thief.StashCount; i++)
        {
            Transform dropPoint = thief.StashDropPoint(i);
            if (dropPoint == null) continue;

            float chance = thief.StashChance(i);
            bool isTarget = heading && i == thief.TargetStashIndex;
            bool isLast = i == thief.LastStashIndex;

            string result;
            Color color = Color.white;
            switch (thief.StashOutcomeOf(i))
            {
                case StashOutcome.Candidate: result = $"{chance:0%}"; break;
                case StashOutcome.NoRoute: result = "No route"; color = new Color(1f, 0.3f, 0.3f); break;
                case StashOutcome.SkippedLast: result = "Skipped (last used)"; color = Color.gray; break;
                default: result = "Not chosen yet"; break;
            }

            if (isTarget)
            {
                result = $"Target {chance:0%}";
                color = ThiefColor;
            }
            else if (isLast && thief.StashOutcomeOf(i) != StashOutcome.SkippedLast)
            {
                result += "\nLast used";
                color = Color.gray;
            }

            DrawLabel(dropPoint.position, $"Stash {i + 1}\n{result}", color, stashLabelHeight);
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
