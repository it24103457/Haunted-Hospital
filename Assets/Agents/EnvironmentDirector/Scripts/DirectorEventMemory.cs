using System;
using System.Collections.Generic;
using UnityEngine;

namespace HauntedHospital.EnvironmentAI
{
    public enum DirectorEventOutcome
    {
        Completed,
        Cancelled,
        Failed
    }

    [Serializable]
    public struct DirectorEventRecord
    {
        public string eventId;
        public DirectorEventType eventType;
        public string anchorId;
        public string roomId;

        public float startedAt;
        public float endedAt;

        public DirectorEventOutcome outcome;
        public bool effectDelivered;
        public bool debugRecord;
    }

    [DisallowMultipleComponent]
    public sealed class DirectorEventMemory : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private DirectorSettings directorSettings;

        [SerializeField]
        private DirectorTensionSettings tensionSettings;

        [Header("Runtime display")]
        [SerializeField]
        private float eventPressure;

        [SerializeField]
        private float quietSeconds;

        [SerializeField]
        private float secondsSinceEventEnded;

        [SerializeField]
        private bool hasEndedEvent;

        [SerializeField]
        private bool eventActive;

        [SerializeField]
        private DirectorEventRecord activeEvent;

        [SerializeField]
        private List<DirectorEventRecord> history =
            new List<DirectorEventRecord>();

        [Header("Shared voice cycle - completed clips only")]
        [SerializeField] private int phoneClipsCompleted;
        [SerializeField] private int whisperClipsCompleted;
        [SerializeField] private int announcementClipsCompleted;
        [SerializeField] private int completedVoiceCycles;

        // These match the validated distinct clip counts required by the three executors.
        // This memory is independent of the bounded diagnostic history.
        public bool VoiceCycleExhausted(DirectorEventType type) =>
            type == DirectorEventType.PhoneRing ? phoneClipsCompleted >= 3 :
            type == DirectorEventType.Whisper ? whisperClipsCompleted >= 4 :
            type == DirectorEventType.HospitalAnnouncement && announcementClipsCompleted >= 4;

        private void CompleteVoiceClip(DirectorEventType type)
        {
            if (type == DirectorEventType.PhoneRing) phoneClipsCompleted = Math.Min(3, phoneClipsCompleted + 1);
            else if (type == DirectorEventType.Whisper) whisperClipsCompleted = Math.Min(4, whisperClipsCompleted + 1);
            else if (type == DirectorEventType.HospitalAnnouncement) announcementClipsCompleted = Math.Min(4, announcementClipsCompleted + 1);
            if (phoneClipsCompleted == 3 && whisperClipsCompleted == 4 && announcementClipsCompleted == 4)
            {
                completedVoiceCycles++;
                phoneClipsCompleted = whisperClipsCompleted = announcementClipsCompleted = 0;
            }
        }

        private IReadOnlyList<DirectorEventRecord> historyView;

        private float lastDeliveredAt;
        private float lastEndedAt;
        private float lastPressureUpdate;
        private bool initialized;
        // Independent of the bounded diagnostic history: old records cannot erase cooldowns.
        private readonly Dictionary<string, float> cooldownEnds = new Dictionary<string, float>();

        public float CooldownRemaining(string eventId) =>
            !string.IsNullOrEmpty(eventId) && cooldownEnds.TryGetValue(eventId, out float end)
                ? Mathf.Max(0f, end - Time.time) : 0f;

        public bool IsReady =>
            initialized &&
            isActiveAndEnabled &&
            directorSettings != null &&
            tensionSettings != null &&
            tensionSettings.IsValid() &&
            Finite(directorSettings.MinimumTimeBetweenEvents) &&
            directorSettings.MinimumTimeBetweenEvents >= 0f;

        public bool EventActive => eventActive;

        // Read-only projection for planning; unlike EventPressure this does not advance memory.
        public float PlanningEventPressure => IsReady
            ? Mathf.Clamp(eventPressure - tensionSettings.EventPressureDecayPerSecond *
                (eventActive && activeEvent.effectDelivered ? 0f : Mathf.Max(0f, Time.time - lastPressureUpdate)), 0f, tensionSettings.MaximumEventPressure)
            : 0f;

        public IReadOnlyList<DirectorEventRecord> History =>
            historyView ?? (historyView = history.AsReadOnly());

        public bool GlobalIntervalReady
        {
            get
            {
                if (!IsReady || eventActive)
                    return false;

                return !hasEndedEvent ||
                    Time.time - lastEndedAt >=
                    directorSettings.MinimumTimeBetweenEvents;
            }
        }

        public float QuietSeconds =>
            initialized
                ? (eventActive && activeEvent.effectDelivered ? 0f : Mathf.Max(0f, Time.time - lastDeliveredAt))
                : 0f;

        public float SecondsSinceEventEnded =>
            hasEndedEvent
                ? Mathf.Max(0f, Time.time - lastEndedAt)
                : -1f;

        public float EventPressure
        {
            get
            {
                AdvancePressure();
                return eventPressure;
            }
        }

