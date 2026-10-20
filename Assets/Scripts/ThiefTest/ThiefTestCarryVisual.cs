using UnityEngine;

/*
 * Test-only stand-in for the Agent Controller's carry visuals, used only on the Thief in the Thief test scene.
 * When an item is marked stolen near this Thief, the item is attached to the Thief's CarrySocket so it rides
 * along with the Thief. Releasing the item detaches it again (PickupInteractable.Release),
 * and delivering it at a stash destroys it, so nothing else is needed here.
 * When the Agent Controller's ThiefCarryVisual is on the same Thief, this one switches itself off, so only one
 * script ever moves a carried item.
 */
public class ThiefTestCarryVisual : MonoBehaviour
{
    [Tooltip("Where carried items are attached. Found anywhere under the Thief by the name CarrySocket when left empty.")]
    [SerializeField] private Transform carrySocket;
    [Tooltip("Only items stolen within this distance of the Thief are attached, so other thieves' items are left alone.")]
    [SerializeField] private float claimDistance = 3f;

    /*
     * Steps aside when the real carry visuals are present. Otherwise finds the CarrySocket when none is assigned,
     * searching the whole Thief because the socket can sit under a bone of the rigged model, and warns when there is none.
     */
    private void Awake()
    {
        if (GetComponent("ThiefCarryVisual") != null)
        {
            enabled = false;
            return;
        }

        if (carrySocket == null) carrySocket = FindCarrySocket(transform);
        if (carrySocket == null) Debug.LogWarning($"ThiefTestCarryVisual on '{name}' has no CarrySocket.", this);
    }

    // Listens for stolen items only while enabled, so a disabled tool never moves anything.
    private void OnEnable() => PickupInteractable.AnyStolen += OnItemStolen;
    private void OnDisable() => PickupInteractable.AnyStolen -= OnItemStolen;

    // Attaches a stolen item to the CarrySocket when it was stolen close to this Thief.
    private void OnItemStolen(PickupInteractable item)
    {
        if (carrySocket == null) return;
        if (Vector3.Distance(item.transform.position, transform.position) > claimDistance) return;

        item.transform.SetParent(carrySocket, false);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
    }

    // The first object named CarrySocket anywhere under the given root, or null when there is none.
    private static Transform FindCarrySocket(Transform root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == "CarrySocket") return t;
        return null;
    }
}
