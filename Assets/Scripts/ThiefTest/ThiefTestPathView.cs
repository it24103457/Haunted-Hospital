using System.Collections.Generic;
using UnityEngine;

/*
 * Test-only debug view for the Thief in the Thief test scene.
 * Draws ThiefAgent.CurrentPath as a line from the Thief through every path point, and shows its State above
 * it, as seen through the scene's top-down camera. While it carries an item the label adds which one
 * (for example "carrying Medkit"), read from the item held under the Thief's CarrySocket.
 * The path and state come only from the output contract, so they show exactly what the Agent Controller receives.
 */
[RequireComponent(typeof(ThiefAgent))]
public class ThiefTestPathView : MonoBehaviour
{
    [Tooltip("Height of the path line: above furniture, so the top-down camera sees it, and below doorway tops.")]
    [SerializeField] private float lineHeight = 2f;
    [SerializeField] private float lineWidth = 0.15f;
    [SerializeField] private Color lineColor = new Color(1f, 0.2f, 1f);
    [Tooltip("Height above the Thief where its state label is anchored.")]
    [SerializeField] private float labelHeight = 2.2f;
    [SerializeField] private int fontSize = 20;

    private ThiefAgent thief;
    private Transform carrySocket;
    private LineRenderer line;
    private Camera view;
    private GUIStyle style;
    private readonly List<Vector3> points = new List<Vector3>();

    // Finds the CarrySocket and builds the unlit path line once, so drawing it each frame only updates its points.
    private void Awake()
    {
        thief = GetComponent<ThiefAgent>();
        // Searched under the whole Thief, because the socket can sit under a bone of the rigged model.
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == "CarrySocket") { carrySocket = t; break; }

        // The line lives on its own child so it never interferes with the Thief's colliders or model.
        GameObject lineObject = new GameObject("PathLine");
        lineObject.transform.SetParent(transform, false);
        line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.widthMultiplier = lineWidth;
        line.positionCount = 0;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        line.material.SetColor("_BaseColor", lineColor);
    }

    // The label is drawn through the camera that renders last, which is the top-down camera in the test scene.
    private void Start()
    {
        foreach (Camera c in Camera.allCameras)
            if (view == null || c.depth > view.depth) view = c;
    }

    // Redraws the line after the Thief has moved this frame, from the Thief through the path points still ahead.
    private void LateUpdate()
    {
        IReadOnlyList<Vector3> path = thief.CurrentPath;

        points.Clear();
        if (path.Count > 0)
        {
            points.Add(Lift(transform.position));
            for (int i = FirstPointAhead(path); i < path.Count; i++) points.Add(Lift(path[i]));
        }

        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
    }

    // Raises a point to the line height, so the line stays visible above furniture from the top-down camera.
    private Vector3 Lift(Vector3 p) => new Vector3(p.x, lineHeight, p.z);

    /*
     * Index of the first path point still ahead of the Thief. A published path keeps the points the Thief has
     * already walked past, so the line starts at the end of the path piece the Thief is currently closest to,
     * measured flat, instead of drawing back to points behind it.
     */
    private int FirstPointAhead(IReadOnlyList<Vector3> path)
    {
        if (path.Count < 2) return 0;

        Vector2 here = new Vector2(transform.position.x, transform.position.z);
        int bestIndex = 0;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2 a = new Vector2(path[i].x, path[i].z);
            Vector2 b = new Vector2(path[i + 1].x, path[i + 1].z);
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(here - a, ab) / ab.sqrMagnitude) : 0f;
            float distance = Vector2.Distance(here, a + t * ab);

            if (distance >= bestDistance) continue;
            bestDistance = distance;
            bestIndex = i + 1;
        }

        return bestIndex;
    }

    // Name of the item held under the CarrySocket, or "item" when it can't be seen there.
    private string CarriedItemName()
    {
        PickupInteractable item = carrySocket != null ? carrySocket.GetComponentInChildren<PickupInteractable>() : null;
        return item != null ? ThiefTestWorldLabels.ItemName(item.Type) : "item";
    }

    // Draws the state label above the Thief, with a dark shadow so it stays readable on any floor.
    private void OnGUI()
    {
        if (view == null) return;

        Vector3 screen = view.WorldToScreenPoint(transform.position + Vector3.up * labelHeight);
        if (screen.z < 0f) return;

        if (style == null) style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;

        string text = thief.IsCarrying ? $"{thief.State}\ncarrying {CarriedItemName()}" : thief.State.ToString();
        Rect rect = new Rect(screen.x - 150f, Screen.height - screen.y - 30f, 300f, 60f);

        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
        style.normal.textColor = lineColor;
        GUI.Label(rect, text, style);
    }
}
