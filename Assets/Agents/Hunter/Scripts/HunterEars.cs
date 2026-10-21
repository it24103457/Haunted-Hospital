using System;
using System.Collections.Generic;
using UnityEngine;

// One sound the Hunter heard and still remembers.
public readonly struct HeardNoise
{
    public readonly int Id;
    public readonly Vector3 Position;
    public readonly NoiseType Type;
    public readonly float HeardLoudness;   // how loud it was AT the Hunter, after walls and doors
    public readonly float Importance;
    public readonly float Time;
    public readonly GameObject Source;

    public HeardNoise(int id, Vector3 position, NoiseType type, float heardLoudness, float importance, float time, GameObject source)
    {
        Id = id;
        Position = position;
        Type = type;
        HeardLoudness = heardLoudness;
        Importance = importance;
        Time = time;
        Source = source;
    }
}

/*
 * The Hunter's hearing. It listens to every noise, keeps only what could really reach it through the building,
 * scores it (importance x heard loudness x e^(-age / time constant)) and remembers the best few.
 * It never moves the Hunter: HunterBrain reads Target and decides what to do.
 */
[RequireComponent(typeof(HunterBrain))]
public class HunterEars : MonoBehaviour
{
    [Tooltip("The shared waypoint graph. Found in the scene when left empty.")]
    [SerializeField] private WaypointGraph graph;

    // Remembered noises, in no particular order.
    public IReadOnlyList<HeardNoise> Memory => memory;

    // True while there is a noise worth heading for.
    public bool HasTarget { get; private set; }

    // The noise the Hunter would go to. Only valid while HasTarget is true.
    public HeardNoise Target { get; private set; }

    public float TargetScore => HasTarget ? ScoreOf(Target) : 0f;

    // The target is strong enough to leave Patrol and investigate.
    public bool WantsToInvestigate => HasTarget && settings != null && TargetScore >= settings.InvestigateThreshold;

    // Raised when the target appears, disappears or changes to a different noise.
    public event Action TargetChanged;

    /// <summary>Raised for every noise the Hunter hears (Id is 0, because this is the sound itself, not a memory).</summary>
    public event Action<HeardNoise> NoiseHeard;

    private readonly List<HeardNoise> memory = new List<HeardNoise>();
    private HunterBrain brain;
    private HunterSettings settings;
    private int nextId = 1;
    private int targetId = -1;

    private void Awake()
    {
        brain = GetComponent<HunterBrain>();
        settings = brain.Settings;
    }

    private void Start()
    {
        if (graph == null) graph = FindAnyObjectByType<WaypointGraph>();
    }

    private void OnEnable()
    {
        Noise.Emitted += OnNoise;
        brain.OnSpawned += ClearMemory;
        brain.OnDied += ClearMemory;
    }

    private void OnDisable()
    {
        Noise.Emitted -= OnNoise;
        brain.OnSpawned -= ClearMemory;
        brain.OnDied -= ClearMemory;
    }

    private void Update()
    {
        if (settings == null || memory.Count == 0) return;

        float now = Time.time;
        for (int i = memory.Count - 1; i >= 0; i--)
        {
            if (ScoreOf(memory[i], now) < settings.ForgetBelowScore) memory.RemoveAt(i);
        }

        UpdateTarget(now);
    }

    // Forgets the current target, for example once the Hunter has reached it. The next best noise (if any) takes over.
    public void RemoveTarget()
    {
        if (HasTarget) Forget(targetId);
    }

    // Forgets one remembered noise by its Id. The next best noise (if any) becomes the target.
    public void Forget(int id)
    {
        for (int i = 0; i < memory.Count; i++)
        {
            if (memory[i].Id != id) continue;
            memory.RemoveAt(i);
            break;
        }

        UpdateTarget(Time.time);
    }

    // Forgets everything, for example on death or respawn.
    public void ClearMemory()
    {
        memory.Clear();
        UpdateTarget(Time.time);
    }

    public float ScoreOf(HeardNoise noise) => ScoreOf(noise, Time.time);

    private float ScoreOf(HeardNoise noise, float now)
    {
        return noise.Importance * noise.HeardLoudness * Mathf.Exp(-(now - noise.Time) / settings.MemoryTimeConstant);
    }

    // Hearing