        private void Awake()
        {
            phoneClipsCompleted = whisperClipsCompleted = announcementClipsCompleted = completedVoiceCycles = 0;
            history.Clear();
            cooldownEnds.Clear();

            eventPressure = 0f;
            eventActive = false;
            hasEndedEvent = false;
            activeEvent = default;

            lastDeliveredAt = Time.time;
            lastEndedAt = Time.time;
            lastPressureUpdate = Time.time;

            initialized = true;
        }

        private void Start()
        {
            if (!IsReady)
            {
                Debug.LogError(
                    "[Environment Director] Event Memory requires valid " +
                    "Director Settings and Tension Settings.",
                    this);
            }
        }

        private void Update()
        {
            if (!IsReady)
                return;

            AdvancePressure();

            quietSeconds = QuietSeconds;
            secondsSinceEventEnded = SecondsSinceEventEnded;

            while (history.Count > tensionSettings.HistoryCapacity)
                history.RemoveAt(0);
        }

        private void AdvancePressure()
        {
            if (!IsReady)
                return;

            float now = Time.time;
            float elapsed = eventActive && activeEvent.effectDelivered ? 0f : Mathf.Max(0f, now - lastPressureUpdate);

            eventPressure = Mathf.Clamp(
                eventPressure -
                tensionSettings.EventPressureDecayPerSecond * elapsed,
                0f,
                tensionSettings.MaximumEventPressure);

            lastPressureUpdate = now;
        }

        // Called when an executor starts an event.
        // Starting alone does not add pressure or reset quiet time.
        public bool BeginEvent(
            string eventId,
            DirectorEventType eventType,
            string anchorId,
            string roomId,
            bool debugRecord = false)
        {
            if (!Application.isPlaying ||
                !IsReady ||
                eventActive ||
                eventType == DirectorEventType.Rest ||
                (!debugRecord && VoiceCycleExhausted(eventType)) ||
                !Enum.IsDefined(typeof(DirectorEventType), eventType) ||
                string.IsNullOrWhiteSpace(eventId) ||
                string.IsNullOrWhiteSpace(anchorId) ||
                string.IsNullOrWhiteSpace(roomId))
            {
                return false;
            }

            activeEvent = new DirectorEventRecord
            {
                eventId = eventId,
                eventType = eventType,
                anchorId = anchorId,
                roomId = roomId,
                startedAt = Time.time,
                endedAt = -1f,
                effectDelivered = false,
                debugRecord = debugRecord
            };

            eventActive = true;
            return true;
        }

        // Call once when the effect actually begins playing/showing.
        // Repeated calls for the same event cannot add pressure twice.
        public bool MarkEffectDelivered(float pressureIncrease)
        {
            if (!IsReady ||
                !eventActive ||
                activeEvent.effectDelivered ||
                !Finite(pressureIncrease) ||
                pressureIncrease < 0f ||
                pressureIncrease > 1f)
            {
                return false;
            }

            AdvancePressure();

            activeEvent.effectDelivered = true;

            eventPressure = Mathf.Clamp(
                eventPressure + pressureIncrease,
                0f,
                tensionSettings.MaximumEventPressure);

            lastDeliveredAt = Time.time;
            return true;
        }

        // The executor calls this AFTER restoring its lights/audio/visuals.
        public bool FinishEvent(DirectorEventOutcome outcome)
        {
            if (!IsReady ||
                !eventActive ||
                !Enum.IsDefined(typeof(DirectorEventOutcome), outcome))
            {
                return false;
            }

            AdvancePressure();
            if (activeEvent.effectDelivered) lastDeliveredAt = Time.time;
            activeEvent.endedAt = Time.time;
            activeEvent.outcome = outcome;

            if (!activeEvent.debugRecord && activeEvent.effectDelivered && outcome == DirectorEventOutcome.Completed)
                CompleteVoiceClip(activeEvent.eventType);
            history.Add(activeEvent);
            if (!activeEvent.debugRecord)
                foreach (var definition in directorSettings.Events)
                    if (definition != null && definition.EventId == activeEvent.eventId)
                    {
                        cooldownEnds[activeEvent.eventId] = Time.time + definition.Cooldown;
                        break;
                    }

            while (history.Count > tensionSettings.HistoryCapacity)
                history.RemoveAt(0);

            lastEndedAt = Time.time;
            hasEndedEvent = true;
            eventActive = false;
            activeEvent = default;

            return true;
        }

        [ContextMenu("Debug: Simulate delivered event (+0.20 pressure)")]
        private void DebugDeliveredEvent()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode before using this test.", this);
                return;
            }

            var observation = GetComponent<DirectorObservation>();

            if (observation == null ||
                !observation.Current.IsValid ||
                !observation.Current.IsAlive ||
                !GlobalIntervalReady)
            {
                Debug.LogWarning(
                    "Debug event requires a living observed player " +
                    "and an available global event interval.",
                    this);
                return;
            }

            if (!BeginEvent(
                "debug_delivered_event",
                DirectorEventType.LightFlicker,
                "debug_anchor",
                observation.Current.RoomId,
                true))
            {
                return;
            }

            MarkEffectDelivered(0.20f);
            FinishEvent(DirectorEventOutcome.Completed);

            Debug.Log(
                "[Environment Director] Simulated one delivered event. " +
                "No light or sound was changed.",
                this);
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
