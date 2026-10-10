using System;
using System.Collections;
using UnityEngine;

// One script for every collectable: Medkit (Health), AmmoBox (Ammo), SanityBottle (Sanity), PF_ExitKey (ExitKey).
// Put it on the prefab, pick the Type, set the Amount. Works only once.
// How it fits together:
//  Player interaction: Detects the item through a non-trigger collider and calls Interact().
//  Pickup process: Claims the item, plays its opening animation if available, waits briefly, gives the item to the player, and removes it.
//  AI interaction: Thieves can also claim items, preventing both the player and thief from collecting the same item.
public class PickupInteractable : MonoBehaviour, IInteractable
{
    [Header("Item")]
    [SerializeField] private PickupType type = PickupType.Health;
    [Tooltip("Health / Sanity points, or number of rounds for Ammo. Ignored for ExitKey.")]
    [SerializeField] private int amount = 25;
    [Tooltip("Optional. Name shown in the prompt. Leave empty to use the default for the type.")]
    [SerializeField] private string displayName = "";

    [Header("Open animation (optional)")]
    [Tooltip("Animator on this prefab. Found automatically in children if left empty. No Animator = collected instantly.")]
    [SerializeField] private Animator animator;
    [Tooltip("Animator parameter (Bool or Trigger) to switch on at pickup. Leave empty to use the default for the type " +
             "(MedkitOpen, AmmoBoxOpen, SanityBottleOpen).")]
    [SerializeField] private string animationTrigger = "";
    [Tooltip("Seconds to wait after the animation starts before the item is given and removed. Set to the length of the open clip.")]
    [SerializeField] private float collectDelay = 0.8f;

    [Header("On collect")]
    [Tooltip("Destroy the object (matches 'remove the world instance'). Untick to just disable it.")]
    [SerializeField] private bool destroyOnCollect = true;

    // Raised on this pickup when it is collected.
    public event Action<PickupInteractable> Collected;

    // Raised for every pickup. The spawn manager can subscribe once to clear markers.
    public static event Action<PickupInteractable> AnyCollected;

    public PickupType Type => type;
    public int Amount => amount;

    // True from the moment E is pressed (or a thief claims it), including during the open animation.
    public bool IsCollected { get; private set; }

    private bool warnedNoReceiver;

    // Keeps the static event clean if "Enter Play Mode Options" has domain reload turned off.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => AnyCollected = null;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        ResetOpenParameter();

        Collider[] colliders = GetComponentsInChildren<Collider>();
        bool hasSolid = false;
        foreach (Collider c in colliders)
        {
            if (!c.isTrigger) { hasSolid = true; break; }
        }

