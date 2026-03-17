using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Safe camera rebinding pattern for scene objects that need a camera during AR/non-AR transitions.
/// </summary>
public class ARCameraBinder : MonoBehaviour
{
    [SerializeField] private Camera defaultSceneCamera;
    [SerializeField] private bool preferARCameraWhenAvailable;

    public Camera CurrentCamera { get; private set; }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        RebindCamera();
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)   
    {
        RebindCamera();
    }

    public void RebindCamera()
    {
        Camera candidate = null;

        if (preferARCameraWhenAvailable && ARRuntimeContext.Instance != null)
            candidate = ARRuntimeContext.Instance.ResolveARCamera();

        if (candidate == null && defaultSceneCamera != null)
            candidate = defaultSceneCamera;

        if (candidate == null)
            candidate = Camera.main;

        if (candidate == null)
        {
            var allCameras = FindObjectsOfType<Camera>(true);
            if (allCameras.Length > 0)
                candidate = allCameras[0];
        }

        CurrentCamera = candidate;
    }
}
