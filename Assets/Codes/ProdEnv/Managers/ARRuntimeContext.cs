using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
using System.Reflection;
using System.Collections;

/// <summary>
/// Persistent runtime registry for AR systems. Owns stable references and safe rebinding.
/// </summary>
public class ARRuntimeContext : MonoBehaviour
{
    public static ARRuntimeContext Instance { get; private set; }

    public GameObject ARRoot { get; private set; }
    public ARSession ARSession { get; private set; }
    public GameObject XROriginRoot { get; private set; }
    public XROrigin XROrigin { get; private set; }
    public Camera ARCamera { get; private set; }
    public ARRaycastManager RaycastManager { get; private set; }
    public ARPlaneManager PlaneManager { get; private set; }
    public ARCameraManager CameraManager { get; private set; }
    public MonoBehaviour XRInteractionManager { get; private set; }

    public bool IsReady => ARRoot != null && ARCamera != null;
    
    /// <summary>
    /// True if simulation was pre-initialized at boot (Editor only).
    /// </summary>
    public bool SimulationPreInitialized { get; private set; }
    
    private bool isARCurrentlyActive;

    /// <summary>
    /// Returns true when a UnityEngine.Object reference has been destroyed
    /// (the C++ side is gone) but the C# variable is not yet null.
    /// </summary>
    private static bool IsDestroyedUnityObject(Object obj)
    {
        // A destroyed Unity object is NOT "== null" from pure C#, but IS "== null"
        // through the overloaded operator.  ReferenceEquals skips that operator.
        return !ReferenceEquals(obj, null) && obj == null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindIfMissing();
#if UNITY_EDITOR
        if (ARSession != null && ARSession.enabled)
            StartCoroutine(ProtectSimulationInternalObjectsDeferred());
#endif
    }

    public void Register(
        GameObject arRoot,
        ARSession arSession,
        GameObject xrOriginRoot,
        Camera arCamera,
        ARRaycastManager raycastManager,
        ARPlaneManager planeManager,
        ARCameraManager cameraManager,
        MonoBehaviour xrInteractionManager)
    {
        ARRoot = arRoot;
        ARSession = arSession;
        XROriginRoot = xrOriginRoot;
        ARCamera = arCamera;
        RaycastManager = raycastManager;
        PlaneManager = planeManager;
        CameraManager = cameraManager;
        XRInteractionManager = xrInteractionManager;
    }

    public void RebindIfMissing()
    {
        // ---- Detect stale (destroyed) cached references and null them out ----
        if (IsDestroyedUnityObject(ARRoot))      ARRoot = null;
        if (IsDestroyedUnityObject(ARSession))   ARSession = null;
        if (IsDestroyedUnityObject(XROriginRoot)) XROriginRoot = null;
        if (IsDestroyedUnityObject(XROrigin))    XROrigin = null;
        if (IsDestroyedUnityObject(ARCamera))    ARCamera = null;
        if (IsDestroyedUnityObject(RaycastManager))  RaycastManager = null;
        if (IsDestroyedUnityObject(PlaneManager))    PlaneManager = null;
        if (IsDestroyedUnityObject(CameraManager))   CameraManager = null;
        if (IsDestroyedUnityObject(XRInteractionManager)) XRInteractionManager = null;

        if (ARRoot == null)
        {
            var arRootCandidate = FindByNames("ARRoot", "AR Root", "ARCoreRoot");
            if (arRootCandidate != null)
                ARRoot = arRootCandidate;
        }

        if (ARSession == null && ARRoot != null)
            ARSession = ARRoot.GetComponentInChildren<ARSession>(true);

        if (XROriginRoot == null && ARRoot != null)
            XROriginRoot = FindByNamesUnder(ARRoot.transform, "XR Origin (Mobile AR)", "XR Origin", "AR Session Origin");

        if (XROrigin == null)
        {
            if (XROriginRoot != null)
                XROrigin = XROriginRoot.GetComponent<XROrigin>();

            if (XROrigin == null && ARRoot != null)
                XROrigin = ARRoot.GetComponentInChildren<XROrigin>(true);
        }

        if (ARCamera == null)
        {
            if (XROrigin != null && XROrigin.Camera != null)
                ARCamera = XROrigin.Camera;

            if (ARCamera == null && XROriginRoot != null)
            {
                var cameras = XROriginRoot.GetComponentsInChildren<Camera>(true);
                for (int i = 0; i < cameras.Length; i++)
                {
                    var candidate = cameras[i];
                    if (candidate != null && candidate.GetComponent<ARCameraManager>() != null)
                    {
                        ARCamera = candidate;
                        break;
                    }
                }

                if (ARCamera == null)
                    ARCamera = XROriginRoot.GetComponentInChildren<Camera>(true);
            }

            if (ARCamera == null && ARRoot != null)
                ARCamera = ARRoot.GetComponentInChildren<Camera>(true);
        }

        if (RaycastManager == null && ARRoot != null)
            RaycastManager = ARRoot.GetComponentInChildren<ARRaycastManager>(true);

        if (PlaneManager == null && ARRoot != null)
            PlaneManager = ARRoot.GetComponentInChildren<ARPlaneManager>(true);

        if (CameraManager == null && ARRoot != null)
            CameraManager = ARRoot.GetComponentInChildren<ARCameraManager>(true);

        if (CameraManager == null && ARCamera != null)
            CameraManager = ARCamera.GetComponent<ARCameraManager>();

        if (XRInteractionManager == null && ARRoot != null)
            XRInteractionManager = FindComponentByTypeName(ARRoot, "XRInteractionManager");
    }

