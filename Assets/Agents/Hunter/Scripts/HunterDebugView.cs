using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/*
 * Test-only. While playing, draws what the Hunter remembers in the Scene view: a sphere and a label for each
 * remembered noise (yellow = current target), a line to the target, and the Hunter's state above its head.
 */
[RequireComponent(typeof(HunterEars), typeof(HunterBrain))]
public class HunterDebugView : MonoBehaviour
{
    [SerializeField] private bool showMemory = true;

    private HunterEars ears;
    private HunterBrain brain;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showMemory) return;

        if (ears == null) ears = GetComponent<HunterEars>();
        if (brain == null) brain = GetComponent<HunterBrain>();
        if (ears == null || brain == null) return;

        foreach (HeardNoise noise in ears.Memory)
        {
            float score = ears.ScoreOf(noise);
            bool isTarget = ears.HasTarget && ears.Target.Id == noise.Id;

            Gizmos.color = isTarget ? Color.yellow : Color.Lerp(Color.gray, Color.red, Mathf.InverseLerp(0f, 10f, score));
            Gizmos.DrawWireSphere(noise.Position, 0.3f + Mathf.Min(score, 10f) * 0.05f);

            if (isTarget) Gizmos.DrawLine(transform.position + Vector3.up, noise.Position);

#if UNITY_EDITOR
            Handles.Label(noise.Position + Vector3.up * 0.6f, $"{noise.Type}  score {score:0.0}");
#endif
        }

#if UNITY_EDITOR
        string target = ears.HasTarget ? $"{ears.Target.Type} ({ears.TargetScore:0.0})" : "none";
        Handles.Label(transform.position + Vector3.up * 2.5f, $"{brain.State}   hp {brain.Health:0}\ntarget: {target}");
#endif
    }
}
