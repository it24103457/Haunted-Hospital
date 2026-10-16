using System.Collections.Generic;
using UnityEngine;


// Test-only helper. Listens to every noise and, for a few seconds, draws how far it spread through the
// waypoint graph: green nodes heard it loudly, red nodes only just heard it. Lets you check closed doors,
// barricades and walls before the Hunter exists. Put it on any object in the scene.
// Look at it in the Scene view (or switch on Gizmos in the Game view) while the game is playing.

public class NoiseDebugView : MonoBehaviour
{
    [SerializeField] private WaypointGraph graph;
    [SerializeField] private SoundPropagationSettings settings = new SoundPropagationSettings();
    [Tooltip("The quietest loudness that still counts as heard.")]
    [SerializeField, Min(0.01f)] private float hearingThreshold = 1f;
    [SerializeField, Min(0.5f)] private float showSeconds = 4f;
    [SerializeField] private bool logEvents = true;

    private readonly Dictionary<WaypointNode, float> heard = new Dictionary<WaypointNode, float>();
    private NoiseEvent last;
    private float hideTime = -1f;

    private void Awake()
    {
        if (graph == null) graph = FindAnyObjectByType<WaypointGraph>();
    }

    private void OnEnable()
    {
        Noise.Emitted += OnNoise;
    }

    private void OnDisable()
    {
        Noise.Emitted -= OnNoise;
    }

    private void OnNoise(NoiseEvent noise)
    {
        if (graph == null) return;

        SoundPropagation.Propagate(graph, noise.Position, noise.Loudness, hearingThreshold, settings, heard);

        last = noise;
        hideTime = Time.time + showSeconds;

        if (logEvents)
            Debug.Log($"[Noise] {noise.Type} (loudness {noise.Loudness:0.#}) reached {heard.Count} nodes.", this);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || Time.time > hideTime) return;

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(last.Position, 0.5f);

        foreach (KeyValuePair<WaypointNode, float> pair in heard)
        {
            if (pair.Key == null) continue;

            // 0 = barely heard (red), 1 = as loud as the source (green).
            float t = Mathf.InverseLerp(hearingThreshold, last.Loudness, pair.Value);
            Gizmos.color = Color.Lerp(Color.red, Color.green, t);
            Gizmos.DrawSphere(pair.Key.Position + Vector3.up * 0.3f, 0.2f + 0.2f * t);
        }
    }
}
