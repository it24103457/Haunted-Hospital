using System.Collections;
using UnityEngine;

/*
 * Makes a ceiling lamp flicker like a failing fluorescent tube.
 * The lamp stays on with a faint shimmer for a random while, then stutters off and on a few times,
 * and sometimes ends the stutter in a short blackout before settling again.
 * Each assigned light is scaled from the intensity it starts with. Each glow renderer (the lamp's diffuser panel)
 * darkens with it through a per-renderer property block, so the shared lamp materials are never changed.
 * The optional hum follows the same level, dropping to a quiet buzz whenever the lamp goes dark.
 */
public class LightFlicker : MonoBehaviour
{
    [SerializeField] private Light[] lights;
    [SerializeField] private Renderer[] glowRenderers;
    [SerializeField] private AudioSource hum;

    [Header("Timing (seconds)")]
    [SerializeField, Min(0f)] private float steadyMin = 1.5f;
    [SerializeField, Min(0f)] private float steadyMax = 5f;
    [SerializeField, Min(1)] private int stuttersMin = 2;
    [SerializeField, Min(1)] private int stuttersMax = 6;
    [SerializeField, Range(0f, 1f)] private float blackoutChance = 0.3f;
    [SerializeField, Min(0f)] private float blackoutMin = 0.3f;
    [SerializeField, Min(0f)] private float blackoutMax = 1.2f;

    [Header("Levels")]
    [Tooltip("Highest level a stutter drops to (0 = fully off).")]
    [SerializeField, Range(0f, 1f)] private float stutterLow = 0.08f;
    [Tooltip("How much the steady light wavers (0 = perfectly steady).")]
    [SerializeField, Range(0f, 0.3f)] private float shimmer = 0.05f;
    [Tooltip("How dark the diffuser panel gets when the lamp is off, as a share of its normal colour.")]
    [SerializeField, Range(0f, 1f)] private float glowOffLevel = 0.25f;
    [Tooltip("Hum volume while the lamp is dark, as a share of its normal volume.")]
    [SerializeField, Range(0f, 1f)] private float humOffLevel = 0.2f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private float[] baseIntensities;
    private Color[] baseColors;
    private MaterialPropertyBlock block;
    private float baseVolume;

    private void Awake()
    {
        baseIntensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) baseIntensities[i] = lights[i].intensity;

        block = new MaterialPropertyBlock();
        baseColors = new Color[glowRenderers.Length];
        for (int i = 0; i < glowRenderers.Length; i++)
        {
            Material mat = glowRenderers[i].sharedMaterial;
            baseColors[i] = mat != null && mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : Color.white;
        }

        if (hum != null) baseVolume = hum.volume;
    }

    private void OnEnable()
    {
        StartCoroutine(Run());
    }

    // Leaves the lamp fully on when the component is switched off or the scene closes.
    private void OnDisable()
    {
        StopAllCoroutines();
        Apply(1f);
    }

    private IEnumerator Run()
    {
        while (true)
        {
            float steadyUntil = Time.time + Random.Range(steadyMin, steadyMax);
            while (Time.time < steadyUntil)
            {
                Apply(1f - Random.value * shimmer);
                yield return null;
            }

            int stutters = Random.Range(stuttersMin, stuttersMax + 1);
            for (int i = 0; i < stutters; i++)
            {
                Apply(Random.Range(0f, stutterLow));
                yield return new WaitForSeconds(Random.Range(0.03f, 0.12f));
                Apply(Random.Range(0.6f, 1f));
                yield return new WaitForSeconds(Random.Range(0.03f, 0.15f));
            }

            if (Random.value < blackoutChance)
            {
                Apply(0f);
                yield return new WaitForSeconds(Random.Range(blackoutMin, blackoutMax));
            }
        }
    }

    // Sets every light, glow panel and the hum to the given level (0 = off, 1 = normal).
    private void Apply(float level)
    {
        if (baseIntensities == null) return;

        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) lights[i].intensity = baseIntensities[i] * level;

        for (int i = 0; i < glowRenderers.Length; i++)
        {
            if (glowRenderers[i] == null) continue;
            glowRenderers[i].GetPropertyBlock(block);
            block.SetColor(BaseColorId, baseColors[i] * Mathf.Lerp(glowOffLevel, 1f, level));
            glowRenderers[i].SetPropertyBlock(block);
        }

        if (hum != null) hum.volume = baseVolume * Mathf.Lerp(humOffLevel, 1f, level);
    }
}
