using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    /// <summary>Presentation only: plays a spatial voice with the apparition's existing animation.</summary>
    [DisallowMultipleComponent]
    public sealed class DirectorApparitionVoice : MonoBehaviour
    {
        [SerializeField] private AudioSource voice;
        [SerializeField] private Animator apparitionAnimator;
        [SerializeField] private AudioSource effects;
        [SerializeField] private AudioClip arrivalClip, disappearanceClip;
        [SerializeField] private Transform pointingArm, pointingForearm, pointingHand, pointingFinger;
        [Tooltip("Enable on a test-scene instance to audition once on entering Play mode. Keep disabled on the prefab for Director-controlled timing.")]
        [SerializeField] private bool playOnEnable;
        private float normalVolume = -1f;
        private Vector3 pointTarget;
        private float pointWeight, requestedPointWeight;

        public float ArrivalDuration => arrivalClip == null ? 0f : arrivalClip.length;

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
            if (normalVolume < 0f) normalVolume = voice.volume;
            voice.volume = normalVolume;
            if (triggerJolt && apparitionAnimator != null && apparitionAnimator.isActiveAndEnabled)
            {
                apparitionAnimator.SetBool("PulsePose", false);
                apparitionAnimator.ResetTrigger("Jolt");
                apparitionAnimator.SetTrigger("Jolt");
            }
            voice.ignoreListenerPause = false;
            // One monster performance per appearance. Never restart it to fill the event.
            voice.loop = false;
            voice.Play();
        }

        public void PlayArrival()
        {
            if (effects != null && arrivalClip != null)
            {
                effects.ignoreListenerPause = false;
                effects.PlayOneShot(arrivalClip);
            }
        }

        public void PlayDisappearance()
        {
            if (disappearanceClip == null || voice == null) return;
            // This tail must outlive the model, which the executor removes immediately.
            var tail = new GameObject("Apparition_Disappearance_Audio");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(tail, gameObject.scene);
            tail.transform.position = voice.transform.position;
            var source = tail.AddComponent<AudioSource>();
            source.clip = disappearanceClip;
            source.outputAudioMixerGroup = voice.outputAudioMixerGroup;
            source.volume = normalVolume >= 0f ? normalVolume : voice.volume;
            source.spatialBlend = voice.spatialBlend;
            source.rolloffMode = voice.rolloffMode;
            source.minDistance = voice.minDistance;
            source.maxDistance = voice.maxDistance;
            source.dopplerLevel = 0f;
            source.playOnAwake = false;
            source.loop = false;
            source.ignoreListenerPause = false;
            source.Play();
            Destroy(tail, disappearanceClip.length + .2f);
        }

        public void PointAt(Vector3 position, float weight)
        {
            pointTarget = position;
            requestedPointWeight = Mathf.Clamp01(weight);
        }

        private void LateUpdate()
        {
            if (Time.timeScale <= 0f || AudioListener.pause ||
                pointingArm == null || pointingForearm == null || pointingHand == null) return;
            pointWeight = Mathf.MoveTowards(pointWeight, requestedPointWeight, Time.deltaTime * 4f);
            if (pointWeight <= 0f) return;
            // Aim after the authored pose, so the extended arm points at the actual player.
            AimBone(pointingArm, pointingForearm.position - pointingArm.position, pointWeight);
            AimBone(pointingForearm, pointingHand.position - pointingForearm.position, pointWeight);
            if (pointingFinger != null)
                AimBone(pointingHand, pointingFinger.position - pointingHand.position, pointWeight);
        }

        private void AimBone(Transform bone, Vector3 direction, float weight)
        {
            Vector3 target = pointTarget - bone.position;
            if (target.sqrMagnitude < .001f || direction.sqrMagnitude < .000001f) return;
            bone.rotation = Quaternion.Slerp(bone.rotation,
                Quaternion.FromToRotation(direction, target) * bone.rotation, weight);
        }

        public void StopVoice()
        {
            if (voice != null)
            {
                voice.Stop();
                if (normalVolume >= 0f) voice.volume = normalVolume;
            }
        }

        private void OnDisable()
        {
            StopVoice();
            pointWeight = requestedPointWeight = 0f;
            if (effects != null) effects.Stop();
        }

        [ContextMenu("Play Voice + Jolt (Play Mode)")]
        private void PreviewVoice() => PlayVoice();

        [ContextMenu("Stop Voice (Play Mode)")]
        private void PreviewStop() => StopVoice();
    }
}
