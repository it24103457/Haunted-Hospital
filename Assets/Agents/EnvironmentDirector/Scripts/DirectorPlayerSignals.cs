using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [DisallowMultipleComponent]
    public sealed class DirectorPlayerSignals : MonoBehaviour
    {
        [SerializeField] private PlayerWeapon weapon;
        [SerializeField] private ObjectiveState objectives;
        [SerializeField] private DirectorSettings settings;
        private PlayerWeapon boundWeapon;
        private float lastShot, boundAt;
        private bool shotObserved;
        private void OnEnable() => Bind();
        private void OnDisable() { if (boundWeapon != null) boundWeapon.Fired -= OnShot; boundWeapon = null; }
        private void Update() { if (boundWeapon != weapon) Bind(); }
        private void Bind()
        {
            if (boundWeapon != null) boundWeapon.Fired -= OnShot;
            boundWeapon = weapon; boundAt = Time.time; shotObserved = false;
            if (boundWeapon != null) boundWeapon.Fired += OnShot;
        }
        private void OnShot(Vector3 position) { shotObserved = true; lastShot = Time.time; }
        public void Read(Transform player, out DirectorSignalState gunfire,
            out DirectorSignalState keypad, out DirectorSignalState ending)
        {
            gunfire = keypad = ending = DirectorSignalState.Unavailable;
            if (!isActiveAndEnabled || player == null) return;
            if (boundWeapon == weapon && weapon != null && weapon.isActiveAndEnabled && settings != null &&
                weapon.gameObject.scene == gameObject.scene &&
                (weapon.transform == player || weapon.transform.IsChildOf(player)))
            {
                float recovery = settings.GunfireRecoverySeconds;
                if (!float.IsNaN(recovery) && !float.IsInfinity(recovery) && recovery >= 0)
                    gunfire = Time.time - (shotObserved ? lastShot : boundAt) < recovery
                        ? DirectorSignalState.Active : DirectorSignalState.Inactive;
            }
            if (objectives != null && objectives.isActiveAndEnabled && objectives.IsReady &&
                objectives.gameObject.scene == gameObject.scene)
            {
                keypad = objectives.IsKeypadOpen ? DirectorSignalState.Active : DirectorSignalState.Inactive;
                ending = objectives.HasWon || objectives.IsEscapeInProgress
                    ? DirectorSignalState.Active : DirectorSignalState.Inactive;
            }
        }
    }
}
