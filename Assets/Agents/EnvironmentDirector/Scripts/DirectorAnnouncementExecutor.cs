using System;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    [DisallowMultipleComponent]
    public sealed class DirectorAnnouncementExecutor : MonoBehaviour
    {
        [SerializeField] private DirectorEventEligibility eligibility;
        [SerializeField] private DirectorTargetReservations reservations;
        [SerializeField] private DirectorEventMemory memory;
        [SerializeField] private DirectorObservation observation;
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private AudioClip[] voiceClips = new AudioClip[4];
        [SerializeField, Range(0f, 1f)] private float voiceVolume = .7f;
        [Header("Runtime announcement sequence")]
        [SerializeField] private string status = "Idle";
        [SerializeField] private bool running;
        [SerializeField] private string currentVoice;
        [SerializeField] private float currentDuration;
        [SerializeField] private int completedInCycle;
        [SerializeField] private int completedCycles;
        private const float ScheduleLead = .15f;
        private int[] order;
        private int lastCompletedIndex = -1;
        private DirectorPlanningAction action;
        private string token;
        private bool ownsMemory, delivered;
        private double voiceStarts, endsAt;

        private void Awake()
        {
            completedInCycle = completedCycles = 0;
            ShuffleCycle();
        }

        private bool ClipsValid()
        {
            if (voiceClips == null || voiceClips.Length != 4) return false;
            for (int i = 0; i < voiceClips.Length; i++)
            {
                if (voiceClips[i] == null || voiceClips[i].length <= 0) return false;
                for (int j = 0; j < i; j++) if (voiceClips[i] == voiceClips[j]) return false;
            }
            return true;
        }

        private void ShuffleCycle()
        {
            order = new[] { 0, 1, 2, 3 };
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int value = order[i]; order[i] = order[j]; order[j] = value;
            }
            // All four still play once, without an immediate repeat across cycle boundaries.
            if (order[0] == lastCompletedIndex)
            { int value = order[0]; order[0] = order[1]; order[1] = value; }
        }

        public float NextDuration => ClipsValid() && order != null
            ? ScheduleLead + voiceClips[order[completedInCycle]].length : 0f;

        public bool Supports(DirectorEventDefinition definition) => isActiveAndEnabled &&
            definition != null && definition.EventType == DirectorEventType.HospitalAnnouncement && ClipsValid() &&
            order != null && voiceSource != null && voiceSource.isActiveAndEnabled;

        private static bool Loaded(AudioClip clip)
        {
            if (clip.loadState == AudioDataLoadState.Loaded) return true;
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            return clip.loadState == AudioDataLoadState.Loaded;
        }

        public bool TryStart(DirectorPlanningAction candidate)
        {
            if (!Application.isPlaying || running || !Supports(candidate.definition) || eligibility == null ||
                reservations == null || memory == null || observation == null || !memory.IsReady) return false;
            AudioClip voice = voiceClips[order[completedInCycle]];
            if (!Loaded(voice))
            { status = "Waiting for announcement audio data"; return false; }
            if (!eligibility.TryReserveCandidate(candidate.definition, candidate.anchor, out token)) return false;
            var check = eligibility.Evaluate(candidate.definition, candidate.anchor, token);
            if (!check.eligible) { status = check.reason; Release(); return false; }
            currentDuration = NextDuration;
            if (!reservations.Renew(token, currentDuration + 2f)) { Release(); return false; }
            if (!memory.BeginEvent(candidate.definition.EventId, candidate.definition.EventType,
                    candidate.anchorId, candidate.roomId)) { Release(); return false; }
            action = candidate; ownsMemory = true; running = true; delivered = false;
            currentVoice = voice.name;
            try
            {
                Configure(voiceSource, voice, voiceVolume, candidate.anchor.Position);
                voiceStarts = AudioSettings.dspTime + ScheduleLead;
                endsAt = voiceStarts + voice.length;
                voiceSource.PlayScheduled(voiceStarts);
                status = "Scheduled " + currentVoice;
                return true;
            }
            catch (Exception exception)
            {
                Finish(DirectorEventOutcome.Cancelled);
                status = "Announcement audio could not start: " + exception.Message;
                return false;
            }
        }

        private static void Configure(AudioSource source, AudioClip clip, float volume, Vector3 position)
        {
            source.Stop(); source.transform.position = position; source.clip = clip;
            source.volume = volume; source.pitch = 1f; source.loop = false;
            source.playOnAwake = false; source.dopplerLevel = 0f; source.spatialBlend = 1f;
        }

        private void Update()
        {
            if (!running) return;
            var sample = observation == null ? default : observation.Current;
            if (Time.timeScale <= 0f || AudioListener.pause || observation == null || !observation.isActiveAndEnabled ||
                eligibility == null || !eligibility.isActiveAndEnabled || memory == null || !memory.IsReady ||
                !memory.EventActive || reservations == null || !reservations.isActiveAndEnabled ||
                !reservations.Owns(action.anchor, token) || !sample.IsValid ||
                Time.time - sample.SampledAt > .5f || sample.SampledAt > Time.time || !sample.IsAlive ||
                sample.DamageRecoveryActive || sample.Victory != DirectorSignalState.Inactive ||
                sample.KeypadInteraction != DirectorSignalState.Inactive || sample.RecentGunfire != DirectorSignalState.Inactive ||
                sample.HunterDanger != DirectorSignalState.Inactive || action.anchor == null || !action.anchor.IsAvailable ||
                voiceSource == null || !voiceSource.isActiveAndEnabled)
            { Finish(DirectorEventOutcome.Cancelled); return; }
            // A room boundary alone must not cut a sentence short. The sound stays at its anchor.
            double now = AudioSettings.dspTime;
            if (!delivered && now >= voiceStarts)
            { memory.MarkEffectDelivered(action.definition.PredictedTensionIncrease); delivered = true; }
            if (now >= endsAt) { Finish(DirectorEventOutcome.Completed); return; }
            status = now < voiceStarts ? "Scheduled at " + action.anchorId : "Playing " + currentVoice;
        }

        private void Finish(DirectorEventOutcome outcome)
        {
            if (voiceSource != null) voiceSource.Stop();
            running = false;
            if (ownsMemory && memory != null) memory.FinishEvent(outcome);
            ownsMemory = false;
            if (outcome == DirectorEventOutcome.Completed)
            {
                lastCompletedIndex = order[completedInCycle];
                completedInCycle++;
                if (completedInCycle == order.Length)
                { completedInCycle = 0; completedCycles++; ShuffleCycle(); }
            }
            // A safety interruption retries the unfinished line at the next eligible event.
            Release(); status = outcome + ": " + currentVoice;
        }

        private void Release()
        {
            if (reservations != null && token != null) reservations.Release(token);
            token = null;
        }

        private void OnDisable()
        { if (running || ownsMemory) Finish(DirectorEventOutcome.Cancelled); }
    }
}

