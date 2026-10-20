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

    [Header("Pickup Timing")]
    [Tooltip("Delay in seconds before the item attaches to the hand, matching the reach-down moment of the grab animation.")]
    [SerializeField] private float grabPickupDelay = 0.38f;

    [Tooltip("Seconds to smoothly interpolate the item from the ground into the hand socket.")]
    [SerializeField] private float pickupInterpolateSeconds = 0.12f;

    private ThiefAgent thief;
    private Coroutine pickupRoutine;

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
        if (pickupRoutine != null) { StopCoroutine(pickupRoutine); pickupRoutine = null; }
    }

    private void HandleStolen(PickupInteractable item)
    {
        if (carrySocket == null || item == null) return;
        if (pickupRoutine != null) StopCoroutine(pickupRoutine);
        pickupRoutine = StartCoroutine(SmoothPickupItem(item));
    }

    private void HandleDropped(PickupInteractable item)
    {
        if (pickupRoutine != null) { StopCoroutine(pickupRoutine); pickupRoutine = null; }
        if (item != null) StartCoroutine(PunchScale(item.transform));
    }

    private IEnumerator SmoothPickupItem(PickupInteractable item)
    {
        Transform itemTransform = item.transform;

        // Leave item in place on the floor while the Thief bends down and reaches
        if (grabPickupDelay > 0f)
            yield return new WaitForSeconds(grabPickupDelay);

        if (item == null || itemTransform == null || carrySocket == null) yield break;

        Vector3 startWorldPos = itemTransform.position;
        Quaternion startWorldRot = itemTransform.rotation;

        // Parent to socket preserving world position during the blend
        itemTransform.SetParent(carrySocket, true);

        float elapsed = 0f;
        while (elapsed < pickupInterpolateSeconds)
        {
            if (item == null || itemTransform == null || carrySocket == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / pickupInterpolateSeconds);
            itemTransform.position = Vector3.Lerp(startWorldPos, carrySocket.position, t);
            itemTransform.rotation = Quaternion.Slerp(startWorldRot, carrySocket.rotation, t);
            yield return null;
        }

        if (itemTransform != null)
        {
            itemTransform.localPosition = Vector3.zero;
            itemTransform.localRotation = Quaternion.identity;
            StartCoroutine(PunchScale(itemTransform));
        }

        pickupRoutine = null;
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
