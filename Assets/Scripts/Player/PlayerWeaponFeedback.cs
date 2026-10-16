using UnityEngine;
using UnityEngine.Rendering;

// Audio and visual feedback for PlayerWeapon: muzzle flash, shot sound, dry-fire click, reload sounds,
// and a hit effect with a sound where a shot lands.
// Put this on the Player (next to PlayerWeapon). It only LISTENS to the weapon, so the weapon never knows
// it exists, and it never calls Noise.Emit (the Hunter's gunshot noise is PlayerNoiseEmitter's job).
// Works with no setup: any empty clip slot falls back to the placeholder clips in
// Resources/WeaponFeedback, and the muzzle flash and hit effect are created in code.
// To use better assets later, fill in the Inspector slots (clips, a flash object, an impact prefab).

[RequireComponent(typeof(PlayerWeapon))]
public class PlayerWeaponFeedback : MonoBehaviour
{
    [Header("Muzzle flash")]
    [Tooltip("Where the flash appears. Leave empty to use a point in front of the camera.")]
    [SerializeField] private Transform muzzle;
    [Tooltip("Optional flash model or particle object. It is switched on for the flash duration.")]
    [SerializeField] private GameObject muzzleFlashObject;
    [SerializeField] private bool useFlashLight = true;
    [SerializeField] private Color flashColor = new Color(1f, 0.75f, 0.4f);
    [SerializeField, Min(0f)] private float flashIntensity = 4f;
    [SerializeField, Min(0f)] private float flashRange = 6f;
    [SerializeField, Min(0.01f)] private float flashDuration = 0.06f;
    [Tooltip("Flash position relative to the camera when no Muzzle is assigned (right, up, forward).")]
    [SerializeField] private Vector3 fallbackMuzzleOffset = new Vector3(0.12f, -0.1f, 0.6f);

    [Header("Audio (empty slots use the placeholder clips in Resources/WeaponFeedback)")]
    [SerializeField] private AudioClip[] shotClips;
    [SerializeField] private AudioClip dryFireClip;
    [SerializeField] private AudioClip reloadStartClip;
    [SerializeField] private AudioClip reloadFinishClip;
    [SerializeField] private AudioClip impactWorldClip;
    [SerializeField] private AudioClip impactTargetClip;
    [SerializeField, Range(0f, 1f)] private float shotVolume = 0.8f;
    [SerializeField, Range(0f, 0.3f)] private float shotPitchVariation = 0.06f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float impactVolume = 0.6f;