    public void SetARActive(bool active)
    {
        RebindIfMissing();
        isARCurrentlyActive = active;

        if (active)
        {
            // Snap XR Origin to simulation origin so camera starts inside the environment
            if (XROriginRoot != null)
            {
                XROriginRoot.transform.position = Vector3.zero;
                XROriginRoot.transform.rotation = Quaternion.identity;
            }

            // Enable AR fully
            if (ARRoot != null)
                ARRoot.SetActive(true);

            if (ARSession != null)
            {
                ARSession.enabled = true;
                // Resume subsystem if it was paused (preserves XR Simulation environment)
                ResumeARSubsystem();
            }

            if (ARCamera != null)
            {
                ARCamera.gameObject.SetActive(true);
                ARCamera.enabled = true;
                ARCamera.cullingMask = -1;
                ARCamera.depth = 100;
                
                var bg = ARCamera.GetComponent<ARCameraBackground>();
                if (bg != null) bg.enabled = true;
            }

            if (CameraManager != null)
                CameraManager.enabled = true;

            if (PlaneManager != null)
                PlaneManager.enabled = true;

#if UNITY_EDITOR
            StartCoroutine(ProtectSimulationInternalObjectsDeferred());
#endif
        }
        else
        {
#if UNITY_EDITOR
            // In Editor with XR Simulation: ALWAYS pause subsystem instead of disabling
            // the session.  Disabling the session triggers Destroy() on the simulation
            // provider which disposes the SimulationCameraPoseProvider and its Transform.
            // The SimulationEnvironmentScanner singleton keeps a cached reference to that
            // Transform and will throw MissingReferenceException on the next AR activation
            // (e.g. entering mission 2 AR after exiting mission 1 AR).
            if (ARSession != null)
            {
                PauseARSubsystem();
                
                // Hide AR camera output but keep session alive
                if (ARCamera != null)
                    ARCamera.enabled = false;

                if (PlaneManager != null)
                    PlaneManager.enabled = false;

                if (CameraManager != null)
                    CameraManager.enabled = false;
                    
                return;
            }
#endif
            // Fallback: fully disable AR (used in builds or if not pre-initialized)
            if (ARSession != null)
                ARSession.enabled = false;

            if (ARCamera != null)
                ARCamera.enabled = false;
        }
    }
    
    /// <summary>
    /// Pre-initialize AR and XR Simulation environment at boot time (Editor only).
    /// Call this from Bootloader to ensure simulation is ready before any AR scene loads.
    /// </summary>
    public void PreInitializeSimulation()
    {
#if UNITY_EDITOR
        if (SimulationPreInitialized)
            return;
            
        RebindIfMissing();
        
        if (ARSession == null)
        {
            Debug.LogWarning("ARRuntimeContext: Cannot pre-initialize simulation - ARSession not found.");
            return;
        }
        
        Debug.Log("ARRuntimeContext: Pre-initializing XR Simulation environment...");
        
        // Briefly enable AR to create simulation environment
        if (ARRoot != null)
            ARRoot.SetActive(true);
            
        ARSession.enabled = true;
        
        // Mark as pre-initialized
        SimulationPreInitialized = true;
        
        // Immediately pause - simulation environment is now created and will persist
        PauseARSubsystem();
        
        // Hide AR camera until actually needed
        if (ARCamera != null)
            ARCamera.enabled = false;
            
        Debug.Log("ARRuntimeContext: XR Simulation pre-initialized and paused. Ready for AR scenes.");
#else
        Debug.Log("ARRuntimeContext: PreInitializeSimulation is Editor-only. Skipping in build.");
#endif
    }
    
    private void PauseARSubsystem()
    {
        if (ARSession == null)
            return;
            
        var subsystem = ARSession.subsystem;
        if (subsystem != null && subsystem.running)
        {
            subsystem.Stop();
            Debug.Log("ARRuntimeContext: AR subsystem paused (simulation environment preserved).");
        }
    }
    
