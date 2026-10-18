using UnityEngine;

/*
 * Spins the helicopter's rotors for the escape cutscene.
 * The main rotor and its hub turn around the main rotor's own axis; the tail rotor turns around its own axis.
 * Speed is a multiplier on the base turns per second, so the cutscene can spin the rotors up for the lift-off.
 * Runs on real time, like the rest of the cutscene.
 */
public class HeliRotors : MonoBehaviour
{
    [SerializeField] private Transform mainRotor;
    [Tooltip("The main rotor's spin axis in its own local space.")]
    [SerializeField] private Vector3 mainAxis = Vector3.forward;
    [SerializeField] private Transform hub;
    [SerializeField] private Transform tailRotor;
    [Tooltip("The tail rotor's spin axis in its own local space.")]
    [SerializeField] private Vector3 tailAxis = Vector3.up;
    [Tooltip("Main rotor turns per second at speed 1.")]
    [SerializeField, Min(0f)] private float mainTurnsPerSecond = 4f;
    [Tooltip("Tail rotor turns per second at speed 1.")]
    [SerializeField, Min(0f)] private float tailTurnsPerSecond = 9f;

    public float speed = 1f;

    private void Update()
    {
        float dt = Time.unscaledDeltaTime * speed;
        if (mainRotor != null)
        {
            Vector3 axis = mainRotor.TransformDirection(mainAxis);
            float angle = mainTurnsPerSecond * 360f * dt;
            mainRotor.Rotate(axis, angle, Space.World);
            if (hub != null) hub.RotateAround(mainRotor.position, axis, angle);
        }
        if (tailRotor != null) tailRotor.Rotate(tailRotor.TransformDirection(tailAxis), tailTurnsPerSecond * 360f * dt, Space.World);
    }
}