    [Header("Hit effect")]
    [Tooltip("Optional. Spawned at the hit point, facing out of the surface. Leave empty for the built-in puff.")]
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField, Min(0.5f)] private float impactPrefabLifetime = 3f;
    [SerializeField, Min(0.01f)] private float puffSize = 0.12f;
    [SerializeField, Min(0.02f)] private float puffDuration = 0.18f;
    [SerializeField] private Color worldImpactColor = new Color(0.85f, 0.82f, 0.7f);
    [SerializeField] private Color targetImpactColor = new Color(0.8f, 0.1f, 0.1f);
    [SerializeField, Range(1, 32)] private int puffPoolSize = 12;

    private sealed class Puff
    {
        public Transform transform;
        public Renderer renderer;
        public Vector3 start;
        public Vector3 direction;
        public float startTime;
        public bool active;
    }

    private static MaterialPropertyBlock block;

    private PlayerWeapon weapon;
    private AudioSource shotSource;
    private AudioSource sfxSource;
    private Light flashLight;
    private float flashEndTime;
    private bool flashActive;
    private Puff[] puffs;
    private Transform effectsRoot;

    private void Awake()
    {
        weapon = GetComponent<PlayerWeapon>();

        LoadFallbackClips();
        SetUpAudio();
        SetUpMuzzle();
        if (impactEffectPrefab == null) BuildPuffPool();
    }

    private void OnEnable()
    {
        weapon.Fired += OnFired;
        weapon.DryFired += OnDryFired;
        weapon.ReloadStarted += OnReloadStarted;
        weapon.ReloadFinished += OnReloadFinished;
        weapon.ShotHit += OnShotHit;
    }

    private void OnDisable()
    {
        weapon.Fired -= OnFired;
        weapon.DryFired -= OnDryFired;
        weapon.ReloadStarted -= OnReloadStarted;
        weapon.ReloadFinished -= OnReloadFinished;
        weapon.ShotHit -= OnShotHit;

        HideFlash();
    }

    private void OnDestroy()
    {
        // The effects live in the scene root (not under the moving Player), so clean them up here.
        if (effectsRoot != null) Destroy(effectsRoot.gameObject);
    }

    // Setup

    private void LoadFallbackClips()
    {
        if (shotClips == null || shotClips.Length == 0)
            shotClips = Resources.LoadAll<AudioClip>("WeaponFeedback/Shots");
        if (dryFireClip == null) dryFireClip = Resources.Load<AudioClip>("WeaponFeedback/dry_click");
        if (reloadStartClip == null) reloadStartClip = Resources.Load<AudioClip>("WeaponFeedback/reload_start");
        if (reloadFinishClip == null) reloadFinishClip = Resources.Load<AudioClip>("WeaponFeedback/reload_finish");
        if (impactWorldClip == null) impactWorldClip = Resources.Load<AudioClip>("WeaponFeedback/impact_world");
        if (impactTargetClip == null) impactTargetClip = Resources.Load<AudioClip>("WeaponFeedback/impact_target");
    }

    private void SetUpAudio()
    {
        // Two sources: shots get a random pitch, and PlayOneShot pitch is per source, so keep them apart.
        shotSource = CreateSource("WeaponShotAudio");
        sfxSource = CreateSource("WeaponSfxAudio");
    }

    private AudioSource CreateSource(string objectName)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // the player's own gun: plain 2D sound
        return source;
    }

    private void SetUpMuzzle()
    {
        if (muzzle == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            Transform parent = cam != null ? cam.transform : transform;

            var anchor = new GameObject("MuzzleFlashAnchor");
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = fallbackMuzzleOffset;
            muzzle = anchor.transform;
        }

        if (useFlashLight)
        {
            var go = new GameObject("MuzzleFlashLight");
            go.transform.SetParent(muzzle, false);
            flashLight = go.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.color = flashColor;
            flashLight.intensity = flashIntensity;
            flashLight.range = flashRange;
            flashLight.shadows = LightShadows.None;
            flashLight.enabled = false;
        }

        if (muzzleFlashObject != null) muzzleFlashObject.SetActive(false);
    }

    private void BuildPuffPool()
    {
        effectsRoot = new GameObject("WeaponImpactEffects").transform;
        puffs = new Puff[puffPoolSize];

        for (int i = 0; i < puffs.Length; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ImpactPuff";
            go.transform.SetParent(effectsRoot, false);

            // Purely visual: no collider, so it never blocks shots, the player or the Director's raycasts.
            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer r = go.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;

            go.SetActive(false);
            puffs[i] = new Puff { transform = go.transform, renderer = r };
        }
    }

    // Weapon events

    private void OnFired(Vector3 position)
    {
        ShowFlash();

        AudioClip clip = PickShotClip();
        if (clip != null)
        {
            shotSource.pitch = 1f + Random.Range(-shotPitchVariation, shotPitchVariation);
            shotSource.PlayOneShot(clip, shotVolume);
        }
    }

    private void OnDryFired() => PlaySfx(dryFireClip);
    private void OnReloadStarted() => PlaySfx(reloadStartClip);
    private void OnReloadFinished() => PlaySfx(reloadFinishClip);

    private void OnShotHit(Vector3 point, Vector3 normal, bool hitTarget)
    {
        AudioClip clip = hitTarget ? impactTargetClip : impactWorldClip;
        if (clip != null) AudioSource.PlayClipAtPoint(clip, point, impactVolume);

        if (impactEffectPrefab != null)
        {
            GameObject fx = Instantiate(impactEffectPrefab, point, Quaternion.LookRotation(normal));
            Destroy(fx, impactPrefabLifetime);
        }
        else
        {
            SpawnPuff(point, normal, hitTarget ? targetImpactColor : worldImpactColor);
        }
    }

    // Effects

    private void ShowFlash()
    {
        if (flashLight != null) flashLight.enabled = true;
        if (muzzleFlashObject != null) muzzleFlashObject.SetActive(true);

        flashActive = true;
        flashEndTime = Time.time + flashDuration;
    }

    private void HideFlash()
    {
        if (flashLight != null) flashLight.enabled = false;
        if (muzzleFlashObject != null) muzzleFlashObject.SetActive(false);
        flashActive = false;
    }

    private void SpawnPuff(Vector3 point, Vector3 normal, Color color)
    {
        if (puffs == null) return;

        // Reuse a free puff, or the oldest one if all are busy.
        Puff chosen = null;
        foreach (Puff p in puffs)
        {
            if (!p.active) { chosen = p; break; }
            if (chosen == null || p.startTime < chosen.startTime) chosen = p;
        }

        // Start just off the surface so it is not buried in the wall.
        chosen.start = point + normal * 0.02f;
        chosen.direction = normal;
        chosen.startTime = Time.time;
        chosen.active = true;

        chosen.transform.position = chosen.start;
        chosen.transform.localScale = Vector3.one * puffSize;
        chosen.transform.gameObject.SetActive(true);

        if (block == null) block = new MaterialPropertyBlock();
        chosen.renderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color); // URP Lit
        block.SetColor("_Color", color);     // other shaders
        chosen.renderer.SetPropertyBlock(block);
    }

    private void Update()
    {
        if (flashActive && Time.time >= flashEndTime) HideFlash();

        if (puffs == null) return;

        foreach (Puff p in puffs)
        {
            if (!p.active) continue;

            float k = (Time.time - p.startTime) / puffDuration;
            if (k >= 1f)
            {
                p.active = false;
                p.transform.gameObject.SetActive(false);
                continue;
            }

            // Shrink away while drifting out of the surface.
            p.transform.position = p.start + p.direction * (k * 0.12f);
            p.transform.localScale = Vector3.one * (puffSize * (1f - k));
        }
    }

    // Audio helpers

    private AudioClip PickShotClip()
    {
        if (shotClips == null || shotClips.Length == 0) return null;
        return shotClips[Random.Range(0, shotClips.Length)];
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
    }
}
