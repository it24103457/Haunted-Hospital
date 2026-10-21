using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ObjectiveSpawner manages sequential clue paper spawning and the exit key placement
/// according to the Hospital Escape Objectives role plan (Section 4).
///
/// Lifecycle:
/// 1. At start, generates a 4-digit code (e.g. "0429"), assigns it to ObjectiveState.SetCode,
///    spawns Paper 1 on a safe, reachable SP_C* marker, and announces the start objective.
/// 2. Subscribes to ObjectiveState.DigitFound. When the expected paper is read:
///    - Papers 1 to 3: immediately spawns the next paper on an unused SP_C* marker and shows progress.
///    - Paper 4: announces the keypad objective.
/// 3. Subscribes to ObjectiveState.StateChanged. When IsCodeAccepted becomes true:
///    - Immediately spawns the Exit Key on an approved SP_K* marker.
/// 4. Subscribes to PlayerStats.ExitKeyChanged. When the key is collected:
///    - Shows the instruction to use the elevator key switch.
/// </summary>
[DisallowMultipleComponent]
public class ObjectiveSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The central ObjectiveState. Found automatically if left empty.")]
    [SerializeField] private ObjectiveState objectives;

    [Tooltip("The player stats component for distance checking and key possession. Found automatically if left empty.")]
    [SerializeField] private PlayerStats player;

    [Tooltip("Prefab for digit clues (Assets/Prefabs/Hospital/Props/PF_DigitPaper.prefab).")]
    [SerializeField] private GameObject paperPrefab;

    [Tooltip("Prefab for exit key (Assets/Prefabs/Hospital/Props/PF_ExitKey.prefab).")]
    [SerializeField] private GameObject keyPrefab;

    [Header("Marker Settings")]
    [Tooltip("Name prefix for clue paper spawn markers (e.g. SP_C).")]
    [SerializeField] private string paperMarkerPrefix = "SP_C";

    [Tooltip("Name prefix for exit key spawn markers (e.g. SP_K).")]
    [SerializeField] private string keyMarkerPrefix = "SP_K";

    [Tooltip("Optional list of objective marker names that sit behind barricades.")]
    [SerializeField] private string[] barricadeMarkerNames;

    [Tooltip("Metres: a barricaded marker binds to the nearest NavMeshObstacle within this range.")]
    [SerializeField, Min(0f)] private float barricadeSearchRadius = 5f;

    [Header("Placement Rules")]
    [Tooltip("Preferred minimum distance in metres from the player. Relaxed immediately if no marker qualifies.")]
    [SerializeField, Min(0f)] private float minSpawnDistance = 8f;

    [Tooltip("Selects randomly among the top N most isolated candidates.")]
    [SerializeField, Min(1)] private int spreadTopCount = 3;

    [Header("Debug")]
    [SerializeField] private bool logDetails = true;

    // Runtime state
    private readonly List<Transform> paperMarkers = new List<Transform>();
    private readonly List<Transform> keyMarkers = new List<Transform>();
    private readonly HashSet<string> usedPaperMarkers = new HashSet<string>();
    private readonly HashSet<string> barricadeNames = new HashSet<string>();
    private readonly Dictionary<string, GameObject> barricadeBindings = new Dictionary<string, GameObject>();

    // 0 = uninitialized, 1..4 = active stage waiting to be read, 5 = all 4 read (waiting for keypad)
    private int currentPaperStage = 0;
    private bool keySpawned = false;
    private bool exitKeyMessageShown = false;
    private bool isInitialized = false;
    private bool eventsSubscribed = false;
    private Coroutine retryCoroutine = null;

    public int CurrentPaperStage => currentPaperStage;
    public bool IsKeySpawned => keySpawned;
    public IReadOnlyCollection<string> UsedPaperMarkers => usedPaperMarkers;

    private void Awake()
    {
        if (objectives == null) objectives = FindAnyObjectByType<ObjectiveState>();
        if (player == null) player = FindAnyObjectByType<PlayerStats>();

        FindMarkers();
        BindBarricades();
    }

    private void OnEnable()
    {
        SubscribeEvents();

        if (isInitialized)
        {
            ReconcileState();
        }
    }

    private void Start()
    {
        InitializeRun();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (eventsSubscribed) return;

        if (objectives == null) objectives = FindFirstObjectByType<ObjectiveState>();
        if (player == null) player = FindFirstObjectByType<PlayerStats>();

        if (objectives != null)
        {
            objectives.DigitFound += OnDigitFound;
            objectives.StateChanged += OnStateChanged;
        }

        if (player != null)
        {
            player.ExitKeyChanged += OnExitKeyChanged;
            player.Died += OnPlayerDied;
        }

        eventsSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!eventsSubscribed) return;

        if (objectives != null)
        {
            objectives.DigitFound -= OnDigitFound;
            objectives.StateChanged -= OnStateChanged;
        }

        if (player != null)
        {
            player.ExitKeyChanged -= OnExitKeyChanged;
            player.Died -= OnPlayerDied;
        }

        eventsSubscribed = false;
    }

    private void InitializeRun()
    {
        if (isInitialized) return;
        isInitialized = true;

        if (objectives == null) objectives = FindAnyObjectByType<ObjectiveState>();
        if (player == null) player = FindAnyObjectByType<PlayerStats>();

        if (objectives == null)
        {
            Debug.LogWarning("[ObjectiveSpawner] No ObjectiveState found in scene. Objective flow cannot start.", this);
            return;
        }

        if (objectives.HasWon || objectives.IsEscapeInProgress) return;
        if (player != null && player.IsDead) return;

        // Generate or retain 4-digit code
        string code = objectives.Code;
        if (string.IsNullOrEmpty(code) || code.Length != 4)
        {
            code = GenerateCode();
            objectives.SetCode(code);
            if (logDetails) Debug.Log($"[ObjectiveSpawner] Generated code: {code}", this);
        }
        else
        {
            if (logDetails) Debug.Log($"[ObjectiveSpawner] Using established code: {code}", this);
        }

        ReconcileState();

        // Start fresh sequence if no clues have been discovered yet
        if (currentPaperStage == 0 && objectives.CollectedClueCount == 0)
        {
            currentPaperStage = 1;
            SpawnPaper(1);
            ShowHudMessage("Find the first code paper somewhere in the hospital.");
        }
    }

    /// <summary>
    /// Reconciles spawner state with the ObjectiveState without replaying past events.
    /// </summary>
    private void ReconcileState()
    {
        if (objectives == null) return;

        int discoveredCount = 0;
        for (int pos = 1; pos <= 4; pos++)
        {
            if (objectives.TryGetDigit(pos, out _))
            {
                discoveredCount++;
            }
        }

        if (discoveredCount >= 4)
        {
            currentPaperStage = 5;
            if (objectives.IsCodeAccepted && !keySpawned)
            {
                SpawnExitKey();
            }
        }
        else if (discoveredCount > 0 && currentPaperStage <= discoveredCount)
        {
            currentPaperStage = discoveredCount + 1;
            if (currentPaperStage <= 4)
            {
                SpawnPaper(currentPaperStage);
            }
        }

        if (player != null && player.HasExitKey)
        {
            exitKeyMessageShown = true;
        }
    }

    private void OnDigitFound(int positionInCode, int digit)
    {
        // Ignore rereads, duplicate notifications, or older positions
        if (positionInCode != currentPaperStage)
        {
            if (logDetails)
            {
                Debug.Log($"[ObjectiveSpawner] Ignored DigitFound(pos:{positionInCode}, digit:{digit}) - current expected stage is {currentPaperStage}.", this);
            }
            return;
        }

        if (positionInCode >= 1 && positionInCode <= 3)
        {
            currentPaperStage = positionInCode + 1;
            ShowHudMessage($"Digit found: {digit}. Find the next code paper.");
            SpawnPaper(currentPaperStage);
        }
        else if (positionInCode == 4)
        {
            currentPaperStage = 5;
            ShowHudMessage("Code complete. Enter it at the elevator keypad.");
        }
    }

    private void OnStateChanged()
    {
        if (objectives == null) return;

        if (objectives.IsCodeAccepted && !keySpawned)
        {
            SpawnExitKey();
        }
    }

    private void OnExitKeyChanged(bool held)
    {
        if (held && !exitKeyMessageShown)
        {
            exitKeyMessageShown = true;
            ShowHudMessage("Exit key collected. Use the key switch inside the elevator.");
        }
    }

    private void OnPlayerDied()
    {
        if (retryCoroutine != null)
        {
            StopCoroutine(retryCoroutine);
            retryCoroutine = null;
        }
    }

    private bool SpawnPaper(int stage)
    {
        if (paperPrefab == null)
        {
            Debug.LogError("[ObjectiveSpawner] Paper prefab is not assigned! Assign Assets/Prefabs/Hospital/Props/PF_DigitPaper.prefab.", this);
            return false;
        }

        if (objectives != null && (objectives.HasWon || objectives.IsEscapeInProgress)) return false;
        if (player != null && player.IsDead) return false;

        Transform marker = SelectMarker(paperMarkers, usedPaperMarkers);
        if (marker == null)
        {
            Debug.LogWarning($"[ObjectiveSpawner] No valid, reachable paper marker found for stage {stage}. Will retry...", this);
            ScheduleRetry(stage);
            return false;
        }

        usedPaperMarkers.Add(marker.name);

        GameObject paperObj = Instantiate(paperPrefab, marker.position, marker.rotation);
        paperObj.name = $"PF_DigitPaper_Stage{stage}";

        int digit = 0;
        if (!string.IsNullOrEmpty(objectives.Code) && stage - 1 < objectives.Code.Length)
        {
            digit = objectives.Code[stage - 1] - '0';
        }

        ClueInteractable clue = paperObj.GetComponentInChildren<ClueInteractable>();
        if (clue == null)
        {
            clue = paperObj.AddComponent<ClueInteractable>();
        }
        clue.Configure(stage, digit);

        if (logDetails)
        {
            Debug.Log($"[ObjectiveSpawner] Spawned Paper {stage} (digit {digit}) at {marker.name} {DistanceText(marker)}", this);
        }

        return true;
    }

    private bool SpawnExitKey()
    {
        if (keySpawned) return true;

        if (keyPrefab == null)
        {
            Debug.LogError("[ObjectiveSpawner] Key prefab is not assigned! Assign Assets/Prefabs/Hospital/Props/PF_ExitKey.prefab.", this);
            return false;
        }

        if (objectives != null && (objectives.HasWon || objectives.IsEscapeInProgress)) return false;
        if (player != null && player.IsDead) return false;

        var dummyUsed = new HashSet<string>();
        Transform marker = SelectMarker(keyMarkers, dummyUsed);
        if (marker == null)
        {
            Debug.LogWarning("[ObjectiveSpawner] No valid, reachable key marker found for Exit Key. Will retry...", this);
            ScheduleRetryKey();
            return false;
        }

        keySpawned = true;

        GameObject keyObj = Instantiate(keyPrefab, marker.position, marker.rotation);
        keyObj.name = "PF_ExitKey_Spawned";

        ShowHudMessage("Elevator unlocked. Find the exit key somewhere in the hospital.");

        if (logDetails)
        {
            Debug.Log($"[ObjectiveSpawner] Spawned Exit Key at {marker.name} {DistanceText(marker)}", this);
        }

        return true;
    }

    /// <summary>
    /// Selects an optimal marker: excludes used/occupied/barricaded, prefers distance >= minSpawnDistance,
    /// relaxes distance if none meet it, and picks randomly among the top N most-isolated candidates.
    /// </summary>
    private Transform SelectMarker(List<Transform> pool, HashSet<string> excludedNames)
    {
        if (pool == null || pool.Count == 0) return null;

        var valid = new List<Transform>();
        foreach (Transform m in pool)
        {
            if (m == null) continue;
            if (excludedNames.Contains(m.name)) continue;
            if (!IsMarkerReachable(m)) continue;
            if (IsMarkerOccupied(m)) continue;
            valid.Add(m);
        }

        if (valid.Count == 0) return null;

        // Distance preference (>= minSpawnDistance)
        List<Transform> distant = new List<Transform>();
        if (player != null && minSpawnDistance > 0f)
        {
            Vector3 playerPos = player.transform.position;
            foreach (Transform m in valid)
            {
                if (Vector3.Distance(m.position, playerPos) >= minSpawnDistance)
                    distant.Add(m);
            }
        }

        // If distance filter leaves none, relax distance immediately
        List<Transform> candidates = distant.Count > 0 ? distant : valid;

        // Rank by isolation from player and other objects
        List<Vector3> obstacles = CollectObstacles();
        return ItemSpawnDirector.PickMostIsolated(candidates, obstacles, spreadTopCount);
    }

    private bool IsMarkerReachable(Transform m)
    {
        if (m == null) return false;

        // If ItemSpawnDirector is in the scene and knows this marker name, use its check
        ItemSpawnDirector director = FindAnyObjectByType<ItemSpawnDirector>();
        if (director != null && !director.IsReachableMarker(m.name))
        {
            return false;
        }

        // Check explicit barricade bindings
        if (barricadeNames.Contains(m.name))
        {
            if (!barricadeBindings.TryGetValue(m.name, out GameObject barricade) || barricade == null)
                return false; // unbound conservative rule: intact until proven open

            var obstacle = barricade.GetComponent<NavMeshObstacle>();
            if (barricade.activeInHierarchy && obstacle != null && obstacle.enabled)
                return false;
        }

        return true;
    }

    private bool IsMarkerOccupied(Transform m)
    {
        Collider[] hits = Physics.OverlapSphere(m.position, 0.35f);
        foreach (Collider hit in hits)
        {
            if (hit.GetComponentInParent<ClueInteractable>() != null ||
                hit.GetComponentInParent<PickupInteractable>() != null)
            {
                return true;
            }
        }
        return false;
    }

    private List<Vector3> CollectObstacles()
    {
        var pts = new List<Vector3>();
        if (player != null) pts.Add(player.transform.position);

        foreach (ClueInteractable clue in FindObjectsByType<ClueInteractable>(FindObjectsInactive.Exclude))
        {
            if (clue != null) pts.Add(clue.transform.position);
        }

        foreach (PickupInteractable pickup in FindObjectsByType<PickupInteractable>(FindObjectsInactive.Exclude))
        {
            if (pickup != null) pts.Add(pickup.transform.position);
        }

        return pts;
    }

    private void FindMarkers()
    {
        paperMarkers.Clear();
        keyMarkers.Clear();

        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (t.name.StartsWith(paperMarkerPrefix)) paperMarkers.Add(t);
            else if (t.name.StartsWith(keyMarkerPrefix)) keyMarkers.Add(t);
        }

        if (logDetails)
        {
            Debug.Log($"[ObjectiveSpawner] Found {paperMarkers.Count} paper markers ({paperMarkerPrefix}*) " +
                      $"and {keyMarkers.Count} key markers ({keyMarkerPrefix}*).", this);
        }
    }

    private void BindBarricades()
    {
        barricadeNames.Clear();
        barricadeBindings.Clear();

        if (barricadeMarkerNames != null)
        {
            foreach (string name in barricadeMarkerNames)
            {
                if (!string.IsNullOrEmpty(name)) barricadeNames.Add(name);
            }
        }

        if (barricadeNames.Count == 0) return;

        var obstacles = FindObjectsByType<NavMeshObstacle>(FindObjectsInactive.Exclude);
        var allMarkers = new List<Transform>(paperMarkers);
        allMarkers.AddRange(keyMarkers);

        foreach (Transform m in allMarkers)
        {
            if (!barricadeNames.Contains(m.name)) continue;

            NavMeshObstacle best = null;
            float bestDistance = barricadeSearchRadius;
            foreach (NavMeshObstacle obstacle in obstacles)
            {
                if (obstacle.GetComponentInParent<Door>() != null) continue;
                float d = Vector3.Distance(m.position, obstacle.transform.position);
                if (d <= bestDistance)
                {
                    bestDistance = d;
                    best = obstacle;
                }
            }

            if (best != null) barricadeBindings[m.name] = best.gameObject;
        }
    }

    private void ScheduleRetry(int stage)
    {
        if (retryCoroutine != null) StopCoroutine(retryCoroutine);
        retryCoroutine = StartCoroutine(RetryRoutine(stage));
    }

    private IEnumerator RetryRoutine(int stage)
    {
        yield return new WaitForSeconds(1f);
        retryCoroutine = null;
        if (currentPaperStage == stage)
        {
            SpawnPaper(stage);
        }
    }

    private void ScheduleRetryKey()
    {
        if (retryCoroutine != null) StopCoroutine(retryCoroutine);
        retryCoroutine = StartCoroutine(RetryKeyRoutine());
    }

    private IEnumerator RetryKeyRoutine()
    {
        yield return new WaitForSeconds(1f);
        retryCoroutine = null;
        if (!keySpawned && objectives != null && objectives.IsCodeAccepted)
        {
            SpawnExitKey();
        }
    }

    private static string GenerateCode()
    {
        char[] digits = new char[4];
        for (int i = 0; i < 4; i++)
        {
            digits[i] = (char)('0' + Random.Range(0, 10));
        }
        return new string(digits);
    }

    private void ShowHudMessage(string text)
    {
        ObjectiveHud hud = ObjectiveHud.Instance;
        if (hud != null)
        {
            hud.ShowMessage(text);
        }
        else
        {
            if (logDetails) Debug.Log($"[ObjectiveSpawner HUD Message] {text}", this);
        }
    }

    private string DistanceText(Transform marker)
    {
        return player != null
            ? $"({Vector3.Distance(marker.position, player.transform.position):0.0}m from player)"
            : string.Empty;
    }

    // Testing helpers
    [ContextMenu("Debug: Dump State")]
    private void DebugDumpState()
    {
        Debug.Log($"[ObjectiveSpawner] Initialized: {isInitialized}, Code: '{objectives?.Code}', " +
                  $"Stage: {currentPaperStage}, KeySpawned: {keySpawned}, " +
                  $"PaperMarkers: {paperMarkers.Count}, KeyMarkers: {keyMarkers.Count}, " +
                  $"UsedPaperMarkers: {usedPaperMarkers.Count}", this);
    }
}
