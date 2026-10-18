using UnityEngine;
using UnityEngine.UI;

/*
 * Shows a slowly turning 3D model inside a UI RawImage, for the How to Play pages.
 * A small stage (its own camera and two lights) is built far below the level, so no layers or project settings are needed
 * and the level's cameras never see it. The camera draws into a render texture shown by the RawImage.
 * Each model is a display copy of a game prefab: it is created under an inactive holder so none of its scripts ever start,
 * then every script, collider, physics body, NavMesh part, animator and sound is removed before it is shown.
 * The copy is centred and the camera distance is fitted to its size, so small and large prefabs both fill the view.
 * It turns on unscaled time, so it keeps turning while the game is paused.
 */
public class ModelPreview : MonoBehaviour
{
    [SerializeField] private RawImage target;
    [SerializeField] private Color background = new Color(0.07f, 0.07f, 0.08f, 1f);
    [SerializeField, Min(0f)] private float turnSpeed = 25f;
    [SerializeField, Range(10f, 60f)] private float fieldOfView = 30f;
    [SerializeField, Min(64)] private int resolution = 1024;

    private static readonly Vector3 StagePosition = new Vector3(0f, -1000f, 0f);

    private Transform stage;
    private Transform turntable;
    private Transform holder;
    private Camera stageCamera;
    private RenderTexture texture;
    private GameObject current;

    private void Awake()
    {
        if (stage == null) BuildStage();
    }

    private void OnEnable()
    {
        if (stage != null) stage.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        if (stage != null) stage.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (stage != null) Destroy(stage.gameObject);
        if (texture != null) texture.Release();
    }

    private void Update()
    {
        if (turntable != null) turntable.Rotate(0f, turnSpeed * Time.unscaledDeltaTime, 0f, Space.World);
    }

    // Shows a display copy of the prefab, or clears the view when prefab is null.
    public void Show(GameObject prefab, Vector3 extraRotation)
    {
        // The page can ask for a model before this component's own Awake has run.
        if (stage == null) BuildStage();

        // Destroy only happens at the end of the frame, so the old copy is hidden first to stop both showing at once.
        if (current != null)
        {
            current.SetActive(false);
            Destroy(current);
        }
        current = null;
        turntable.localRotation = Quaternion.identity;
        if (prefab == null) return;

        holder.gameObject.SetActive(false);
        current = Instantiate(prefab, holder);
        current.transform.localPosition = Vector3.zero;
        current.transform.localRotation = Quaternion.Euler(extraRotation);
        StripToVisuals(current);
        current.transform.SetParent(turntable, false);
        current.SetActive(true);

        Frame(current);
    }

    private void BuildStage()
    {
        stage = new GameObject("ModelPreviewStage").transform;
        stage.position = StagePosition;

        turntable = new GameObject("Turntable").transform;
        turntable.SetParent(stage, false);

        holder = new GameObject("InactiveHolder").transform;
        holder.SetParent(stage, false);
        holder.gameObject.SetActive(false);

        var key = new GameObject("KeyLight").AddComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 1.2f;
        key.transform.SetParent(stage, false);
        key.transform.rotation = Quaternion.Euler(35f, -40f, 0f);

        var fill = new GameObject("FillLight").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.45f;
        fill.color = new Color(0.75f, 0.8f, 1f);
        fill.transform.SetParent(stage, false);
        fill.transform.rotation = Quaternion.Euler(20f, 150f, 0f);

        texture = new RenderTexture(resolution, resolution, 24) { name = "ModelPreviewTexture", antiAliasing = 4 };

        stageCamera = new GameObject("PreviewCamera").AddComponent<Camera>();
        stageCamera.transform.SetParent(stage, false);
        stageCamera.clearFlags = CameraClearFlags.SolidColor;
        stageCamera.backgroundColor = background;
        stageCamera.fieldOfView = fieldOfView;
        stageCamera.targetTexture = texture;
        stageCamera.depth = -10f;

        if (target != null) target.texture = texture;
        stage.gameObject.SetActive(isActiveAndEnabled);
    }

    /*
     * Removes everything from the copy except what is needed to draw it.
     * Scripts go first, then the parts they may depend on. A component that another remaining component still requires
     * is skipped on that pass and removed on a later one, once the component needing it is gone.
     */
    private static void StripToVisuals(GameObject copy)
    {
        for (int pass = 0; pass < 4; pass++)
        {
            bool removedAny = false;
            foreach (var c in copy.GetComponentsInChildren<Component>(true))
            {
                if (c == null || !IsGameplayPart(c) || IsRequiredByAnother(c)) continue;
                DestroyImmediate(c);
                removedAny = true;
            }
            if (!removedAny) break;
        }

        foreach (var p in copy.GetComponentsInChildren<ParticleSystem>(true)) p.gameObject.SetActive(false);
    }

    private static bool IsGameplayPart(Component c)
    {
        return c is MonoBehaviour || c is Collider || c is Rigidbody || c is Animator || c is AudioSource || c is Light
            || c is UnityEngine.AI.NavMeshAgent || c is UnityEngine.AI.NavMeshObstacle;
    }

    private static bool IsRequiredByAnother(Component target)
    {
        System.Type type = target.GetType();
        foreach (var other in target.GetComponents<Component>())
        {
            if (other == null || other == target) continue;
            foreach (RequireComponent req in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
            {
                if (Needs(req.m_Type0, type) || Needs(req.m_Type1, type) || Needs(req.m_Type2, type)) return true;
            }
        }
        return false;
    }

    private static bool Needs(System.Type required, System.Type type)
    {
        return required != null && required.IsAssignableFrom(type);
    }

    // Centres the copy on the turntable and moves the camera back far enough to fit it.
    private void Frame(GameObject copy)
    {
        var renderers = copy.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);

        copy.transform.position -= bounds.center - turntable.position;
        float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
        float distance = radius / Mathf.Sin(fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;

        stageCamera.transform.position = turntable.position + new Vector3(0f, radius * 0.35f, -distance);
        stageCamera.transform.LookAt(turntable.position);
        stageCamera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 2f);
        stageCamera.farClipPlane = distance + radius * 2f;
    }
}
