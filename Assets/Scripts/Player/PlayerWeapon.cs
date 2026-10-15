using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Instant-hit weapon (the syringe gun). Left mouse fires, R reloads.
/// The weapon keeps its own magazine; reloading takes rounds from PlayerStats' reserve ammo.
/// Anything with an IDamageable (the Thief, barricade planks, ...) takes a DamageInfo hit.
/// Put this on the Player (next to PlayerStats).
/// </summary>
[RequireComponent(typeof(PlayerStats))]
public class PlayerWeapon : MonoBehaviour
{
    [Header("References (found automatically if left empty)")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerStats stats;

    [Header("Weapon")]
    [SerializeField, Min(1)] private int magazineSize = 6;
    [SerializeField, Min(0)] private int startingMagazine = 6;
    [SerializeField, Min(0f)] private float damage = 50f;
    [SerializeField, Min(1f)] private float range = 40f;
    [Tooltip("Minimum seconds between shots.")]
    [SerializeField, Min(0f)] private float fireInterval = 0.5f;
    [SerializeField, Min(0f)] private float reloadTime = 1.5f;
    [Tooltip("Layers the shot can hit. Leave on Everything unless you add a Player layer.")]
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool showDebugReadout = true;
    [SerializeField] private bool logHits = false;

    public int Magazine { get; private set; }
    public int MagazineSize => magazineSize;
    public bool IsReloading { get; private set; }

    // (current magazine, magazine size)
    public event Action<int, int> MagazineChanged;

    // Raised for every shot fired, with the world position it was fired from (for noise, effects).
    public event Action<Vector3> Fired;

    public event Action ReloadStarted;
    public event Action ReloadFinished;

    private InputAction fireAction;
    private InputAction reloadAction;

    private float nextFireTime;
    private float reloadEndTime;

    private void Awake()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();

        Magazine = Mathf.Clamp(startingMagazine, 0, magazineSize);

        fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
        reloadAction = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");
    }

    private void OnEnable()
    {
        fireAction.Enable();
        reloadAction.Enable();
    }

    private void OnDisable()
    {
        fireAction.Disable();
        reloadAction.Disable();
    }

    private void OnDestroy()
    {
        fireAction.Dispose();
        reloadAction.Dispose();
    }

    private void Start()
    {
        // Let anything already subscribed (a HUD) show the starting value.
        MagazineChanged?.Invoke(Magazine, magazineSize);
    }

    private void Update()
    {
        if (stats.IsDead) return;

        if (IsReloading)
        {
            if (Time.time >= reloadEndTime) FinishReload();
            return;
        }

        // Don't fire on the click that grabs the mouse back after pressing Esc.
        if (Cursor.lockState != CursorLockMode.Locked) return;

        if (reloadAction.WasPressedThisFrame())
        {
            StartReload();
            return;
        }

        if (fireAction.WasPressedThisFrame())
            TryFire();
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime) return;

        if (Magazine <= 0)
        {
            StartReload(); // empty: reload instead of firing
            return;
        }

        Magazine--;
        nextFireTime = Time.time + fireInterval;
        MagazineChanged?.Invoke(Magazine, magazineSize);

        Transform cam = playerCamera.transform;
        Fired?.Invoke(cam.position);

        Shoot(cam);
    }

    private void Shoot(Transform cam)
    {
        if (!Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range,
                             hitMask, QueryTriggerInteraction.Ignore))
        {
            if (logHits) Debug.Log("[PlayerWeapon] Shot hit nothing.", this);
            return;
        }

        // GetComponentInParent so a child collider (a plank, a body part) finds the script on its parent.
        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();

        if (logHits)
        {
            Debug.Log($"[PlayerWeapon] Shot hit '{hit.collider.name}'" +
                      (target != null ? " (damageable)" : " (not damageable)"), this);
        }

        if (target == null) return;

        target.TakeDamage(new DamageInfo(damage, gameObject, hit.point));
    }

    private void StartReload()
    {
        if (IsReloading || Magazine >= magazineSize || stats.ReserveAmmo <= 0) return;

        IsReloading = true;
        reloadEndTime = Time.time + reloadTime;
        ReloadStarted?.Invoke();
    }

    private void FinishReload()
    {
        IsReloading = false;

        int wanted = magazineSize - Magazine;
        int take = Mathf.Min(wanted, stats.ReserveAmmo);

        if (take > 0 && stats.TryUseAmmo(take))
        {
            Magazine += take;
            MagazineChanged?.Invoke(Magazine, magazineSize);
        }

        ReloadFinished?.Invoke();
    }

    private void OnGUI()
    {
        if (!showDebugReadout) return;

        string text = IsReloading
            ? "Reloading..."
            : $"Magazine {Magazine}/{magazineSize}   Reserve {stats.ReserveAmmo}";

        GUI.Label(new Rect(12f, Screen.height - 36f, 500f, 24f), text);
    }
}
