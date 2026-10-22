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

        HunterNavigator nav = brain.Navigator;
        if (nav != null && nav.Route.Count > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 1; i < nav.Route.Count; i++)
                Gizmos.DrawLine(nav.Route[i - 1].Position + Vector3.up * 0.3f, nav.Route[i].Position + Vector3.up * 0.3f);

            if (nav.HoldIndex >= 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(nav.Route[nav.HoldIndex].Position + Vector3.up * 0.3f, 0.5f);
            }
        }

#if UNITY_EDITOR
        string target = ears.HasTarget ? $"{ears.Target.Type} ({ears.TargetScore:0.0})" : "none";
        Handles.Label(transform.position + Vector3.up * 2.5f, $"{brain.State}   hp {brain.Health:0}\nwalk: {(brain.Navigator != null ? brain.Navigator.Status.ToString() : "-")}   target: {target}");
#endif
    }
}
