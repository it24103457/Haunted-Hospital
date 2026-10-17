using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/*
 * Plays a click sound for every button under this object, and an optional hover sound when the pointer enters one.
 * Put one on a menu's Canvas; buttons on hidden panels are included too.
 * Each sound plays from its own short-lived object that survives scene loads and ignores the game's audio pause,
 * so a click is still heard when the button pauses the game or switches to another scene.
 */
public class UIButtonSounds : MonoBehaviour
{
    [SerializeField] private AudioClip click;
    [SerializeField] private AudioClip hover;
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
    [Tooltip("Buttons that play their own sound when clicked (e.g. page turns), so they only get the hover sound.")]
    [SerializeField] private Button[] noClickSound;

    private void Awake()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
        {
            if (noClickSound == null || System.Array.IndexOf(noClickSound, button) < 0) button.onClick.AddListener(() => Play(click));
            if (hover != null) button.gameObject.AddComponent<HoverRelay>().Init(this, button);
        }
    }

    private void Play(AudioClip clip)
    {
        if (clip == null) return;

        var go = new GameObject("UISound");
        DontDestroyOnLoad(go);
        var source = go.AddComponent<AudioSource>();
        source.ignoreListenerPause = true;
        source.spatialBlend = 0f;
        source.PlayOneShot(clip, volume);
        Destroy(go, clip.length + 0.1f);
    }

    // Forwards the pointer entering a button to the hover sound, only while that button can be clicked.
    private class HoverRelay : MonoBehaviour, IPointerEnterHandler
    {
        private UIButtonSounds owner;
        private Button button;

        public void Init(UIButtonSounds soundOwner, Button target)
        {
            owner = soundOwner;
            button = target;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button.interactable) owner.Play(owner.hover);
        }
    }
}
