using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/*
 * Loads a scene by its asset path for the menus and end screens.
 * A built game can only load scenes listed in Build Settings. While testing in the Editor, a scene that is not
 * listed yet is loaded through the Editor's play-mode loader instead, so the screens work before Build Settings is updated.
 * Game time and audio are always set back to normal first, so a scene left from a paused game never starts frozen or silent.
 */
public static class SceneLoader
{
    // The level the player was last playing, remembered before a cutscene scene takes over, so Play Again can return to it.
    public static string LastLevelPath { get; set; }

    public static void Load(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogWarning("SceneLoader: no scene was set to load.");
            return;
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (SceneUtility.GetBuildIndexByScenePath(scenePath) >= 0)
        {
            SceneManager.LoadScene(scenePath);
            return;
        }

#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogError("SceneLoader: " + scenePath + " is not in Build Settings, so it cannot be loaded.");
#endif
    }

    /*
     * Loads a scene in the background, so the current scene keeps running (for example a loading animation) until it is ready.
     * Follows the same rules as Load. Returns null if the scene cannot be loaded.
     */
    public static AsyncOperation LoadAsync(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogWarning("SceneLoader: no scene was set to load.");
            return null;
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (SceneUtility.GetBuildIndexByScenePath(scenePath) >= 0) return SceneManager.LoadSceneAsync(scenePath);

#if UNITY_EDITOR
        return EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogError("SceneLoader: " + scenePath + " is not in Build Settings, so it cannot be loaded.");
        return null;
#endif
    }
}
