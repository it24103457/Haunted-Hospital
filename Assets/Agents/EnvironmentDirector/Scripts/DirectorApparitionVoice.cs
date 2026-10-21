using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    /// <summary>Presentation only: plays a spatial voice with the apparition's existing animation.</summary>
    [DisallowMultipleComponent]
    public sealed class DirectorApparitionVoice : MonoBehaviour
    {
        [SerializeField] private AudioSource voice;
        [SerializeField] private Animator apparitionAnimator;
        [Tooltip("Enable on a test-scene instance to audition once on entering Play mode. Keep disabled on the prefab for Director-controlled timing.")]
        [SerializeField] private bool playOnEnable;

        public AudioClip Clip => voice == null ? null : voice.clip;

        public bool IsPlaying => voice != null && voice.isPlaying;

        private void OnEnable()
        {
            if (playOnEnable) PlayVoice();
        }

        /// <summary>Call after activating/placing the apparition. This does not emit gameplay noise events.</summary>
        public void PlayVoice() => PlayVoice(true);

        public void PlayVoice(bool triggerJolt)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || voice == null || voice.clip == null)
                return;

            voice.Stop();
            if (triggerJolt && apparitionAnimator != null && apparitionAnimator.isActiveAndEnabled)
            {
                apparitionAnimator.SetBool("PulsePose", false);
                apparitionAnimator.ResetTrigger("Jolt");
                apparitionAnimator.SetTrigger("Jolt");
            }
            voice.ignoreListenerPause = false;
            voice.Play();
        }

        public void StopVoice()
        {
            if (voice != null) voice.Stop();
        }

        private void OnDisable() => StopVoice();

        [ContextMenu("Play Voice + Jolt (Play Mode)")]
        private void PreviewVoice() => PlayVoice();

        [ContextMenu("Stop Voice (Play Mode)")]
        private void PreviewStop() => StopVoice();
    }
}
