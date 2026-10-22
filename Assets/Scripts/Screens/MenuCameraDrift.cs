using UnityEngine;

/*
 * Gives the main menu camera a slow, subtle drift, like a handheld shot, so the menu background feels alive.
 * The camera wanders a few centimetres and a fraction of a degree around the pose it starts in, following smooth noise
 * on each axis, so the motion never repeats in an obvious loop and never strays from the chosen shot.
 * The drift stops for good once the wake-up intro starts, leaving the camera to the intro. It runs on real time.
 */
public class MenuCameraDrift : MonoBehaviour
{
    [SerializeField] private WakeUpIntro intro;
    [Tooltip("Largest distance the camera moves from its starting point on each axis, in metres (right, up, forward).")]
    [SerializeField] private Vector3 moveAmount = new Vector3(0.04f, 0.025f, 0.05f);
    [Tooltip("Largest turn from its starting angle on each axis, in degrees (pitch, yaw, roll).")]
    [SerializeField] private Vector3 turnAmount = new Vector3(0.5f, 0.8f, 0.3f);
    [Tooltip("How fast the drift wanders. Lower is slower and calmer.")]
    [SerializeField, Min(0f)] private float speed = 0.08f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private float time;

    // Remembers where the camera starts, since the drift is always an offset from there.
    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        // A random starting point in the noise, so the drift is different each time the menu opens.
        time = Random.Range(0f, 100f);
    }

    // Sways the camera slightly around its start; stops for good once the wake-up intro takes the camera over.
    private void Update()
    {
        if (intro != null && intro.IsRunning)
        {
            enabled = false;
            return;
        }

        time += Time.unscaledDeltaTime * speed;

        Vector3 move = new Vector3(Noise(0f) * moveAmount.x, Noise(10f) * moveAmount.y, Noise(20f) * moveAmount.z);
        Vector3 turn = new Vector3(Noise(30f) * turnAmount.x, Noise(40f) * turnAmount.y, Noise(50f) * turnAmount.z);

        transform.position = startPosition + startRotation * move;
        transform.rotation = startRotation * Quaternion.Euler(turn);
    }

    // Smooth noise between -1 and 1; each axis reads a different row so they move independently.
    private float Noise(float row)
    {
        return Mathf.PerlinNoise(time, row) * 2f - 1f;
    }
}
