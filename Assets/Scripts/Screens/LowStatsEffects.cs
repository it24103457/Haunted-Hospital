using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/*
 * The screen effects and sounds for low health and low sanity. It only does what it is told through SetHealthLow and
 * SetSanityLow; PlayerHudBinder decides when.
 * Low health: a faint red tint over the screen, a red vignette and a slight blur at the edges, and a heartbeat that starts
 * loud, eases a little softer and keeps beating until health recovers.
 * Low sanity: a black vignette and a blur strongest at the edges, easing towards a slightly blurred centre, and a headache
 * sound whose loud opening plays once before its quieter remainder loops until sanity recovers.
 * The picture effect is one full-screen shader pass added to each game camera from code, so no render pipeline settings
 * are changed; the HUD canvas is drawn after it and stays sharp. Each effect fades in and out over a moment.
 * Clips loop over a chosen stretch (skipping silent ends), so the heartbeat keeps its rhythm and the headache never pauses.
 */
public class LowStatsEffects : MonoBehaviour
{
    private static readonly int HealthLowId = Shader.PropertyToID("_HealthLow");
    private static readonly int SanityLowId = Shader.PropertyToID("_SanityLow");
    private static readonly int HealthTintId = Shader.PropertyToID("_HealthTint");
    private static readonly int HealthVignetteId = Shader.PropertyToID("_HealthVignette");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");

    [Header("Screen effect")]
    [SerializeField] private Shader screenShader;
    [Tooltip("Colour multiplied over the whole screen at low health; alpha is how strongly.")]
    [SerializeField] private Color healthTint = new Color(0.8f, 0.42f, 0.4f, 0.35f);
    [Tooltip("Blood colour staining the darkened edges at low health; alpha is how strongly.")]
    [SerializeField] private Color healthVignette = new Color(0.4f, 0.02f, 0.03f, 0.85f);
    [Tooltip("Seconds for an effect to fade in or out.")]
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.6f;

    [Header("Heartbeat (low health)")]
    [SerializeField] private AudioClip heartbeat;
    [Tooltip("The heartbeat repeats from 0 to this point, skipping the silence at the end of the clip.")]
    [SerializeField, Min(0.1f)] private float heartbeatLoopEnd = 7.5f;
    [SerializeField, Range(0f, 1f)] private float heartbeatLoudVolume = 0.9f;
    [SerializeField, Range(0f, 1f)] private float heartbeatQuietVolume = 0.55f;
    [Tooltip("Seconds for the heartbeat to ease from loud to quiet.")]
    [SerializeField, Min(0.1f)] private float heartbeatSoftenSeconds = 4f;

    [Header("Headache (low sanity)")]
    [SerializeField] private AudioClip headache;
    [Tooltip("The loud opening plays once from the start up to here, then the clip repeats from here to Loop End.")]
    [SerializeField, Min(0f)] private float headacheLoopStart = 4f;
    [Tooltip("The end of the repeated stretch, skipping the silence at the end of the clip.")]
    [SerializeField, Min(0.1f)] private float headacheLoopEnd = 29f;
    [SerializeField, Range(0f, 1f)] private float headacheLoudVolume = 0.9f;
    [SerializeField, Range(0f, 1f)] private float headacheLoopVolume = 0.6f;

    private Material material;
    private LowStatsPass pass;
    private AudioSource heartbeatSource;
    private AudioSource headacheSource;
    private bool healthLow;
    private bool sanityLow;
    private float healthAmount;
    private float sanityAmount;
    private float healthLowSince;

    private void Awake()
    {
        if (screenShader != null) material = CoreUtils.CreateEngineMaterial(screenShader);
        pass = new LowStatsPass(material);

        heartbeatSource = NewSource(heartbeat);
        headacheSource = NewSource(headache);
    }

    private void OnEnable() => RenderPipelineManager.beginCameraRendering += AddPassToCamera;
    private void OnDisable() => RenderPipelineManager.beginCameraRendering -= AddPassToCamera;

    private void OnDestroy()
    {
        if (material != null) CoreUtils.Destroy(material);
    }

    /*
     * Starts or stops the low-health effect. With withSound false (health at 0, as the death screen comes in) the picture
     * effect stays on, so the screen never clears before the death screen covers it, but the heartbeat stops.
     */
    public void SetHealthLow(bool low, bool withSound = true)
    {
        bool wasLow = healthLow;
        healthLow = low;

        if (!low || !withSound)
        {
            heartbeatSource.Stop();
            return;
        }

        if (!wasLow)
        {
            healthLowSince = Time.unscaledTime;
            Play(heartbeatSource, 0f, heartbeatLoudVolume);
        }
    }

