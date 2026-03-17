using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

/// <summary>
/// Boot-scene owner for persistent AR systems. Guarantees single AR root across scene loads.
/// </summary>
public class ARBootstrapPersistent : MonoBehaviour
{
    public static ARBootstrapPersistent Instance { get; private set; }

    [Header("AR Root (Boot Scene Only)")]
    [SerializeField] private GameObject arRoot;

    [Header("Core Components")]
    [SerializeField] private ARSession arSession;
    [SerializeField] private GameObject xrOriginRoot;
    [SerializeField] private Camera arCamera;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private MonoBehaviour xrInteractionManager;

    [Header("Duplicate Protection")]
    [SerializeField] private bool destroyDuplicateARObjects = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (arRoot == null)
            arRoot = gameObject;

        DontDestroyOnLoad(arRoot);

        AutoBindMissingReferences();
        EnsureRuntimeContext();

        ARRuntimeContext.Instance.Register(
            arRoot,
            arSession,
            xrOriginRoot,
            arCamera,
            raycastManager,
            planeManager,
            cameraManager,
            xrInteractionManager
        );

        // Disable AR at startup - will be enabled when needed
        if (arSession != null)
            arSession.enabled = false;
        if (arCamera != null)
            arCamera.enabled = false;

        SceneManager.sceneLoaded += OnSceneLoaded;
        
#if UNITY_EDITOR
        // Watch for XR Simulation environment and parent it under ARCoreRoot
        StartCoroutine(WatchAndReparentSimulationEnvironment());
#endif
    }
    
#if UNITY_EDITOR
    private bool simulationDetected = false;
    
    private System.Collections.IEnumerator WatchAndReparentSimulationEnvironment()
    {
        while (true)
        {
            yield return null; // Check every frame for fast response
            
            if (simulationDetected)
            {
                yield return new WaitForSeconds(5f);
                continue;
            }
            
            // Find simulation scene - DO NOT reparent, just detect and log
            // Reparenting breaks XR Simulation's internal lighting and rendering
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                
                if (scene.name.Contains("Simulated Environment"))
                {
                    simulationDetected = true;
                    Debug.Log($"ARBootstrapPersistent: XR Simulation environment detected in scene '{scene.name}'. Leaving it unmodified for proper rendering.");
                    break;
                }
            }
        }
    }
#endif


    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (Instance == this)
            Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ARRuntimeContext.Instance.RebindIfMissing();
        // ValidateNoDuplicateARSystems(); // Disabled - breaks XR Simulation
    }

    private void ValidateNoDuplicateARSystems()
    {
        // Disabled - destroying duplicate AR systems breaks XR Simulation's transform references
        return;
    }

    private void RemoveDuplicateComponentsOfType<T>(string label) where T : Component
    {
        var all = FindObjectsOfType<T>(true);
        for (int i = 0; i < all.Length; i++)
        {
            var component = all[i];
            if (component == null)
                continue;

            if (component.transform.IsChildOf(arRoot.transform))
                continue;

            Debug.LogWarning($"ARBootstrapPersistent: Duplicate {label} found in scene on '{component.gameObject.name}'.");
            if (destroyDuplicateARObjects)
                Destroy(component.gameObject);
        }
    }

    private void RemoveDuplicateComponentsByTypeName(string typeName)
    {
        var allBehaviours = FindObjectsOfType<MonoBehaviour>(true);
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            var behaviour = allBehaviours[i];
            if (behaviour == null)
                continue;

            if (!string.Equals(behaviour.GetType().Name, typeName, System.StringComparison.OrdinalIgnoreCase))
                continue;

            if (behaviour.transform.IsChildOf(arRoot.transform))
                continue;

            Debug.LogWarning($"ARBootstrapPersistent: Duplicate {typeName} found in scene on '{behaviour.gameObject.name}'.");
            if (destroyDuplicateARObjects)
                Destroy(behaviour.gameObject);
        }
    }

    private void AutoBindMissingReferences()
    {
        if (arSession == null)
            arSession = arRoot.GetComponentInChildren<ARSession>(true);

        if (xrOriginRoot == null)
        {
            var candidate = FindNamedChild(arRoot.transform, "XR Origin (Mobile AR)", "XR Origin", "AR Session Origin");
            xrOriginRoot = candidate != null ? candidate.gameObject : null;
        }

        if (arCamera == null)
        {
            var xrOrigin = xrOriginRoot != null ? xrOriginRoot.GetComponent<XROrigin>() : null;
            if (xrOrigin != null && xrOrigin.Camera != null)
                arCamera = xrOrigin.Camera;

            if (arCamera == null && xrOriginRoot != null)
            {
                var candidates = xrOriginRoot.GetComponentsInChildren<Camera>(true);
                for (int i = 0; i < candidates.Length; i++)
                {
                    var candidate = candidates[i];
                    if (candidate != null && candidate.GetComponent<ARCameraManager>() != null)
                    {
                        arCamera = candidate;
                        break;
                    }
                }

                if (arCamera == null)
                    arCamera = xrOriginRoot.GetComponentInChildren<Camera>(true);
            }

            if (arCamera == null)
                arCamera = arRoot.GetComponentInChildren<Camera>(true);
        }

        if (raycastManager == null && xrOriginRoot != null)
            raycastManager = xrOriginRoot.GetComponentInChildren<ARRaycastManager>(true);

        if (raycastManager == null)
            raycastManager = arRoot.GetComponentInChildren<ARRaycastManager>(true);

        if (planeManager == null)
            planeManager = arRoot.GetComponentInChildren<ARPlaneManager>(true);

        if (cameraManager == null)
            cameraManager = arRoot.GetComponentInChildren<ARCameraManager>(true);

        if (cameraManager == null && arCamera != null)
            cameraManager = arCamera.GetComponent<ARCameraManager>();

        if (xrInteractionManager == null)
            xrInteractionManager = FindComponentByTypeName(arRoot, "XRInteractionManager");
    }

    private static Transform FindNamedChild(Transform root, params string[] names)
    {
        if (root == null)
            return null;

        var nodes = root.GetComponentsInChildren<Transform>(true);
        foreach (var node in nodes)
        {
            foreach (var name in names)
            {
                if (string.Equals(node.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return node;
            }
        }

        return null;
    }

    private static MonoBehaviour FindComponentByTypeName(GameObject root, string typeName)
    {
        if (root == null || string.IsNullOrWhiteSpace(typeName))
            return null;

        var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            if (string.Equals(behaviour.GetType().Name, typeName, System.StringComparison.OrdinalIgnoreCase))
                return behaviour;
        }

        return null;
    }

    private static void EnsureRuntimeContext()
    {
        if (ARRuntimeContext.Instance != null)
            return;

        var contextObject = new GameObject("ARRuntimeContext");
        contextObject.AddComponent<ARRuntimeContext>();
    }
}
