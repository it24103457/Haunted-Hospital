using UnityEngine;

/*
 * Every number the Sound Hunter uses lives here, so tuning never means editing code.
 * Create one with: right-click in the Project window > Create > Haunted Hospital > Hunter > Hunter Settings.
 * The other Hunter scripts (HunterEars, HunterPathfinder, HunterSearch, HunterBrain) only READ these values.
 *
 * Distances are in metres, times in seconds, loudness in the same units as NoiseLevels.
 */
[CreateAssetMenu(
    fileName = "HunterSettings_Hospital",
    menuName = "Haunted Hospital/Hunter/Hunter Settings")]
public sealed class HunterSettings : ScriptableObject
{
    // Hearing

    [Header("Hearing")]
    [Tooltip("A noise that reaches the Hunter quieter than this is not heard at all.")]
    [SerializeField, Min(0.01f)] private float hearingThreshold = 1f;

    [Tooltip("How sound loses loudness through the building. Keep in step with NoiseDebugView so what you see is what it hears.")]
    [SerializeField] private SoundPropagationSettings propagation = new SoundPropagationSettings();

    [Tooltip("Ignore noises whose Source is the Hunter itself (its own footsteps, doors it opens).")]
    [SerializeField] private bool ignoreOwnSounds = true;

    // Scoring and memory

    [Header("Scoring and memory")]
    [Tooltip("score = importance x heard loudness x e^(-age / this). Smaller = forgets faster.")]
    [SerializeField, Min(0.1f)] private float memoryTimeConstant = 12f;

    [Tooltip("How many remembered noises are kept. The lowest score is dropped first.")]
    [SerializeField, Range(1, 32)] private int memoryCapacity = 8;

    [Tooltip("A new noise only replaces the current target if its score is this much higher (0.25 = 25%).")]
    [SerializeField, Range(0f, 1f)] private float retargetMargin = 0.25f;

    [Tooltip("Best score needed to leave Patrol and go and investigate.")]
    [SerializeField, Min(0f)] private float investigateThreshold = 1.5f;

    [Header("Noise importance (0 to 1)")]
    [SerializeField, Range(0f, 1f)] private float gunshotImportance = 1f;
    [SerializeField, Range(0f, 1f)] private float barricadeBreakImportance = 0.8f;
    [SerializeField, Range(0f, 1f)] private float doorSlamImportance = 0.7f;
    [SerializeField, Range(0f, 1f)] private float thrownObjectImportance = 0.6f;
    [SerializeField, Range(0f, 1f)] private float footstepImportance = 0.4f;
    [SerializeField, Range(0f, 1f)] private float otherImportance = 0.3f;

    // Patrol, investigate, search

    [Header("Patrol, investigate, search")]
    [Tooltip("Pause at each patrol point before picking the next one.")]
    [SerializeField, Min(0f)] private float patrolWaitSeconds = 3f;

    [Tooltip("How close to a point counts as having arrived.")]
    [SerializeField, Min(0.1f)] private float arrivalRadius = 1f;

    [Tooltip("A new patrol point is at least this many metres (straight line) from where the Hunter stands, so it does not shuffle on the spot.")]
    [SerializeField, Min(0f)] private float patrolMinLegDistance = 6f;

    [Tooltip("Give up on a patrol point after this long (stuck, or the way is blocked) and pick another.")]
    [SerializeField, Min(5f)] private float patrolLegTimeoutSeconds = 45f;

    [Tooltip("How long the Hunter searches a spot before giving up and returning to Patrol.")]
    [SerializeField, Min(1f)] private float searchSeconds = 10f;

    [Tooltip("Search points are picked within this many metres (by path) of the spot the Hunter is searching.")]
    [SerializeField, Min(1f)] private float searchRadius = 8f;

    // Chase and attack

    [Header("Chase")]
    [Tooltip("A player noise at least this loud (as heard by the Hunter) starts a Chase.")]
    [SerializeField, Min(0f)] private float chaseLoudness = 12f;

    [Tooltip("The player this close by PATH (not straight line) also starts a Chase. Set to 0 to make the Hunter purely sound-driven.")]
    [SerializeField, Min(0f)] private float chaseProximity = 8f;

    [Tooltip("Silence this long during a Chase sends the Hunter to Search the last known spot.")]
    [SerializeField, Min(0.5f)] private float chaseLoseSeconds = 6f;

    [Header("Attack")]
    [Tooltip("Within this distance, with a fresh valid reach, the Hunter attacks.")]
    [SerializeField, Min(0.5f)] private float attackRange = 1.5f;

    [Tooltip("Hysteresis: Attack ends only when the player is further than attackRange + this.")]
    [SerializeField, Min(0f)] private float attackExitMargin = 0.5f;

    [SerializeField, Min(0f)] private float attackDamage = 20f;

    [SerializeField, Min(0.1f)] private float attackCooldownSeconds = 1.2f;

