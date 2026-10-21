using UnityEngine;

// The small cooler boxes that hide an exit-key spot (PF_CoolerBox). Press E to lift the lid.
//
// The Auto_Colliders box covers the whole cooler, so it also covers the key inside. While the lid is closed that
// is what we want (the player looks at the lid and gets the prompt). Once the lid is open the box is shrunk to
// a thin slice at the bottom, below the key, so the E ray reaches the key's own collider.
public class CoolerBoxInteractable : OpenableInteractable
{
    [Header("Body collider")]
    [Tooltip("The box that covers the whole cooler. Found automatically (child 'Auto_Colliders') if left empty.")]
    [SerializeField] private BoxCollider bodyCollider;

    [Tooltip("Height of the slice that stays solid after the lid opens, measured from the cooler's base. " +
             "Must be BELOW the bottom of the key (the key marker sits about 0.11 m up).")]
    [SerializeField, Min(0.01f)] private float openBodyHeight = 0.07f;

    protected override string DefaultOpenParameter => "OpenLid";
    protected override string DefaultDisplayName => "the cooler";

    protected override void Awake()
    {
        base.Awake();

        if (bodyCollider == null)
        {
            Transform t = transform.Find("Auto_Colliders");
            if (t != null) bodyCollider = t.GetComponent<BoxCollider>();
        }
        if (bodyCollider == null) bodyCollider = GetComponentInChildren<BoxCollider>();
    }

    protected override void OnOpened()
    {
        if (bodyCollider == null) return;

        // Keep the bottom edge where it is, only lower the top edge.
        Vector3 size = bodyCollider.size;
        float bottom = bodyCollider.center.y - size.y * 0.5f;
        float height = Mathf.Min(openBodyHeight, size.y);

        Vector3 center = bodyCollider.center;
        center.y = bottom + height * 0.5f;
        size.y = height;

        bodyCollider.center = center;
        bodyCollider.size = size;
    }
}