    private void ResumeARSubsystem()
    {
        if (ARSession == null)
            return;
            
        var subsystem = ARSession.subsystem;
        if (subsystem != null && !subsystem.running)
        {
            subsystem.Start();
            Debug.Log("ARRuntimeContext: AR subsystem resumed.");
        }
    }

#if UNITY_EDITOR
    private IEnumerator ProtectSimulationInternalObjectsDeferred()
    {
        const int maxFrames = 30;

        for (int frame = 0; frame < maxFrames; frame++)
        {
            if (ProtectSimulationInternalObjects())
                yield break;

            yield return null;
        }
    }

    private bool ProtectSimulationInternalObjects()
    {
        var allBehaviours = FindObjectsOfType<MonoBehaviour>(true);
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            var behaviour = allBehaviours[i];
            if (behaviour == null)
                continue;

            if (!string.Equals(behaviour.GetType().Name, "SimulationCameraPoseProvider", System.StringComparison.Ordinal))
                continue;

            var providerObject = behaviour.gameObject;
            if (providerObject != null && providerObject.scene.name != "DontDestroyOnLoad")
            {
                DontDestroyOnLoad(providerObject);
                Debug.Log("ARRuntimeContext: Protected SimulationCameraPoseProvider with DontDestroyOnLoad.");
            }

            return true;
        }

        return false;
    }
#endif

    public Camera ResolveARCamera(Camera fallback = null)
    {
        RebindIfMissing();
        if (ARCamera != null)
            return ARCamera;
        return fallback;
    }

    public ARRaycastManager ResolveRaycastManager(ARRaycastManager fallback = null)
    {
        RebindIfMissing();

        if (RaycastManager != null && IsRaycastManagerValid(RaycastManager))
        {
            EnsureTrackablesParentForManager(RaycastManager);
            return RaycastManager;
        }

        if (XROriginRoot != null)
        {
            var managerUnderOrigin = XROriginRoot.GetComponentInChildren<ARRaycastManager>(true);
            if (managerUnderOrigin != null && IsRaycastManagerValid(managerUnderOrigin))
            {
                RaycastManager = managerUnderOrigin;
                EnsureTrackablesParentForManager(RaycastManager);
                return RaycastManager;
            }
        }

        var allManagers = FindObjectsOfType<ARRaycastManager>(true);
        for (int i = 0; i < allManagers.Length; i++)
        {
            var manager = allManagers[i];
            if (manager != null && IsRaycastManagerValid(manager))
            {
                RaycastManager = manager;
                EnsureTrackablesParentForManager(RaycastManager);
                return RaycastManager;
            }
        }

        if (fallback != null && IsRaycastManagerValid(fallback))
        {
            EnsureTrackablesParentForManager(fallback);
            return fallback;
        }

        return fallback;
    }

    private bool IsRaycastManagerValid(ARRaycastManager manager)
    {
        if (manager == null)
            return false;

        var origin = manager.GetComponent<XROrigin>() ?? manager.GetComponentInParent<XROrigin>();
        if (origin == null)
            return false;

        if (origin.Camera == null)
            return false;

        return true;
    }

    private void EnsureTrackablesParentForManager(ARRaycastManager manager)
    {
        if (manager == null)
            return;

        var origin = manager.GetComponent<XROrigin>() ?? manager.GetComponentInParent<XROrigin>();
        if (origin == null)
            return;

        if (origin.TrackablesParent != null)
            return;

        var trackablesObject = new GameObject("Trackables");
        trackablesObject.transform.SetParent(origin.transform, false);

        bool assigned = TryAssignTrackablesParent(origin, trackablesObject.transform);
        if (!assigned)
        {
            Debug.LogWarning("ARRuntimeContext: Could not assign XROrigin.TrackablesParent via reflection. ARRaycast may still fail.");
            return;
        }

        Debug.LogWarning("ARRuntimeContext: Auto-created missing XROrigin TrackablesParent at runtime.");
    }

    private bool TryAssignTrackablesParent(XROrigin origin, Transform trackables)
    {
        if (origin == null || trackables == null)
            return false;

        var type = origin.GetType();

        var property = type.GetProperty("TrackablesParent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null)
        {
            var setter = property.GetSetMethod(true);
            if (setter != null)
            {
                setter.Invoke(origin, new object[] { trackables });
                return true;
            }
        }

        var field = type.GetField("<TrackablesParent>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? type.GetField("m_TrackablesParent", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? type.GetField("m_TrackablesParentTransform", BindingFlags.Instance | BindingFlags.NonPublic);

        if (field == null)
            return false;

        field.SetValue(origin, trackables);
        return true;
    }

    private static GameObject FindByNames(params string[] names)
    {
        foreach (var name in names)
        {
            var match = GameObject.Find(name);
            if (match != null)
                return match;
        }

        return null;
    }

    private static GameObject FindByNamesUnder(Transform root, params string[] names)
    {
        if (root == null)
            return null;

        var transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (var transformNode in transforms)
        {
            foreach (var name in names)
            {
                if (string.Equals(transformNode.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return transformNode.gameObject;
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
}
