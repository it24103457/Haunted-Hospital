namespace HauntedHospital.EnvironmentAI
{
    public enum DirectorEventType
    {
        Rest = 0,
        LightFlicker = 1,
        LocalBlackout = 2,
        PhoneRing = 3,
        Whisper = 4,
        Knock = 5,
        HospitalAnnouncement = 6,
        DistantApparition = 7,
        LightPulseApparition = 8
    }

    public enum DirectorEventIntensity
    {
        Subtle = 0,
        Moderate = 1,
        Strong = 2
    }

    public enum DirectorTargetType
    {
        DirectorLightGroup = 0,
        Phone = 1,
        AudioAnchor = 2,
        Loudspeaker = 3,
        ApparitionAnchor = 4
    }
}