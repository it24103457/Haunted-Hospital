// Assets/Editor/HospitalColliderTool.cs
// Adds fitted colliders to the hospital prop prefabs so the player (CharacterController),
// the PlayerInteractor ray, bullets, thrown objects and the NavMesh bake all "see" the props.
//
// CHANGES vs. the first version
//   * Only MeshRenderer / SkinnedMeshRenderer count toward the bounds (particles, lines, trails, sprites ignored).
//   * Rule.maxSize caps the collider per axis (0 = no cap). Height caps keep the bottom on the floor.
//   * Rule.largestOnly measures only the biggest mesh - for models exported as a whole "scene"
//     (e.g. PF_Bin = garbage_scene) where the combined bounds were far bigger than the can.
//   * "Add missing" no longer deletes an existing Auto_Colliders child before deciding to skip.
//   * Each result line now prints the final collider size so oversized ones are easy to spot.
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class HospitalColliderTool
{
    const string AutoChild = "Auto_Colliders";

    static readonly string[] SearchFolders =
    {
        "Assets/Prefabs/Hospital/Props",
        "Assets/Prefabs/Hospital/Gameplay",
        "Assets/Prefabs/Thief_stash",
    };

    enum Kind { Box, Capsule, Skip }

    struct Rule
    {
        public Kind kind;
        public float xzScale;     // shrink footprint a little so the player isn't snagged by mesh overhangs
        public Vector3 maxSize;   // per-axis cap in prefab-local metres, 0 = no cap (capsule: x/z cap = diameter)
        public bool largestOnly;  // measure only the largest mesh (for multi-object model files)

        public Rule(Kind k, float xz = 1f, Vector3 max = default, bool largest = false)
        {
            kind = k; xzScale = xz; maxSize = max; largestOnly = largest;
        }
    }

    static Vector3 Cap(float x, float y, float z) => new Vector3(x, y, z);

    // Anything in the folders above that is not listed here gets a plain Box rule.
    static readonly Dictionary<string, Rule> Rules = new Dictionary<string, Rule>
    {
        // Floor furniture: one box each
        { "PF_Bed",            new Rule(Kind.Box, 1f, Cap(1.10f, 0f, 2.20f)) },  // plan 1.05 x 1.05 x 2.2
        { "PF_HospitalBed_Alt",new Rule(Kind.Box) },
        { "PF_Desk",           new Rule(Kind.Box) },
        { "PF_Nightstand",     new Rule(Kind.Box) },
        { "PF_Shelf",          new Rule(Kind.Box) },
        { "PF_Locker",         new Rule(Kind.Box) },
        { "PF_HospitalTrolley",new Rule(Kind.Box) },
        { "PF_Stool",          new Rule(Kind.Box, 0.9f) },
        { "PF_WaitingChair",   new Rule(Kind.Box, 0.9f) },
        { "PF_Wheelchair",     new Rule(Kind.Box, 0.9f) },
        { "PF_Toilet",         new Rule(Kind.Box) },
        { "PF_Sink",           new Rule(Kind.Box) },
        { "PF_Bin",            new Rule(Kind.Box, 1f, Cap(0.60f, 0.85f, 0.60f), true) }, // plan 0.55 x 0.8 x 0.55
        { "PF_WaterCooler",    new Rule(Kind.Box) },
        { "PF_UtilityBox",     new Rule(Kind.Box) },
        { "PF_Crutch_IV",      new Rule(Kind.Capsule, 1f, Cap(0.30f, 0f, 0.30f)) },      // thin pole stand, max 0.30 m wide
        // Wall-mounted: thin box, mostly for bullets / thrown objects / interaction rays
        { "PF_WallClock",      new Rule(Kind.Box) },
        { "PF_WallPhone",      new Rule(Kind.Box) },
        { "PF_TackBoard",      new Rule(Kind.Box) },
        { "PF_SecurityCamera", new Rule(Kind.Box) },
        { "PF_Loudspeaker",    new Rule(Kind.Box) },
        { "PF_Keypad",         new Rule(Kind.Box) },
        // Thief stash
        { "PF_Bag",            new Rule(Kind.Box) },
        { "PF_Basket",         new Rule(Kind.Box) },
        // Hand-made: leave alone (animated / pickups already have their own)
        { "PF_DigitPaper",     new Rule(Kind.Skip) },
        { "PF_ExitKey",        new Rule(Kind.Skip) },
        { "PF_KeyBox",         new Rule(Kind.Skip) },
    };

    // Renderers whose name matches this are treated as MOVING parts on prefabs that have an Animator
    // (fridge door, cooler lid...). They get their own collider ON the moving object, and are
    // excluded from the fixed body collider so the interior stays usable.
    static readonly Regex MovingPartName = new Regex("door|lid|drawer|hinge|flap", RegexOptions.IgnoreCase);

    [MenuItem("Tools/Hospital/Colliders/Add missing colliders")]
    static void AddMissing() => Run(replace: false);

    [MenuItem("Tools/Hospital/Colliders/Rebuild auto colliders")]
    static void Rebuild() => Run(replace: true);

    [MenuItem("Tools/Hospital/Colliders/Report prefabs w\\o collider")]
    static void Report()
    {
        var sb = new StringBuilder("Prefabs with no Collider:\n");
        int n = 0;
        foreach (var path in FindPrefabs())
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            bool has = root.GetComponentsInChildren<Collider>(true).Length > 0;
            PrefabUtility.UnloadPrefabContents(root);
            if (!has) { sb.AppendLine("  " + path); n++; }
        }
        Debug.Log(n == 0 ? "[Colliders] Every prop prefab has at least one collider." : sb.ToString());
    }

    static void Run(bool replace)
    {
        int done = 0, skipped = 0;
        var log = new StringBuilder();
        try
        {
            var paths = FindPrefabs();
            for (int i = 0; i < paths.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Hospital colliders", paths[i], (float)i / paths.Count);
                string result = Process(paths[i], replace);
                if (result.StartsWith("OK")) done++; else skipped++;
                log.AppendLine($"{result}  {paths[i]}");
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Colliders] {done} prefab(s) updated, {skipped} skipped.\n{log}");
    }

    static List<string> FindPrefabs()
    {
        var list = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", SearchFolders))
            list.Add(AssetDatabase.GUIDToAssetPath(guid));
        return list;
    }

    static string Process(string path, bool replace)
    {
        string name = System.IO.Path.GetFileNameWithoutExtension(path);
        Rule rule = Rules.TryGetValue(name, out var r) ? r : new Rule(Kind.Box);
        if (rule.kind == Kind.Skip) return "SKIP (listed as skip)";

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            // "Add missing" must not touch a prefab that already has ANY collider (including Auto_Colliders).
            if (!replace && root.GetComponentsInChildren<Collider>(true).Length > 0)
                return "SKIP (already has collider)";

            var old = root.transform.Find(AutoChild);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            bool animated = root.GetComponentInChildren<Animator>(true) != null ||
                            root.GetComponentInChildren<Animation>(true) != null;

            Renderer only = rule.largestOnly ? LargestRenderer(root) : null;

            // 1. Fixed body bounds (moving parts excluded on animated prefabs)
            var moving = new List<Renderer>();
            if (!Measure(root, rend =>
                {
                    if (only != null && rend != only) return false;
                    if (animated && MovingPartName.IsMatch(rend.name)) { moving.Add(rend); return false; }
                    return true;
                }, out Bounds b))
                return "SKIP (no mesh renderers found)";

            Vector3 size = b.size;
            Vector3 center = b.center;
            size.x *= rule.xzScale; size.z *= rule.xzScale;

            bool clamped = ApplyCap(ref size, ref center, b, rule.maxSize);
            size = Vector3.Max(size, new Vector3(0.02f, 0.02f, 0.02f));

            var holder = new GameObject(AutoChild);
            holder.transform.SetParent(root.transform, false);

            if (rule.kind == Kind.Capsule)
            {
                var cap = holder.AddComponent<CapsuleCollider>();
                cap.direction = 1; // Y axis
                cap.center = center;
                cap.radius = Mathf.Min(size.x, size.z) * 0.5f;
                cap.height = Mathf.Max(size.y, cap.radius * 2f);
                size = new Vector3(cap.radius * 2f, cap.height, cap.radius * 2f);
            }
            else
            {
                var box = holder.AddComponent<BoxCollider>();
                box.center = center;
                box.size = size;
            }

            // 2. Moving parts: their own collider, on the moving object so it follows the animation
            foreach (var m in moving)
            {
                if (m.GetComponent<Collider>() != null) continue;
                var mb = m.gameObject.AddComponent<BoxCollider>();
                var lb = m.localBounds;
                mb.center = lb.center;
                mb.size = lb.size;
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            string dims = $" {rule.kind} {size.x:F2} x {size.y:F2} x {size.z:F2}";
            string extra = (clamped ? " [capped]" : "") + (only != null ? " [largest mesh only]" : "") +
                           (animated ? $" [animated: {moving.Count} moving part(s) found - CHECK by hand]" : "");
            return "OK" + dims + extra;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // Applies the per-axis cap. A height cap keeps the BOTTOM of the measured bounds so props still sit on the floor.
    static bool ApplyCap(ref Vector3 size, ref Vector3 center, Bounds measured, Vector3 max)
    {
        bool changed = false;
        if (max.x > 0f && size.x > max.x) { size.x = max.x; changed = true; }
        if (max.z > 0f && size.z > max.z) { size.z = max.z; changed = true; }
        if (max.y > 0f && size.y > max.y)
        {
            size.y = max.y;
            center.y = measured.min.y + size.y * 0.5f;
            changed = true;
        }
        return changed;
    }

    static bool IsMesh(Renderer r)
    {
        return r.enabled && (r is MeshRenderer || r is SkinnedMeshRenderer);
    }

    // Bounds of one renderer, expressed in the prefab root's local space.
    static Bounds RootSpaceBounds(GameObject root, Renderer r)
    {
        Bounds lb = r.localBounds;
        Bounds result = new Bounds();
        for (int i = 0; i < 8; i++)
        {
            Vector3 c = lb.center + Vector3.Scale(lb.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector3 p = root.transform.InverseTransformPoint(r.transform.TransformPoint(c));
            if (i == 0) result = new Bounds(p, Vector3.zero);
            else result.Encapsulate(p);
        }
        return result;
    }

    static Renderer LargestRenderer(GameObject root)
    {
        Renderer best = null;
        float bestVol = -1f;
        foreach (var r in root.GetComponentsInChildren<Renderer>(false))
        {
            if (!IsMesh(r)) continue;
            Vector3 s = RootSpaceBounds(root, r).size;
            float vol = s.x * s.y * s.z;
            if (vol > bestVol) { bestVol = vol; best = r; }
        }
        return best;
    }

    // Combined bounds of all enabled mesh renderers, expressed in the prefab root's local space.
    static bool Measure(GameObject root, System.Func<Renderer, bool> include, out Bounds result)
    {
        result = new Bounds();
        bool any = false;
        foreach (var r in root.GetComponentsInChildren<Renderer>(false))
        {
            if (!IsMesh(r)) continue;
            if (!include(r)) continue;

            Bounds rb = RootSpaceBounds(root, r);
            if (!any) { result = rb; any = true; }
            else result.Encapsulate(rb);
        }
        return any;
    }
}
#endif
