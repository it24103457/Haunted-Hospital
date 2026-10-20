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

    [Header("Stash Delivery Polish")]
    [Tooltip("Seconds into the Stash animation before the item releases into the container.")]
    [SerializeField] private float depositDelay = 0.85f;

    [Tooltip("Seconds to drop/sink the item into the stash opening.")]
    [SerializeField] private float depositDropSeconds = 0.2f;

    private ThiefAgent thief;
    private Coroutine pickupRoutine;
    private Coroutine deliverRoutine;
    private GameObject activeProxy;

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
        thief.OnItemDelivered += HandleDelivered;
    }

    private void OnDisable()
    {
        thief.OnItemStolen -= HandleStolen;
        thief.OnItemDropped -= HandleDropped;
        thief.OnItemDelivered -= HandleDelivered;
        if (pickupRoutine != null) { StopCoroutine(pickupRoutine); pickupRoutine = null; }
        if (deliverRoutine != null) { StopCoroutine(deliverRoutine); deliverRoutine = null; }
        if (activeProxy != null) { Destroy(activeProxy); activeProxy = null; }
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
        if (deliverRoutine != null) { StopCoroutine(deliverRoutine); deliverRoutine = null; }
        if (activeProxy != null) { Destroy(activeProxy); activeProxy = null; }
        if (item != null) StartCoroutine(PunchScale(item.transform));
    }

    private void HandleDelivered(PickupInteractable item)
    {
        if (carrySocket == null || item == null) return;
        if (deliverRoutine != null) StopCoroutine(deliverRoutine);
        deliverRoutine = StartCoroutine(AnimateStashDelivery(item));
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

    private IEnumerator AnimateStashDelivery(PickupInteractable item)
    {
        // Cancel any pending pickup routine and previous proxy
        if (pickupRoutine != null) { StopCoroutine(pickupRoutine); pickupRoutine = null; }
        if (activeProxy != null) Destroy(activeProxy);

        // Instantiate visual proxy before real item is destroyed
        activeProxy = Instantiate(item.gameObject, carrySocket);
        activeProxy.name = $"{item.name}_DeliveryProxy";
        activeProxy.transform.localPosition = item.transform.localPosition;
        activeProxy.transform.localRotation = item.transform.localRotation;
        activeProxy.transform.localScale = item.transform.localScale;

        // Strip colliders and interactive scripts from proxy so it is purely visual
        foreach (var col in activeProxy.GetComponentsInChildren<Collider>(true)) Destroy(col);
        foreach (var mb in activeProxy.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);

        // Hold the proxy in the hand as the Thief bends down
        if (depositDelay > 0f)
            yield return new WaitForSeconds(depositDelay);

        if (activeProxy == null) yield break;

        // Find the nearest stash container / drop point
        Transform stashContainer = FindNearestStashTarget();
        Vector3 targetDropPos = stashContainer != null
            ? stashContainer.position + Vector3.up * 0.15f
            : transform.position + transform.forward * 0.5f + Vector3.up * 0.2f;

        Vector3 startPos = activeProxy.transform.position;
        Vector3 startScale = activeProxy.transform.localScale;
        activeProxy.transform.SetParent(null, true);

        float elapsed = 0f;
        while (elapsed < depositDropSeconds)
        {
            if (activeProxy == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / depositDropSeconds);
            activeProxy.transform.position = Vector3.Lerp(startPos, targetDropPos, t);
            activeProxy.transform.localScale = Vector3.Lerp(startScale, startScale * 0.2f, t);
            yield return null;
        }

        if (stashContainer != null)
        {
            StartCoroutine(PunchScale(stashContainer));
        }

        if (activeProxy != null)
        {
            Destroy(activeProxy);
            activeProxy = null;
        }

        deliverRoutine = null;
    }

    private Transform FindNearestStashTarget()
    {
        // Search nearby colliders for stash, bag, or basket
        Collider[] colliders = Physics.OverlapSphere(transform.position, 3.5f);
        Transform closest = null;
        float minDist = float.MaxValue;
        foreach (var col in colliders)
        {
            string n = col.name.ToLower();
            if (n.Contains("stash") || n.Contains("bag") || n.Contains("basket") || n.Contains("droppoint"))
            {
                float d = Vector3.Distance(transform.position, col.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = col.transform;
                }
            }
        }
        if (closest != null) return closest;

        // Fallback: search scene transforms
        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        foreach (var t in all)
        {
            string n = t.name.ToLower();
            if (n.Contains("stash") || n.Contains("bag") || n.Contains("basket") || n.Contains("droppoint"))
            {
                float d = Vector3.Distance(transform.position, t.position);
                if (d < 3.5f && d < minDist)
                {
                    minDist = d;
                    closest = t;
                }
            }
        }
        return closest;
    }
}