        if (!hasSolid)
        {
            Debug.LogWarning($"PickupInteractable '{name}' has no solid (non-trigger) Collider. " +
                             "PlayerInteractor ignores triggers, so the player cannot pick this up.", this);
        }
    }

    private void Start()
    {
        // Second safety net, in case the Animator initialised after Awake.
        ResetOpenParameter();
    }

    // IInteractable 

    public string GetPrompt()
    {
        if (IsCollected) return string.Empty;
        return $"Press E to pick up {Label}";
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (IsCollected) return;

        IPickupReceiver receiver = FindReceiver(interactor);
        if (receiver == null)
        {
            if (!warnedNoReceiver)
            {
                warnedNoReceiver = true;
                Debug.LogWarning("No IPickupReceiver found on the player. Add the player-state script " +
                                 "(or DebugPickupReceiver for testing) to the Player.", this);
            }
            return; // do not consume the item if nothing can receive it
        }

        if (!receiver.CanReceive(type, amount)) return;

        if (!TryClaim()) return; // from here on nobody else can take it

        if (TryPlayOpenAnimation() && collectDelay > 0f)
            StartCoroutine(FinishAfterDelay(receiver));
        else
            Finish(receiver);
    }

    // Public API for other systems (thief, spawn manager)

   
    /// Reserve this item. Returns true for the first caller only, false afterwards.
    /// A thief should call this, and call Remove() if it returns true.
    public bool TryClaim()
    {
        if (IsCollected) return false;
        IsCollected = true;
        return true;
    }

    // Raise the collected events and remove the item from the world. Call after TryClaim().
    public void Remove()
    {
        IsCollected = true;
        Collected?.Invoke(this);
        AnyCollected?.Invoke(this);

        if (destroyOnCollect) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    // Internals

    private IEnumerator FinishAfterDelay(IPickupReceiver receiver)
    {
        yield return new WaitForSeconds(collectDelay);
        Finish(receiver);
    }

    private void Finish(IPickupReceiver receiver)
    {
        // The player could have been destroyed during the delay, skip the effect then.
        bool receiverAlive = !(receiver is UnityEngine.Object o) || o != null;
        if (receiverAlive) receiver.Receive(type, amount);

        Remove();
    }

    // Returns true if an animation was started. Works with Bool or Trigger parameters.
    private bool TryPlayOpenAnimation()
    {
        if (!TryGetOpenParameter(out string paramName, out AnimatorControllerParameterType paramType))
            return false;

        if (paramType == AnimatorControllerParameterType.Trigger) animator.SetTrigger(paramName);
        else animator.SetBool(paramName, true);
        return true;
    }

    // The prefab controllers have their open parameter set to TRUE by default, which makes the lid open
    // by itself when the scene starts. Force it back to "closed" so the item stays closed until E is pressed.
    private void ResetOpenParameter()
    {
        if (IsCollected) return;
        if (!TryGetOpenParameter(out string paramName, out AnimatorControllerParameterType paramType))
            return;

        if (paramType == AnimatorControllerParameterType.Trigger) animator.ResetTrigger(paramName);
        else animator.SetBool(paramName, false);
    }

    private bool TryGetOpenParameter(out string paramName, out AnimatorControllerParameterType paramType)
    {
        paramName = null;
        paramType = AnimatorControllerParameterType.Bool;

        if (animator == null || animator.runtimeAnimatorController == null) return false;

        string wanted = string.IsNullOrEmpty(animationTrigger) ? DefaultTrigger : animationTrigger;
        if (string.IsNullOrEmpty(wanted)) return false;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name != wanted) continue;

            if (p.type == AnimatorControllerParameterType.Bool || p.type == AnimatorControllerParameterType.Trigger)
            {
                paramName = wanted;
                paramType = p.type;
                return true;
            }
        }

        return false;
    }

    private string DefaultTrigger
    {
        get
        {
            switch (type)
            {
                case PickupType.Health: return "MedkitOpen";
                case PickupType.Ammo: return "AmmoBoxOpen";
                case PickupType.Sanity: return "SanityBottleOpen";
                default: return string.Empty; // ExitKey has no animation
            }
        }
    }

    private static IPickupReceiver FindReceiver(PlayerInteractor interactor)
    {
        if (interactor == null) return null;
        IPickupReceiver r = interactor.GetComponentInParent<IPickupReceiver>();
        if (r == null) r = interactor.GetComponentInChildren<IPickupReceiver>();
        return r;
    }

    private string Label
    {
        get
        {
            if (!string.IsNullOrEmpty(displayName)) return displayName;
            switch (type)
            {
                case PickupType.Health: return "Medkit";
                case PickupType.Ammo: return "Ammo";
                case PickupType.Sanity: return "Sanity pills";
                case PickupType.ExitKey: return "Exit key";
                default: return type.ToString();
            }
        }
    }

    // Called by Unity in the Editor when the component is first added (or Reset is chosen).
    // The prefabs have no colliders, so add one that fits the visible mesh.
    private void Reset()
    {
        if (GetComponentInChildren<Collider>() != null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            gameObject.AddComponent<BoxCollider>();
            return;
        }

        bool first = true;
        Bounds bounds = new Bounds();

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

                if (first) { bounds = new Bounds(p, Vector3.zero); first = false; }
                else bounds.Encapsulate(p);
            }
        }

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = bounds.size;
    }
}
