using UnityEngine;
using UnityEngine.AI;


// Hinged door for prefabs whose pivot is in the MIDDLE of the door (not at the hinge).
// The moving part ("leaf") is a child that has a
// Collider and a kinematic Rigidbody. The leaf swings around a hinge point set by hingeOffset.
// Also switches the NavMeshObstacle off while open, so AI agents can path through.
public class Door : MonoBehaviour, IInteractable
{
    [Header("Parts")]
    [Tooltip("The moving part of the door. Must be a child of this object and have a Collider.")]
    [SerializeField] private Transform leaf;
    [Tooltip("Optional. Found automatically on this object if left empty.")]
    [SerializeField] private NavMeshObstacle obstacle;

    [Header("Hinge")]
    [Tooltip("Hinge position in the leaf's parent space. For a 1.2 m wide door use (-0.6, 0, 0) or (0.6, 0, 0).")]
    [SerializeField] private Vector3 hingeOffset = new Vector3(-0.6f, 0f, 0f);
    [Tooltip("Swing angle in degrees. Use a negative value to swing the other way.")]
    [SerializeField] private float openAngle = 90f;
    [Tooltip("Degrees per second.")]
    [SerializeField] private float openSpeed = 120f;
    [SerializeField] private bool isLocked;

    [Header("Noise")]
    [Tooltip("How loud it is when the PLAYER closes this door (a slam). 0 = silent.")]
    [SerializeField, Min(0f)] private float closeLoudness = NoiseLevels.DoorSlam;
    [Tooltip("How loud it is when the PLAYER closes this door while crouching. 0 = silent.")]
    [SerializeField, Min(0f)] private float crouchCloseLoudness = 3f;
    [Tooltip("The player counts as crouching when their CharacterController is shorter than this (standing is 1.8, crouching 0.9).")]
    [SerializeField, Min(0f)] private float crouchingBelowHeight = 1.35f;
    [Tooltip("How loud it is when the PLAYER opens this door. 0 = silent. Doors opened by agents make no noise.")]
    [SerializeField, Min(0f)] private float openLoudness = 0f;

    private Rigidbody leafBody;
    private Vector3 closedLocalPosition;
    private Quaternion closedLocalRotation;
    private float currentAngle;
    private bool isOpen;
    private bool passable; // true while the NavMeshObstacle is off (door opening, open or closing)

    private void Awake()
    {
        if (leaf == null)
        {
            Debug.LogError($"Door '{name}': assign the Leaf (the moving child object).", this);
            enabled = false;
            return;
        }

        leafBody = leaf.GetComponent<Rigidbody>();
        if (leafBody == null) leafBody = leaf.gameObject.AddComponent<Rigidbody>();
        leafBody.isKinematic = true;
        leafBody.interpolation = RigidbodyInterpolation.Interpolate;

        if (obstacle == null) obstacle = GetComponent<NavMeshObstacle>();

        closedLocalPosition = leaf.localPosition;
        closedLocalRotation = leaf.localRotation;

        SetObstacle(true); // closed door blocks the AI
    }

    public string GetPrompt()
    {
        if (isLocked) return "Locked";
        return isOpen ? "Press E to close" : "Press E to open";
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (isLocked) return;

        isOpen = !isOpen;
        if (isOpen) SetObstacle(false); // AI can path through as soon as it starts opening

        float loudness = isOpen ? openLoudness : (IsCrouching(interactor) ? crouchCloseLoudness : closeLoudness);
        if (loudness > 0f && leaf != null)
            Noise.Emit(leaf.position, loudness, NoiseType.DoorSlam, interactor != null ? interactor.gameObject : null);
    }

    // Reads the crouch from the player's CharacterController, so PlayerController does not need to change.
    private bool IsCrouching(PlayerInteractor interactor)
    {
        if (interactor == null) return false;

        CharacterController body = interactor.GetComponentInParent<CharacterController>();
        return body != null && body.height < crouchingBelowHeight;
    }

    // Call this from a key/lever/pickup later.
    public void Unlock() => isLocked = false;

    // Public API for the AI agents (Thief / Hunter).

    // True once the door is passable (from the moment it starts opening).
    public bool IsOpen => passable;

    // True only once the door has finished swinging, so the doorway is clear to walk through.
    public bool IsFullyOpen => isOpen && Mathf.Approximately(currentAngle, openAngle);

    // Who last asked for this door to open (an agent), or null. Handy for debugging two agents at one door.
    public GameObject LastRequester { get; private set; }

    // True while the door can't be opened.
    public bool IsLocked => isLocked;

    // Opens the door.
    public void Open() => Open(null);

    // Opens the door on behalf of an agent. Asking for a door that is already opening is harmless.
    public void Open(GameObject requester)
    {
        if (leaf == null || isLocked) return;

        if (requester != null) LastRequester = requester;
        if (isOpen) return;

        isOpen = true;
        SetObstacle(false);
    }

    private void FixedUpdate()
    {
        float target = isOpen ? openAngle : 0f;

        if (!Mathf.Approximately(currentAngle, target))
        {
            currentAngle = Mathf.MoveTowards(currentAngle, target, openSpeed * Time.fixedDeltaTime);
            ApplySwing();
        }
        else if (!isOpen)
        {
            SetObstacle(true); // fully closed again -> block the AI
        }
    }

    // Rotates the leaf around the hinge point instead of around its own pivot.
    private void ApplySwing()
    {
        Quaternion swing = Quaternion.Euler(0f, currentAngle, 0f);
        Vector3 localPos = hingeOffset + swing * (closedLocalPosition - hingeOffset);
        Quaternion localRot = swing * closedLocalRotation;

        Transform parent = leaf.parent;
        Vector3 worldPos = parent != null ? parent.TransformPoint(localPos) : localPos;
        Quaternion worldRot = parent != null ? parent.rotation * localRot : localRot;

        leafBody.MovePosition(worldPos);
        leafBody.MoveRotation(worldRot);
    }

    private void SetObstacle(bool blocked)
    {
        passable = !blocked;

        if (obstacle != null && obstacle.enabled != blocked)
            obstacle.enabled = blocked;
    }
}