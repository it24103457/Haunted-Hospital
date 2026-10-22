using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/*
 * Adds the in-game screens (death, pause, win) to every scene that has a player, without anything being placed in the scene.
 * When a scene finishes loading, it looks for a PlayerStats; if there is one and the screens are not already there,
 * it creates them from the PF_GameUI prefab in Resources/Screens, plus an EventSystem if the scene has none,
 * since the screens' buttons need one. Scenes without a player, such as the main menu, are left alone.
 * Someone can still drag PF_GameUI into a scene by hand; a scene that already has the screens never gets a second copy.
 */
public static class GameUIBootstrap
{
    private const string PrefabPath = "Screens/PF_GameUI";

    // Clears the scene-loaded hook when play starts, so it is never added twice when the Editor skips reloading scripts.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Runs once the first scene has loaded; later scenes are handled through the scene-loaded hook.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        AddScreens();
    }

    // Every newly loaded scene gets the screens if it needs them.
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AddScreens();
    }

    // Adds the screens to the current scene when it has a player and doesn't already have them, plus an EventSystem if missing.
    private static void AddScreens()
    {
        if (Object.FindAnyObjectByType<PlayerStats>() == null) return;
        if (Object.FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include) != null) return;

        GameObject prefab = Resources.Load<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("GameUIBootstrap: no prefab at Resources/" + PrefabPath + ".");
            return;
        }

        Object.Instantiate(prefab).name = prefab.name;

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        Debug.Log("Game UI (death, pause, win screens) added by GameUIBootstrap.");
    }
}
