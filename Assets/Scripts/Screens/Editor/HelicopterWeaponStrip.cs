using UnityEditor;
using UnityEngine;

/*
 * Removes the weapons (minigun, rocket pods and their mounts) from the escape cutscene's helicopter when it is imported,
 * so it reads as an air ambulance. Only that one model is touched.
 * The parts that are only weapons (miniguns, their mounts and belts, the rocket pods' fittings) have their own material slots and are
 * emptied completely. The rocket pod tubes share a slot with the body, so in every slot each triangle whose centre sits outside
 * the body's width at the height of the side mounts is dropped too; the skids and their struts stay because they sit inside that width.
 * The model's material slots are kept (just empty), so its materials still line up.
 */
public class HelicopterWeaponStrip : AssetPostprocessor
{
    private const string ModelPath = "Assets/Art/Models/Cutscene/Roof/Helicopter/Helicopter.fbx";
    private static readonly int[] WeaponSlots = { 4, 5, 6, 7, 22, 26, 27, 28, 29 };

    // In the model's default pose: the body's centre line, how far either side of it the weapons start, and the height band they sit in.
    private const float CentreX = 0.11f;
    private const float WeaponsBeyond = 0.97f;
    private const float BandLow = 0.35f;
    private const float BandHigh = 0.95f;

    // Raised whenever what this removes changes, so Unity re-imports the model.
    public override uint GetVersion() => 2;

    // When the helicopter model imports, removes the triangles of its weapon parts so only the ambulance body remains.
    private void OnPostprocessModel(GameObject root)
    {
        if (assetPath != ModelPath) return;
        var filter = root.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return;

        Mesh mesh = filter.sharedMesh;
        Matrix4x4 toPose = root.transform.localToWorldMatrix;
        Vector3[] v = mesh.vertices;
        int removed = 0;
        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            int[] t = mesh.GetTriangles(s);
            if (System.Array.IndexOf(WeaponSlots, s) >= 0)
            {
                removed += t.Length / 3;
                mesh.SetTriangles(new int[0], s);
                continue;
            }
            var keep = new System.Collections.Generic.List<int>(t.Length);
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 c = toPose.MultiplyPoint3x4((v[t[i]] + v[t[i + 1]] + v[t[i + 2]]) / 3f);
                bool weapon = Mathf.Abs(c.x - CentreX) > WeaponsBeyond && c.y > BandLow && c.y < BandHigh;
                if (weapon) { removed++; continue; }
                keep.Add(t[i]); keep.Add(t[i + 1]); keep.Add(t[i + 2]);
            }
            mesh.SetTriangles(keep, s);
        }
        Debug.Log("HelicopterWeaponStrip: removed " + removed + " weapon triangles from " + ModelPath);
    }
}
