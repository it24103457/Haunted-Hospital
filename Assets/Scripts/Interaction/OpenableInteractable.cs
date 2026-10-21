using UnityEngine;

// Base class for "press E to open it" props: fridge, cooler box, key box.
// It is NOT a pickup (PickupInteractable removes the item). The prop stays in the world and only opens.
//
// How it fits together:
//  Player interaction: PlayerInteractor raycasts, finds this through GetComponentInParent on the collider it hit,
//                      shows GetPrompt() and calls Interact() when E is pressed.
//  Opening:            Interact() switches on one parameter on the prop's Animator. The parameter can be a Bool
//                      (set to true) or a Trigger (fired once). Nothing sets it back, so the prop stays open
//                      (the controllers have no transition back to closed).
//  After opening:      OnOpened() lets a subclass change colliders, so the item inside can be hit by the E ray.
//
// Put a subclass on the ROOT of the prefab. The door/lid colliders are children, so the ray finds this script
// through GetComponentInParent.
public abstract class OpenableInteractable : MonoBehaviour, IInteractable
{
    [Header("Open animation")]
    [Tooltip("Animator on this prefab. Found automatically in children if left empty.")]
    [SerializeField] protected Animator animator;

    [Tooltip("Name of the Bool or Trigger parameter on the Animator that opens the prop.")]
    [SerializeField] private string openParameter;

    [Header("Prompt")]
    [Tooltip("Name shown in the prompt, for example 'the fridge'.")]
    [SerializeField] private string displayName;

    public bool IsOpen { get; private set; }

    // Name the prompt uses when the field above is empty.
    protected abstract string DefaultOpenParameter { get; }
    protected abstract string DefaultDisplayName { get; }

    // Called once, right after the open parameter has been switched on.
    protected virtual void OnOpened() { }

    private string ParameterName => string.IsNullOrEmpty(openParameter) ? DefaultOpenParameter : openParameter;
    private string Label => string.IsNullOrEmpty(displayName) ? DefaultDisplayName : displayName;

    protected virtual void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        ForceClosed();
    }

    private void Start()
    {
        // Second safety net, in case the Animator initialised after Awake.
        ForceClosed();
    }

    // IInteractable

    public string GetPrompt()
    {
        return IsOpen ? string.Empty : $"Press E to open {Label}";
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (IsOpen) return;
        if (!HasParameter(out string param, out AnimatorControllerParameterType type))
        {
            Debug.LogWarning($"{GetType().Name} '{name}': Animator has no Bool or Trigger parameter '{ParameterName}'. " +
                             "Check the Animator and the parameter name.", this);
            return;
        }

        IsOpen = true;
        if (type == AnimatorControllerParameterType.Trigger) animator.SetTrigger(param);
        else animator.SetBool(param, true);
        OnOpened();
    }

    // The old controllers had the open parameter TRUE by default, which opened the prop when the scene started.
    // Force it to "closed" so it stays shut until the player presses E.
    private void ForceClosed()
    {
        if (IsOpen) return;
        if (!HasParameter(out string param, out AnimatorControllerParameterType type)) return;

        if (type == AnimatorControllerParameterType.Trigger) animator.ResetTrigger(param);
        else animator.SetBool(param, false);
    }

    private bool HasParameter(out string param, out AnimatorControllerParameterType type)
    {
        param = ParameterName;
        type = AnimatorControllerParameterType.Bool;
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(param))
            return false;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name != param) continue;
            if (p.type != AnimatorControllerParameterType.Bool &&
                p.type != AnimatorControllerParameterType.Trigger) continue;

            type = p.type;
            return true;
        }

        return false;
    }
}