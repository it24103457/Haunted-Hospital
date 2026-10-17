using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/*
 * Runs the start menu: Play loads the game scene, How to Play swaps to the guide panel, Quit closes the game.
 * The cursor is freed and game time and audio run normally whenever the menu starts, since the player may arrive
 * here from a paused or finished game.
 * The game scene is picked as a scene asset in the Inspector; its path is stored alongside it so a built game can load it.
 * The How to Play button stays greyed out until a guide panel is assigned.
 */
public class MainMenu : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private SceneAsset gameScene;
#endif
    [SerializeField, HideInInspector] private string gameScenePath;

    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private Button howToPlayButton;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (howToPlayButton != null) howToPlayButton.interactable = howToPlayPanel != null;
        CloseHowToPlay();
    }

    public void Play()
    {
#if UNITY_EDITOR
        // In the Editor the path is read fresh from the scene asset, so moving or renaming the game scene never breaks Play.
        if (gameScene != null) gameScenePath = AssetDatabase.GetAssetPath(gameScene);
#endif
        SceneLoader.Load(gameScenePath);
    }

    public void OpenHowToPlay()
    {
        if (howToPlayPanel == null) return;

        if (mainPanel != null) mainPanel.SetActive(false);
        howToPlayPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    // Application.Quit does nothing in the Editor, so Play mode is stopped there instead.
    public void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

#if UNITY_EDITOR
    // Keeps the stored path in step with the chosen scene, including after the scene is moved or renamed.
    private void OnValidate()
    {
        gameScenePath = gameScene != null ? AssetDatabase.GetAssetPath(gameScene) : string.Empty;
    }
#endif
}
