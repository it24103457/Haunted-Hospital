using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Test-only stand-in for the Item Spawn Director, used only in the Thief test scene.
 * Keeps a fixed number of supply items (Medkit, AmmoBox, SanityBottle) alive on random free SP_S markers.
 * When an item is collected, its marker is freed and a new item appears after a delay, which is longer
 * when the item was stolen (WasStolen) than when the player picked it up.
 * It only uses PickupInteractable's public API, so the real spawner can replace it without Thief changes.
 */
public class ThiefTestSpawner : MonoBehaviour
{
    [Tooltip("Item prefabs to choose from at random.")]
    [SerializeField] private GameObject[] itemPrefabs;
    [Tooltip("Name prefix of the spawn markers searched for in the scene.")]
    [SerializeField] private string markerPrefix = "SP_S";
    [Tooltip("Number of items kept alive at once.")]
    [SerializeField] private int targetCount = 6;
    [Tooltip("Seconds before a replacement appears after the player collects an item.")]
    [SerializeField] private float respawnDelay = 10f;
    [Tooltip("Seconds before a replacement appears after the Thief delivers a stolen item.")]
    [SerializeField] private float stolenRespawnDelay = 20f;

    private readonly List<Transform> markers = new List<Transform>();
    private readonly Dictionary<PickupInteractable, Transform> occupied = new Dictionary<PickupInteractable, Transform>();

    private void OnEnable() => PickupInteractable.AnyCollected += OnItemCollected;
    private void OnDisable() => PickupInteractable.AnyCollected -= OnItemCollected;

    private void Start()
    {
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            if (t.name.StartsWith(markerPrefix)) markers.Add(t);

        if (markers.Count == 0 || itemPrefabs == null || itemPrefabs.Length == 0)
        {
            Debug.LogWarning($"ThiefTestSpawner: found {markers.Count} '{markerPrefix}' markers and " +
                             $"{(itemPrefabs == null ? 0 : itemPrefabs.Length)} prefabs, so nothing spawns.", this);
            return;
        }

        for (int i = 0; i < targetCount; i++) SpawnOne();
    }

    // Places a random item prefab on a random marker that has no item on it.
    private void SpawnOne()
    {
        List<Transform> free = new List<Transform>();
        foreach (Transform m in markers)
            if (!occupied.ContainsValue(m)) free.Add(m);
        if (free.Count == 0) return;

        Transform marker = free[Random.Range(0, free.Count)];
        GameObject prefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];

        GameObject item = Instantiate(prefab, marker.position, prefab.transform.rotation, transform);
        PickupInteractable pickup = item.GetComponent<PickupInteractable>();
        if (pickup == null)
        {
            Debug.LogWarning($"ThiefTestSpawner: prefab '{prefab.name}' has no PickupInteractable.", this);
            Destroy(item);
            return;
        }

        occupied[pickup] = marker;
    }

    // Frees the item's marker and schedules a replacement; stolen items take longer to come back.
    private void OnItemCollected(PickupInteractable pickup)
    {
        if (!occupied.Remove(pickup)) return;

        StartCoroutine(RespawnAfter(pickup.WasStolen ? stolenRespawnDelay : respawnDelay));
    }

    private IEnumerator RespawnAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnOne();
    }
}
