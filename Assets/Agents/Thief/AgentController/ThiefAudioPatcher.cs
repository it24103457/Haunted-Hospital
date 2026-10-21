#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class ThiefAudioPatcher
{
    [MenuItem("Haunted Hospital/Patch Thief Audio")]
    public static void Run()
    {
        const string prefabPath = "Assets/Agents/Thief/Prefabs/PF_Thief.prefab";
        var go = PrefabUtility.LoadPrefabContents(prefabPath);
        if (go == null)
        {
            Debug.LogError("[ThiefAudioPatcher] PF_Thief.prefab not found.");
            return;
        }

        var audioDriver = go.GetComponent<ThiefAudioDriver>();
        if (audioDriver == null)
        {
            audioDriver = go.AddComponent<ThiefAudioDriver>();
            Debug.Log("[ThiefAudioPatcher] Added ThiefAudioDriver component to PF_Thief.");
        }

        var runClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Models/Agents/Thief/Audio/thief running.mp3");
        if (runClip == null)
        {
            Debug.LogError("[ThiefAudioPatcher] thief running.mp3 not found at Assets/Art/Models/Agents/Thief/Audio/thief running.mp3");
        }
        else
        {
            var so = new SerializedObject(audioDriver);
            var runClipProp = so.FindProperty("runClip");
            if (runClipProp != null)
            {
                runClipProp.objectReferenceValue = runClip;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[ThiefAudioPatcher] Assigned thief running.mp3 to ThiefAudioDriver.");
            }
        }

        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        PrefabUtility.UnloadPrefabContents(go);
        Debug.Log("[ThiefAudioPatcher] PF_Thief.prefab patched successfully.");
    }
}
#endif
