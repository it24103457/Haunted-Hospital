
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    public sealed class EnvironmentDirectorSetup : MonoBehaviour
    {
        // Scene binding for the reusable Director prefab.
        public void BindScene(PlayerStats player, Transform director, Transform essential) { playerStats = player; directorLightsRoot = director; essentialLightsRoot = essential; }

        [Header("Required scene references")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private Transform directorLightsRoot;
        [SerializeField] private Transform essentialLightsRoot;

        // IT24103447 - Added DirectorSettings reference for event catalogue configuration.
        [Header("Director configuration")]
        [SerializeField] private DirectorSettings settings;

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

            // IT24103447 - Validate that DirectorSettings is assigned
            // and that its event definitions pass catalogue validation.
            if (settings == null)
            {
                Debug.LogError(
                    "[Environment Director] Director Settings is not assigned.",
                    this);

                valid = false;
            }
            else if (!settings.TryValidate(out var settingsErrors))
            {
                foreach (string error in settingsErrors)
                {
                    Debug.LogError(
                        $"[Environment Director] {error}",
                        this);
                }

                valid = false;
            }

            // Spatial support is installed in the Director test scene first.
            // Scenes without it retain the existing setup validation.
            var spatial = GetComponent<DirectorSpatialRegistry>();
            if (spatial != null && !spatial.TryValidate(out var spatialErrors))
            {
                foreach (string error in spatialErrors)
                    Debug.LogError($"[Environment Director] {error}", this);
                valid = false;
            }

            var observation = GetComponent<DirectorObservation>();
            if (observation != null && !observation.TryValidate(out var observationError))
            {
                Debug.LogError($"[Environment Director] {observationError}", this);
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

            // IT24103447 - Include event count and DirectorSettings
            // asset name in the successful setup validation message.
            Debug.Log(
                "[Environment Director] Setup ready. " +
                $"Player: {playerStats.gameObject.name}. " +
                $"Director lights: {directorCount}. " +
                $"Essential lights: {essentialCount}. " +
                $"Event definitions: {settings.Events.Count}. " +
                $"Settings: {settings.name}.",
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
