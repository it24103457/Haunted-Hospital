using System.Collections.Generic;
using UnityEngine;

/*
 * Test-only debug view for the Thief in the Thief test scene.
 * Draws ThiefAgent.CurrentPath as a line from the Thief through every path point, and shows its State
 * (plus "carrying" while it holds an item) above it, as seen through the scene's top-down camera.
 * Reads only the output contract, so it shows exactly what the Agent Controller would receive.
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
    private LineRenderer line;
    private Camera view;
    private GUIStyle style;
    private readonly List<Vector3> points = new List<Vector3>();

    private void Awake()
    {
        thief = GetComponent<ThiefAgent>();

        /* The line lives on its own child so it never interferes with the Thief's colliders or model. */
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

    private void LateUpdate()
    {
        IReadOnlyList<Vector3> path = thief.CurrentPath;

        points.Clear();
        if (path.Count > 0)
        {
            points.Add(Lift(transform.position));
            foreach (Vector3 p in path) points.Add(Lift(p));
        }

        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
    }

    private Vector3 Lift(Vector3 p) => new Vector3(p.x, lineHeight, p.z);

    private void OnGUI()
    {
        if (view == null) return;

        Vector3 screen = view.WorldToScreenPoint(transform.position + Vector3.up * labelHeight);
        if (screen.z < 0f) return;

        if (style == null) style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;

        string text = thief.IsCarrying ? $"{thief.State}\ncarrying" : thief.State.ToString();
        Rect rect = new Rect(screen.x - 150f, Screen.height - screen.y - 30f, 300f, 60f);

        style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
        style.normal.textColor = lineColor;
        GUI.Label(rect, text, style);
    }
}
