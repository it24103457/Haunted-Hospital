using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/*
 * The scene changes shared by the in-game screens (death, pause, win): restart the current level, or go back to the main menu.
 * The main menu scene is picked as a scene asset in the Inspector. In the Editor its current path is always read from that asset,
 * so moving or renaming the menu scene never breaks the button; the stored copy of the path is what a built game uses.
 */
public class ScreenNavigation : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private SceneAsset mainMenuScene;
#endif
    [SerializeField, HideInInspector] private string mainMenuScenePath;

    private string MainMenuPath
    {
        get
        {
#if UNITY_EDITOR
            if (mainMenuScene != null) return AssetDatabase.GetAssetPath(mainMenuScene);
#endif
            return mainMenuScenePath;
        }
    }

    public void Restart()
    {
        SceneLoader.Load(SceneManager.GetActiveScene().path);
    }

    public void ToMainMenu()
    {
        SceneLoader.Load(MainMenuPath);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        mainMenuScenePath = mainMenuScene != null ? AssetDatabase.GetAssetPath(mainMenuScene) : string.Empty;
    }
#endif
}
