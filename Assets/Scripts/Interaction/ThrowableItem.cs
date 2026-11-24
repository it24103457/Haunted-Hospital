using UnityEngine;


// A small prop the player can pick up (E) and throw (G, see PlayerThrower).

// This is deliberately separate from PickupInteractable: those items are consumed into player stats,
// can be claimed by the Thief and are tracked by the ItemSpawnDirector. A throwable is never consumed.

// When a thrown item hits something it can:
//   - emit a NoiseType.ThrownObject noise through the existing Noise board (the Hunter hears it), and
//   - optionally damage the Hunter or the Thief (damage is 0 by default, so a throw only distracts).

// Setup on a prefab: add this component (a Rigidbody is added for you) and make sure the prefab has a Collider.
// The agents never react to the item itself, only to the noise it makes.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class ThrowableItem : MonoBehaviour, IInteractable
{
    private enum ItemState { Resting, Held, Thrown }

    [Header("Identity")]
    [SerializeField] private string displayName = "Item";

    [Header("Impact noise")]
    [Tooltip("Loudness at a full-speed impact. Bedpan / tray about 20, pill bottle about 10. 0 = silent.")]
    [SerializeField, Min(0f)] private float noiseLoudness = NoiseLevels.ThrownObject;
    [Tooltip("Impacts slower than this (m/s) make no noise.")]
    [SerializeField, Min(0f)] private float minNoiseImpactSpeed = 1.5f;
    [Tooltip("Impacts at or above this speed (m/s) make the full noiseLoudness. Slower ones scale down to 25%.")]
    [SerializeField, Min(0.1f)] private float fullNoiseImpactSpeed = 6f;
    [Tooltip("A bouncing item can only make this many noises per throw.")]
    [SerializeField, Min(1)] private int maxNoisesPerThrow = 3;
    [Tooltip("Off (recommended): the noise comes from the item, so the Hunter goes to investigate the spot. " +
             "On: the noise counts as the player's, so a loud one starts a Chase toward the impact.")]
    [SerializeField] private bool noiseCountsAsPlayer = false;
    [Tooltip("A dropped item (G) makes this fraction of the normal impact noise. 0 = silent drop.")]
    [SerializeField, Range(0f, 1f)] private float dropNoiseScale = 0.3f;

    [Header("Impact sound (what the player hears)")]
    [Tooltip("Sounds played when the item hits something. One is picked at random. Leave empty for no sound. " +
             "This is separate from the noise above, which is what the Hunter hears.")]
    [SerializeField] private AudioClip[] impactClips;
    [Tooltip("Played instead of Impact Clips when the item hits the Hunter or the Thief, so the player can hear that " +
             "they hit something living. Leave empty to use Impact Clips for those hits too.")]
    [SerializeField] private AudioClip[] bodyHitClips;
    [SerializeField, Range(0f, 1f)] private float impactVolume = 0.8f;
    [Tooltip("Each impact is played at a random pitch in this range so repeats do not sound identical.")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);
    [Tooltip("Beyond this distance (metres) the sound is inaudible.")]
    [SerializeField, Min(2f)] private float soundMaxDistance = 30f;

    [Header("Damage (Hunter and Thief only)")]
    [Tooltip("0 = the item only distracts. Barricades and everything else are never damaged by a throw.")]
    [SerializeField, Min(0f)] private float damage = 0f;
    [Tooltip("Impacts slower than this (m/s) deal no damage.")]
    [SerializeField, Min(0f)] private float minDamageImpactSpeed = 4f;
    [Tooltip("Off: a hit does not reveal the player to the Hunter. " +
             "On: it counts as the player's shot, so the Hunter chases the player's position.")]
    [SerializeField] private bool damageRevealsPlayer = false;

    [Header("Lifetime")]
    [Tooltip("Seconds a thrown item stays on the floor before it is removed. 0 = never removed.")]
    [SerializeField, Min(0f)] private float despawnSeconds = 30f;

    [Header("Held pose")]
    [SerializeField] private Vector3 heldLocalEuler = Vector3.zero;

    private Rigidbody body;
    private Collider[] colliders;
    private bool[] colliderWasEnabled;
    private PlayerThrower thrower;
    private ItemState state = ItemState.Resting;

    private bool hasBeenThrown;
    private bool wasDropped;
    private float noiseScaleThisThrow = 1f;
    private int noisesThisThrow;
    private float lastNoiseTime;
    private AudioSource audioSource;
    private int soundsThisThrow;
    private float lastSoundTime;
    private bool damageDealtThisThrow;
    private float throwStartTime;
    private float calmTime;
    private float restTime;

    public Collider[] Colliders => colliders;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        colliderWasEnabled = new bool[colliders.Length];

        // The sound plays from a child object that is moved to each impact point, because the item's own
        // pivot can be far from its visible mesh.
        if (HasClips(impactClips) || HasClips(bodyHitClips))
        {
            var soundObject = new GameObject("ImpactSound");
            soundObject.transform.SetParent(transform, false);
            audioSource = soundObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = soundMaxDistance;
            audioSource.dopplerLevel = 0f;
        }

        if (colliders.Length == 0)
            Debug.LogWarning($"ThrowableItem '{name}' has no Collider, so it cannot be picked up or make impact noise.", this);
    }

    private void Start()
    {
        thrower = FindAnyObjectByType<PlayerThrower>();
        if (thrower != null) thrower.IgnorePlayerCollisions(this);
    }

    // IInteractable

    public string GetPrompt()
    {
        if (state == ItemState.Held) return string.Empty;
        if (thrower != null && thrower.IsHolding) return "Hands full";
        return $"Press E to pick up {displayName}";
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (state == ItemState.Held) return;

        PlayerThrower owner = interactor.GetComponentInParent<PlayerThrower>();
        if (owner == null) owner = interactor.GetComponentInChildren<PlayerThrower>();
        if (owner == null) owner = thrower;
        if (owner == null) return;

        thrower = owner;
        owner.TryPickUp(this);
    }

    // Called by PlayerThrower

    public string DisplayName => displayName;

    public void Hold(Transform anchor)
    {
        if (state == ItemState.Held) return;

        state = ItemState.Held;
        restTime = 0f;

        // Zero the velocity first: setting it on a kinematic body is ignored and logs a warning.
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.isKinematic = true;

        // Colliders are off while held so the item cannot block the interact ray or the weapon ray.
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderWasEnabled[i] = colliders[i].enabled;
            colliders[i].enabled = false;
        }

        transform.SetParent(anchor, true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(heldLocalEuler);

        // Centre the visible mesh on the hand, not the pivot (see VisualCenterOffset).
        transform.position -= VisualCenterOffset();
    }

    // Where the visible mesh is centred, relative to the object's pivot. Several prop prefabs have their model
    // offset a long way from the root, so a held item is positioned by its mesh. Otherwise the root would sit
    // in the hand while the mesh is metres away, out of view.
    private Vector3 VisualCenterOffset()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return Vector3.zero;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds.center - transform.position;
    }

    // Let go of the item. dropped = true for a gentle drop: quieter, and it never deals damage.
    public void Release(Vector3 position, Vector3 velocity, bool dropped = false)
    {
        if (state != ItemState.Held) return;

        // Measured while still in the hand, so the mesh (not the pivot) starts the throw at 'position'.
        Vector3 visualOffset = VisualCenterOffset();

        transform.SetParent(null, true);
        transform.position = position - visualOffset;

        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = colliderWasEnabled[i];

        // Re-applied on every throw: enabling a collider can clear a previous ignore.
        if (thrower != null) thrower.IgnorePlayerCollisions(this);

        body.isKinematic = false;
        body.useGravity = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearVelocity = velocity;
        body.angularVelocity = Random.insideUnitSphere * 6f;

        state = ItemState.Thrown;
        hasBeenThrown = true;
        wasDropped = dropped;
        noiseScaleThisThrow = dropped ? dropNoiseScale : 1f;
        noisesThisThrow = 0;
        soundsThisThrow = 0;
        damageDealtThisThrow = false;
        throwStartTime = Time.time;
        calmTime = 0f;
    }

    // Flight, landing, cleanup

    private void FixedUpdate()
    {
        if (state != ItemState.Thrown) return;

        bool slow = body.linearVelocity.sqrMagnitude < 0.04f && body.angularVelocity.sqrMagnitude < 0.25f;
        calmTime = slow ? calmTime + Time.fixedDeltaTime : 0f;

        if (calmTime >= 0.5f || Time.time - throwStartTime > 10f)
            Settle();
    }

    // Once it has stopped, freeze it. A resting item is then never shoved around by anything.
    private void Settle()
    {
        state = ItemState.Resting;
        restTime = 0f;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
    }

    private void Update()
    {
        if (state != ItemState.Resting || !hasBeenThrown || despawnSeconds <= 0f) return;

        restTime += Time.deltaTime;
        if (restTime >= despawnSeconds) Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (state != ItemState.Thrown) return;

        float speed = collision.relativeVelocity.magnitude;
        Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

        TryDamage(collision, speed, point);
        TryNoise(speed, point);
        TryPlaySound(speed, point, HitLivingAgent(collision.collider));
    }

    private static bool HasClips(AudioClip[] clips)
    {
        return clips != null && clips.Length > 0;
    }

    // True when the thing hit is the Hunter or the Thief.
    private static bool HitLivingAgent(Collider other)
    {
        IDamageable target = other.GetComponentInParent<IDamageable>();
        return target is HunterBrain || target is ThiefAgent;
    }

    private void TryPlaySound(float speed, Vector3 point, bool hitLiving)
    {
        // A living target gets the body-hit sound if one is set, otherwise the normal impact sound.
        AudioClip[] clips = hitLiving && HasClips(bodyHitClips) ? bodyHitClips : impactClips;

        if (audioSource == null || !HasClips(clips)) return;
        if (noiseScaleThisThrow <= 0f) return;                 // a silent drop is silent to the player too
        if (soundsThisThrow >= maxNoisesPerThrow) return;
        if (speed < minNoiseImpactSpeed) return;
        if (Time.time - lastSoundTime < 0.08f) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        float strength = Mathf.Lerp(0.25f, 1f, Mathf.InverseLerp(minNoiseImpactSpeed, fullNoiseImpactSpeed, speed));

        audioSource.transform.position = point;
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, impactVolume * strength * noiseScaleThisThrow);

        soundsThisThrow++;
        lastSoundTime = Time.time;
    }

    private void TryNoise(float speed, Vector3 point)
    {
        if (noiseLoudness <= 0f || noiseScaleThisThrow <= 0f) return;
        if (noisesThisThrow >= maxNoisesPerThrow) return;
        if (speed < minNoiseImpactSpeed) return;
        if (Time.time - lastNoiseTime < 0.12f) return;

        float strength = Mathf.Lerp(0.25f, 1f, Mathf.InverseLerp(minNoiseImpactSpeed, fullNoiseImpactSpeed, speed));
        GameObject source = noiseCountsAsPlayer && thrower != null ? thrower.gameObject : gameObject;

        Noise.Emit(point, noiseLoudness * strength * noiseScaleThisThrow, NoiseType.ThrownObject, source);

        noisesThisThrow++;
        lastNoiseTime = Time.time;
    }

    private void TryDamage(Collision collision, float speed, Vector3 point)
    {
        if (wasDropped || damage <= 0f || damageDealtThisThrow || speed < minDamageImpactSpeed) return;

        IDamageable target = collision.collider.GetComponentInParent<IDamageable>();

        // Allow-list: a throw must never hurt barricade planks, the player, or anything added later.
        if (!(target is HunterBrain) && !(target is ThiefAgent)) return;

        GameObject source = damageRevealsPlayer && thrower != null ? thrower.gameObject : gameObject;
        target.TakeDamage(new DamageInfo(damage, source, point));
        damageDealtThisThrow = true;
    }
}