    [Tooltip("The player's position counts as a valid target only if it was confirmed within this many seconds. Stops attacks through walls or into vents.")]
    [SerializeField, Min(0.05f)] private float freshReachSeconds = 0.5f;

    // Health

    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    [Tooltip("Seconds of alert toward whoever shot the Hunter before it goes back to normal behaviour.")]
    [SerializeField, Min(0f)] private float shotAlertSeconds = 4f;

    // Pathfinding and doors

    [Header("Pathfinding")]
    [Tooltip("Extra path cost, in metres, for a closed door (the time spent opening it, as distance). Locked doors and intact barricades are blocked outright.")]
    [SerializeField, Min(0f)] private float closedDoorExtraCost = 3f;

    [Tooltip("Re-plan the path at most this often while walking, to react to doors and barricades changing.")]
    [SerializeField, Min(0.05f)] private float replanIntervalSeconds = 0.5f;

    [Header("Doors")]
    [Tooltip("How far in front of a door the Hunter stops before opening it.")]
    [SerializeField, Min(0.1f)] private float doorApproachDistance = 1.2f;

    [Tooltip("Give up waiting for a door to open after this long.")]
    [SerializeField, Min(0.5f)] private float doorOpenTimeoutSeconds = 3f;

    [Tooltip("A door that failed to open is avoided for this long before it is tried again.")]
    [SerializeField, Min(0.5f)] private float doorBanSeconds = 6f;

    [Tooltip("After this many failures in a row the Hunter stops trying that route and searches nearby instead, so it can never loop on one door.")]
    [SerializeField, Range(1, 10)] private int doorRetryLimit = 3;

    // Movement speed (the mover multiplies its base speed by these)

    [Header("Speed multipliers")]
    [SerializeField, Min(0.1f)] private float patrolSpeed = 0.7f;
    [SerializeField, Min(0.1f)] private float investigateSpeed = 1f;
    [SerializeField, Min(0.1f)] private float searchSpeed = 0.8f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 1.4f;

    // Debugging

    [Header("Debugging")]
    [SerializeField] private bool enableDebugLogging = false;

    // Read-only access

    public float HearingThreshold => hearingThreshold;
    public SoundPropagationSettings Propagation => propagation;
    public bool IgnoreOwnSounds => ignoreOwnSounds;

    public float MemoryTimeConstant => memoryTimeConstant;
    public int MemoryCapacity => memoryCapacity;
    public float RetargetMargin => retargetMargin;
    public float InvestigateThreshold => investigateThreshold;

    public float PatrolWaitSeconds => patrolWaitSeconds;
    public float ArrivalRadius => arrivalRadius;
    public float PatrolMinLegDistance => patrolMinLegDistance;
    public float PatrolLegTimeoutSeconds => patrolLegTimeoutSeconds;
    public float SearchSeconds => searchSeconds;
    public float SearchRadius => searchRadius;

    public float ChaseLoudness => chaseLoudness;
    public float ChaseProximity => chaseProximity;
    public float ChaseLoseSeconds => chaseLoseSeconds;

    public float AttackRange => attackRange;
    public float AttackExitRange => attackRange + attackExitMargin;
    public float AttackDamage => attackDamage;
    public float AttackCooldownSeconds => attackCooldownSeconds;
    public float FreshReachSeconds => freshReachSeconds;

    public float MaxHealth => maxHealth;
    public float ShotAlertSeconds => shotAlertSeconds;

    public float ClosedDoorExtraCost => closedDoorExtraCost;
    public float ReplanIntervalSeconds => replanIntervalSeconds;
    public float DoorApproachDistance => doorApproachDistance;
    public float DoorOpenTimeoutSeconds => doorOpenTimeoutSeconds;
    public float DoorBanSeconds => doorBanSeconds;
    public int DoorRetryLimit => doorRetryLimit;

    public float PatrolSpeed => patrolSpeed;
    public float InvestigateSpeed => investigateSpeed;
    public float SearchSpeed => searchSpeed;
    public float ChaseSpeed => chaseSpeed;

    public bool EnableDebugLogging => enableDebugLogging;

    /// <summary>How much a kind of noise matters to the Hunter, from 0 to 1.</summary>
    public float GetImportance(NoiseType type)
    {
        switch (type)
        {
            case NoiseType.Gunshot: return gunshotImportance;
            case NoiseType.BarricadeBreak: return barricadeBreakImportance;
            case NoiseType.DoorSlam: return doorSlamImportance;
            case NoiseType.ThrownObject: return thrownObjectImportance;
            case NoiseType.Footstep: return footstepImportance;
            default: return otherImportance;
        }
    }

    // Keeps values sensible if they are edited in an odd order in the Inspector.
    private void OnValidate()
    {
        chaseLoudness = Mathf.Max(chaseLoudness, hearingThreshold);
        attackRange = Mathf.Max(attackRange, 0.5f);
    }
}
