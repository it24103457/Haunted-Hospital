using System;
using UnityEngine;

/// <summary>What kind of sound it was. Agents decide for themselves how important each type is.</summary>
public enum NoiseType
{
    Footstep,
    Gunshot,
    DoorSlam,
    ThrownObject,
    BarricadeBreak,
    Other
}

/// <summary>One sound that happened in the world. Facts only: no agent opinions in here.</summary>
public readonly struct NoiseEvent
{
    public readonly Vector3 Position;
    public readonly float Loudness;
    public readonly NoiseType Type;
    public readonly float Time;
    public readonly GameObject Source;

    public NoiseEvent(Vector3 position, float loudness, NoiseType type, float time, GameObject source)
    {
        Position = position;
        Loudness = loudness;
        Type = type;
        Time = time;
        Source = source;
    }
}

/// <summary>
/// Recommended starting loudness for each kind of sound, in one place so every system agrees.
/// With the default attenuation (0.12 per metre) and a hearing threshold of 1, a sound of loudness L
/// carries ln(L) / 0.12 metres down an open corridor:
///   crouch 1.5 -> 3 m     walk 4 -> 12 m     sprint 10 -> 19 m     door slam 15 -> 23 m
///   thrown object 20 -> 25 m     barricade break 25 -> 27 m     gunshot 40 -> 31 m
/// </summary>
public static class NoiseLevels
{
    public const float CrouchStep = 1.5f;
    public const float WalkStep = 4f;
    public const float SprintStep = 10f;
    public const float DoorSlam = 15f;
    public const float ThrownObject = 20f;
    public const float BarricadeBreak = 25f;
    public const float Gunshot = 40f;
}

/// <summary>
/// The world's noise board. Anything that makes a sound calls Noise.Emit; any listener (the Hunter, a debug view)
/// subscribes to Noise.Emitted. The emitter never knows who is listening.
/// Subscribers must unsubscribe in OnDisable, or they keep receiving events after they are destroyed.
/// </summary>
public static class Noise
{
    public static event Action<NoiseEvent> Emitted;

    public static void Emit(Vector3 position, float loudness, NoiseType type, GameObject source = null)
    {
        if (loudness <= 0f) return;

        Emitted?.Invoke(new NoiseEvent(position, loudness, type, UnityEngine.Time.time, source));
    }

    // Clears leftover subscribers when entering Play mode without a domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Emitted = null;
    }
}
