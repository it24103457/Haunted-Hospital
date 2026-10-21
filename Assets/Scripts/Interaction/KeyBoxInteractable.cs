using UnityEngine;

// The wall-mounted key boxes (KeyBox prefab, K06-K09). Press E to open the door.
//
// The KeyBox prefab has no colliders at all, so without help the E ray passes straight through it. If no solid
// collider exists on this prefab, one is built from the visible meshes. After the door opens that box is
// switched off (the box hangs on a wall, nobody walks into it), so the E ray reaches the key inside.
public class KeyBoxInteractable : OpenableInteractable
{
    [Header("Body collider")]
    [Tooltip("The collider that makes the closed box targetable. Built automatically from the meshes if the prefab " +
             "has no solid collider. Switched off once the box is open.")]
    [SerializeField] private Collider bodyCollider;

    protected override string DefaultOpenParameter => "KeyBoxOpen";
    protected override string DefaultDisplayName => "the box";

    protected override void Awake()
    {
        base.Awake();

        if (bodyCollider == null) bodyCollider = FindSolidCollider();
        if (bodyCollider == null) bodyCollider = BuildBodyCollider();
    }

    protected override void OnOpened()
    {
        if (bodyCollider != null) bodyCollider.enabled = false;
    }

    private Collider FindSolidCollider()
    {
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            if (c.isTrigger) continue;

            // The key (or any other pickup) sits inside the box as a child. It is not the box body.
            if (c.GetComponentInParent<PickupInteractable>() != null) continue;

            return c;
        }
        return null;
    }

    // One BoxCollider on a child object that fits all renderers (the key is not a renderer of this prefab yet).
    private Collider BuildBodyCollider()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning($"KeyBoxInteractable '{name}' has no Renderer and no Collider, so the player cannot target it.", this);
            return null;
        }

        // Bounds in this object's local space, so the box follows the prop if it is rotated on the wall.
        Bounds local = new Bounds();
        bool first = true;
        foreach (Renderer r in renderers)
        {
            Bounds lb = r.localBounds;
            Vector3 c = lb.center;
            Vector3 e = lb.extents;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = c + new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);
                Vector3 p = transform.InverseTransformPoint(r.transform.TransformPoint(corner));

                if (first) { local = new Bounds(p, Vector3.zero); first = false; }
                else local.Encapsulate(p);
            }
        }

        GameObject holder = new GameObject("Auto_Collider");
        holder.transform.SetParent(transform, false);
        BoxCollider box = holder.AddComponent<BoxCollider>();
        box.center = local.center;
        box.size = local.size;
        return box;
    }
}