    private void OnNoise(NoiseEvent noise)
    {
        if (settings == null || graph == null || !brain.IsActive) return;
        if (settings.IgnoreOwnSounds && IsOwnSound(noise.Source)) return;

        WaypointNode listener = graph.GetNearestNode(transform.position);
        if (listener == null) return;

        // Sound travels through the building, so walls block it and closed doors and barricades muffle it.
        if (!SoundPropagation.TryHear(graph, noise.Position, noise.Loudness, listener,
                settings.HearingThreshold, settings.Propagation, out float heard))
        {
            if (settings.EnableDebugLogging)
                Debug.Log($"[Hunter] Did not hear {noise.Type}: too quiet by the time it reached the Hunter.", this);
            return;
        }

        Remember(noise, heard);
    }

    private bool IsOwnSound(GameObject source)
    {
        return source != null && (source == gameObject || source.transform.IsChildOf(transform));
    }

    // Memory

    private void Remember(NoiseEvent noise, float heard)
    {
        float now = Time.time;
        float importance = settings.GetImportance(noise.Type);
        int near = IndexNear(noise.Position, settings.MemoryMergeRadius);
        string outcome;

        if (near >= 0)
        {
            // Same place as something remembered: keep whichever is stronger, so a footstep cannot overwrite a gunshot.
            HeardNoise old = memory[near];
            var updated = new HeardNoise(old.Id, noise.Position, noise.Type, heard, importance, now, noise.Source);
            bool replace = ScoreOf(updated, now) >= ScoreOf(old, now);
            if (replace) memory[near] = updated;
            outcome = replace ? "merged into an existing memory" : "kept the stronger old memory";
        }
        else
        {
            memory.Add(new HeardNoise(nextId++, noise.Position, noise.Type, heard, importance, now, noise.Source));
            if (memory.Count > settings.MemoryCapacity) RemoveWeakest(now);
            outcome = "new memory";
        }

        if (settings.EnableDebugLogging)
            Debug.Log($"[Hunter] Heard {noise.Type}: loudness {noise.Loudness:0.#} -> {heard:0.#} at the Hunter ({outcome}, " +
                      $"{memory.Count} remembered, merge radius {settings.MemoryMergeRadius:0.#} m).", this);

        UpdateTarget(now);

        // Last, because a listener may clear the memory (a chase starting does).
        NoiseHeard?.Invoke(new HeardNoise(0, noise.Position, noise.Type, heard, importance, now, noise.Source));
    }

    private int IndexNear(Vector3 position, float radius)
    {
        int best = -1;
        float bestSqr = radius * radius;

        for (int i = 0; i < memory.Count; i++)
        {
            float sqr = (memory[i].Position - position).sqrMagnitude;
            if (sqr > bestSqr) continue;
            bestSqr = sqr;
            best = i;
        }

        return best;
    }

    private void RemoveWeakest(float now)
    {
        int weakest = 0;
        float weakestScore = float.MaxValue;

        for (int i = 0; i < memory.Count; i++)
        {
            float score = ScoreOf(memory[i], now);
            if (score >= weakestScore) continue;
            weakestScore = score;
            weakest = i;
        }

        memory.RemoveAt(weakest);
    }

    // Picks the best noise, but only abandons the current target for one that is clearly better (hysteresis).
    private void UpdateTarget(float now)
    {
        int bestIndex = -1, currentIndex = -1;
        float bestScore = 0f, currentScore = 0f;

        for (int i = 0; i < memory.Count; i++)
        {
            float score = ScoreOf(memory[i], now);
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }

            if (HasTarget && memory[i].Id == targetId)
            {
                currentIndex = i;
                currentScore = score;
            }
        }

        if (bestIndex < 0)
        {
            SetTarget(-1, default);
            return;
        }

        int chosen = bestIndex;
        bool beatsCurrent = bestScore > currentScore * (1f + settings.RetargetMargin);
        if (currentIndex >= 0 && !beatsCurrent) chosen = currentIndex;

        SetTarget(memory[chosen].Id, memory[chosen]);
    }

    private void SetTarget(int id, HeardNoise noise)
    {
        bool changed = HasTarget != (id >= 0) || targetId != id;

        HasTarget = id >= 0;
        targetId = id;
        Target = noise;

        if (changed) TargetChanged?.Invoke();
    }
}
