using UnityEngine;

/*
 * Test-only overlay for the top-down Thief test scene.
 * Draws the name and current state of every door and barricade on top of the object itself, as seen
 * through this camera, so all of them stay visible while testing. States match what the agents read:
 * doors show Locked, Open (Door.IsOpen, also true while still swinging) or Closed;
 * barricades show Intact or Broken (deactivated).
 */
[RequireComponent(typeof(Camera))]
public class ThiefTestWorldLabels : MonoBehaviour
{
    [Tooltip("Height above each object's position where its label is anchored.")]
    [SerializeField] private float labelHeight = 2.5f;
    [SerializeField] private int fontSize = 18;

    private Camera view;
    private Door[] doors;
    private ThiefTestBreakableBarricade[] barricades;
    private GUIStyle style;

    private void Awake()
    {
        view = GetComponent<Camera>();
    }

    // Start runs after every Awake, so the breakable barricades added by ThiefTestProximityInteractor already exist.
    private void Start()
    {
        doors = FindObjectsByType<Door>(FindObjectsInactive.Exclude);
        barricades = FindObjectsByType<ThiefTestBreakableBarricade>(FindObjectsInactive.Exclude);
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

            DrawLabel(door.transform.position, $"{door.name}\n{state}", color);
        }

        foreach (ThiefTestBreakableBarricade barricade in barricades)
        {
            if (barricade == null) continue;

            bool intact = barricade.gameObject.activeInHierarchy;
            DrawLabel(barricade.transform.position, $"{barricade.name}\n{(intact ? "Intact" : "Broken")}",
                      intact ? new Color(1f, 0.6f, 0.1f) : Color.gray);
        }
    }

    // Text centred on the object's screen position, with a dark offset copy behind it for contrast on light floors.
    private void DrawLabel(Vector3 worldPosition, string text, Color color)
    {
        Vector3 screen = view.WorldToScreenPoint(worldPosition + Vector3.up * labelHeight);
        if (screen.z < 0f) return;

        Rect rect = new Rect(screen.x - 150f, Screen.height - screen.y - 30f, 300f, 60f);

        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);

        style.normal.textColor = color;
        GUI.Label(rect, text, style);
    }
}
