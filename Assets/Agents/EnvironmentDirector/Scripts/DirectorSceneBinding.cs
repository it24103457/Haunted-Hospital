using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    /// <summary>Reconnects the portable Director to objects in its own scene before other components initialize.</summary>
    [DefaultExecutionOrder(-10000), DisallowMultipleComponent]
    public sealed class DirectorSceneBinding : MonoBehaviour
    {
        [Serializable]
        public sealed class AnchorBinding
        {
            public DirectorEventAnchor anchor;
            public string sourcePath;
            public string[] lightPaths = Array.Empty<string>();
        }

        [Tooltip("Optional overrides. Otherwise a unique same-scene player/objective is discovered.")]
        [SerializeField] private PlayerStats player;
        [SerializeField] private ObjectiveState objectives;
        [SerializeField] private Transform directorLights;
        [SerializeField] private Transform essentialLights;
        [SerializeField] private string directorPath;
        [SerializeField] private string essentialPath;
        [SerializeField] private AnchorBinding[] anchors = Array.Empty<AnchorBinding>();
        [SerializeField, TextArea] private string status;
        public string Status => status;

        private void Awake()
        {
            // A copied hospital scene already contains a Director. Keep its authored instance.
            var others = SceneComponents<EnvironmentDirectorSetup>().Where(c => c.gameObject != gameObject && c.isActiveAndEnabled).ToArray();
            if (others.Length > 0)
            {
                status = "Another active Environment Director exists in this scene. Remove the duplicate.";
                Debug.LogError("[Environment Director] " + status, this);
                gameObject.SetActive(false);
                return;
            }
            if (!BindScene())
            {
                Debug.LogError("[Environment Director] " + status, this);
                // Never run a partially bound prefab or silently use targets from another scene.
                gameObject.SetActive(false);
            }
        }

        [ContextMenu("Bind Scene References")]
        private void BindFromInspector()
        {
            if (BindScene()) Debug.Log("[Environment Director] " + status, this);
            else Debug.LogError("[Environment Director] " + status, this);
        }

        public bool BindScene()
        {
            var errors = new List<string>();
            var transforms = SceneComponents<Transform>().Where(t => !t.IsChildOf(transform)).ToArray();
            if (player == null) player = Unique(SceneComponents<PlayerStats>().Where(p => p.isActiveAndEnabled));
            if (objectives == null) objectives = Unique(SceneComponents<ObjectiveState>().Where(o => o.isActiveAndEnabled));
            if (directorLights == null) directorLights = Resolve(directorPath, transforms);
            if (essentialLights == null) essentialLights = Resolve(essentialPath, transforms);
            if (player == null || player.gameObject.scene != gameObject.scene) errors.Add("Assign one same-scene PlayerStats.");
            if (objectives == null || objectives.gameObject.scene != gameObject.scene) errors.Add("Assign one same-scene ObjectiveState.");
            if (directorLights == null || directorLights.gameObject.scene != gameObject.scene) errors.Add("Director lighting group missing or ambiguous: " + directorPath);
            if (essentialLights == null || essentialLights.gameObject.scene != gameObject.scene) errors.Add("Essential lighting group missing or ambiguous: " + essentialPath);
            var camera = player == null ? null : Unique(player.GetComponentsInChildren<Camera>(true).Where(c => c.isActiveAndEnabled));
            var weapon = player == null ? null : Unique(player.GetComponentsInChildren<PlayerWeapon>(true).Where(w => w.isActiveAndEnabled));
            if (camera == null) errors.Add("Player needs one active camera.");
            if (weapon == null) errors.Add("Player needs one active PlayerWeapon.");
            foreach (var entry in anchors)
            {
                if (entry == null || entry.anchor == null) { errors.Add("Missing anchor binding."); continue; }
                var source = string.IsNullOrEmpty(entry.sourcePath) ? null : Resolve(entry.sourcePath, transforms);
                if (!string.IsNullOrEmpty(entry.sourcePath) && source == null) errors.Add("Missing or ambiguous target: " + entry.sourcePath);
                var lights = new List<Light>();
                foreach (string path in entry.lightPaths ?? Array.Empty<string>())
                {
                    var target = Resolve(path, transforms);
                    var light = target == null ? null : target.GetComponent<Light>();
                    if (light == null) errors.Add("Missing or ambiguous light: " + path);
                    else lights.Add(light);
                }
                entry.anchor.BindTargets(source, lights.ToArray());
            }
            if (errors.Count > 0) { status = string.Join("\n", errors); return false; }
            GetComponent<EnvironmentDirectorSetup>().BindScene(player, directorLights, essentialLights);
            GetComponent<DirectorRoomTracker>().BindPlayer(player.transform);
            GetComponent<DirectorObservation>().BindPlayer(player);
            GetComponent<DirectorEventMemory>().BindPlayer(player);
            GetComponent<DirectorEventEligibility>().BindScene(directorLights, essentialLights, camera);
            GetComponent<DirectorPlayerSignals>().BindScene(weapon, objectives);
            GetComponent<DirectorFlickerExecutor>().BindLighting(directorLights, essentialLights);
            GetComponent<DirectorBlackoutExecutor>().BindLighting(directorLights, essentialLights);
            GetComponent<DirectorApparitionExecutor>().BindLighting(directorLights, essentialLights);
            // The live adapter also discovers a Hunter spawned later. Missing Hunter stays unavailable.
            GetComponent<DirectorLiveHunterSource>().BindHunter(Unique(SceneComponents<MonoBehaviour>().Where(c => c is IHunterStatus)));
            var registry = GetComponent<DirectorSpatialRegistry>();
            registry.BindLighting(directorLights, essentialLights);
            if (!registry.TryValidate(out errors)) { status = string.Join("\n", errors); return false; }
            status = "Bound to " + gameObject.scene.name + ": " + registry.Rooms.Count + " rooms, " + registry.Anchors.Count + " anchors.";
            return true;
        }

        private IEnumerable<T> SceneComponents<T>() where T : Component =>
            gameObject.scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));

        private static T Unique<T>(IEnumerable<T> values) where T : UnityEngine.Object
        {
            var candidates = values.Where(v => v != null).Take(2).ToArray();
            return candidates.Length == 1 ? candidates[0] : null;
        }

        public static string ScenePath(Transform target)
        {
            if (target == null) return "";
            string path = target.name;
            while (target.parent != null) { target = target.parent; path = target.name + "/" + path; }
            return path;
        }

        private static Transform Resolve(string path, Transform[] sceneTransforms)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var exact = sceneTransforms.Where(t => ScenePath(t) == path).Take(2).ToArray();
            if (exact.Length > 0) return exact.Length == 1 ? exact[0] : null;
            // Supports a renamed/reparented hospital root only when the target name is unique.
            string name = path.Substring(path.LastIndexOf('/') + 1);
            return Unique(sceneTransforms.Where(t => t.name == name));
        }
    }
}
