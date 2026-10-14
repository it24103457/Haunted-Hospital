using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    public sealed class EnvironmentDirectorSetup : MonoBehaviour
    {
        [Header("Required scene references")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private Transform directorLightsRoot;
        [SerializeField] private Transform essentialLightsRoot;

        private void Start()
        {
            ValidateSetup();
        }

        [ContextMenu("Validate Director Setup")]
        public void ValidateSetup()
        {
            bool valid = true;

            if (playerStats == null)
            {
                Debug.LogError(
                    "[Environment Director] PlayerStats is not assigned.",
                    this);

                valid = false;
            }

            if (directorLightsRoot == null)
            {
                Debug.LogError(
                    "[Environment Director] Director lighting group is not assigned.",
                    this);

                valid = false;
            }

            if (essentialLightsRoot == null)
            {
                Debug.LogError(
                    "[Environment Director] Essential lighting group is not assigned.",
                    this);

                valid = false;
            }

            if (directorLightsRoot != null &&
                essentialLightsRoot != null)
            {
                bool groupsOverlap =
                    directorLightsRoot == essentialLightsRoot ||
                    directorLightsRoot.IsChildOf(essentialLightsRoot) ||
                    essentialLightsRoot.IsChildOf(directorLightsRoot);

                if (groupsOverlap)
                {
                    Debug.LogError(
                        "[Environment Director] Director and Essential groups " +
                        "must be separate groups, not nested inside each other.",
                        this);

                    valid = false;
                }
            }

            int directorCount = CountLights(directorLightsRoot);
            int essentialCount = CountLights(essentialLightsRoot);

            if (directorLightsRoot != null && directorCount == 0)
            {
                Debug.LogError(
                    "[Environment Director] No Light components were found " +
                    "under the Director group.",
                    this);

                valid = false;
            }

            if (essentialLightsRoot != null && essentialCount == 0)
            {
                Debug.LogError(
                    "[Environment Director] No Light components were found " +
                    "under the Essential group.",
                    this);

                valid = false;
            }

            if (!valid)
            {
                Debug.LogWarning(
                    "[Environment Director] Setup is incomplete. " +
                    "Check the errors above.",
                    this);

                return;
            }

            Debug.Log(
                "[Environment Director] Setup ready. " +
                $"Player: {playerStats.gameObject.name}. " +
                $"Director lights: {directorCount}. " +
                $"Essential lights: {essentialCount}.",
                this);
        }

        private static int CountLights(Transform root)
        {
            return root == null
                ? 0
                : root.GetComponentsInChildren<Light>(true).Length;
        }
    }
}