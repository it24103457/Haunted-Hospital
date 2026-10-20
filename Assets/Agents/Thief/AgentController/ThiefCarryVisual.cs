using System.Collections;
using UnityEngine;

/*
 * Puts the stolen item in the Thief's hand and gives the drop a small visual kick. On OnItemStolen the item
 * is parented to CarrySocket (snapped to the hand; its colliders are already off by MarkStolen). On
 * OnItemDropped the item is already detached and placed on the floor by ThiefAgent, so only its scale is
 * punched briefly - its position is never touched. Delivered items are destroyed by the game: nothing to do.
 */
public class ThiefCarryVisual : MonoBehaviour
{
    [Tooltip("Socket the stolen item is parented to; lives under the right hand bone.")]
    [SerializeField] private Transform carrySocket;

    [Tooltip("Seconds of the small scale bounce played on a dropped item.")]
    [SerializeField] private float dropPunchSeconds = 0.2f;

    private ThiefAgent thief;

    private void Awake()
    {
        thief = GetComponent<ThiefAgent>();
        if (carrySocket == null)
        {
            // Prefab places the socket under the model's hand bone; search the whole object.
            Transform[] all = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
                if (t.name == "CarrySocket") { carrySocket = t; break; }
        }
    }

    private void OnEnable()
    {
        thief.OnItemStolen += HandleStolen;
        thief.OnItemDropped += HandleDropped;
    }

    private void OnDisable()
    {
        thief.OnItemStolen -= HandleStolen;
        thief.OnItemDropped -= HandleDropped;
    }

    private void HandleStolen(PickupInteractable item)
    {
        if (carrySocket == null || item == null) return;
        item.transform.SetParent(carrySocket, false);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
    }

    private void HandleDropped(PickupInteractable item)
    {
        if (item != null) StartCoroutine(PunchScale(item.transform));
    }

    private IEnumerator PunchScale(Transform item)
    {
        Vector3 start = item.localScale;
        Vector3 punched = start * 1.15f;
        float t = 0f;
        while (t < dropPunchSeconds && item != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dropPunchSeconds);
            item.localScale = Vector3.Lerp(punched, start, k);
            yield return null;
        }
        if (item != null) item.localScale = start;
    }
}