    /*
     * Starts or stops the low-sanity effect. With withSound false (sanity at 0, as the sanity screen comes in) the picture
     * effect stays on, so the screen never clears before the sanity screen covers it, but the headache stops.
     */
    public void SetSanityLow(bool low, bool withSound = true)
    {
        bool wasLow = sanityLow;
        sanityLow = low;

        if (!low || !withSound)
        {
            headacheSource.Stop();
            return;
        }

        if (!wasLow) Play(headacheSource, 0f, headacheLoudVolume);
    }

    private void Update()
    {
        float step = Time.unscaledDeltaTime / fadeSeconds;
        healthAmount = Mathf.MoveTowards(healthAmount, healthLow ? 1f : 0f, step);
        sanityAmount = Mathf.MoveTowards(sanityAmount, sanityLow ? 1f : 0f, step);

        if (material != null)
        {
            material.SetFloat(HealthLowId, healthAmount);
            material.SetFloat(SanityLowId, sanityAmount);
            // Sent in the linear space the game renders in, so the colours on screen match the ones picked here.
            material.SetVector(HealthTintId, LinearWithAlpha(healthTint));
            material.SetVector(HealthVignetteId, LinearWithAlpha(healthVignette));
            material.SetFloat(AspectId, Screen.height > 0 ? (float)Screen.width / Screen.height : 1f);
        }

        if (healthLow && heartbeatSource.isPlaying)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - healthLowSince) / heartbeatSoftenSeconds);
            heartbeatSource.volume = Mathf.Lerp(heartbeatLoudVolume, heartbeatQuietVolume, t * t * (3f - 2f * t));
            Wrap(heartbeatSource, 0f, heartbeatLoopEnd);
        }

        if (sanityLow && headacheSource.isPlaying)
        {
            // Past the loud opening, the remainder repeats more quietly.
            if (headacheSource.time >= headacheLoopStart) headacheSource.volume = headacheLoopVolume;
            Wrap(headacheSource, headacheLoopStart, headacheLoopEnd);
        }
    }

    // Before each game camera draws: adds the screen effect while either effect is showing (never to preview cameras).
    private void AddPassToCamera(ScriptableRenderContext context, Camera camera)
    {
        if (material == null || (healthAmount <= 0f && sanityAmount <= 0f)) return;
        if (camera.cameraType != CameraType.Game || camera.targetTexture != null) return;
        if (!camera.TryGetComponent(out UniversalAdditionalCameraData data) || data.renderType != CameraRenderType.Base) return;

        data.scriptableRenderer.EnqueuePass(pass);
    }

    // A colour converted to linear space, keeping its alpha (the strength) as it is.
    private static Vector4 LinearWithAlpha(Color colour)
    {
        Color linear = colour.linear;
        return new Vector4(linear.r, linear.g, linear.b, colour.a);
    }

    // Jumps back to the loop start once playback passes the loop end, so only the chosen stretch repeats.
    private static void Wrap(AudioSource source, float loopStart, float loopEnd)
    {
        AudioClip clip = source.clip;
        if (clip == null) return;

        float end = Mathf.Min(loopEnd, clip.length);
        if (source.time < end) return;

        source.timeSamples = Mathf.Clamp((int)(loopStart * clip.frequency), 0, clip.samples - 1);
    }

    // Plays a clip from a point; started first and then moved, since a position set before Play can be reset by Play.
    private static void Play(AudioSource source, float from, float volume)
    {
        if (source.clip == null) return;

        source.volume = volume;
        source.Play();
        source.timeSamples = Mathf.Clamp((int)(from * source.clip.frequency), 0, source.clip.samples - 1);
    }

    private AudioSource NewSource(AudioClip clip)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.clip = clip;
        if (clip != null) clip.LoadAudioData();
        return source;
    }

    /*
     * The render pass: after the game's own post-processing, copies the camera picture through the effect shader into a
     * new texture and makes that the camera picture, so everything drawn afterwards (the HUD) goes on top of the result.
     */
    private class LowStatsPass : ScriptableRenderPass
    {
        private readonly Material material;

        public LowStatsPass(Material material)
        {
            this.material = material;
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            requiresIntermediateTexture = true;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (material == null || resources.isActiveTargetBackBuffer) return;

            TextureHandle source = resources.activeColorTexture;
            TextureDesc desc = renderGraph.GetTextureDesc(source);
            desc.name = "LowStatsScreen";
            desc.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(desc);

            renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0), "Low Stats Screen");
            resources.cameraColor = destination;
        }
    }
}
