using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
using System;
using System.Collections.Generic;

namespace BaHanda.AR
{
    /// <summary>
    /// Manages AR Foundation session lifecycle.
    /// Handles ARSession, ARPlaneManager, and ARRaycastManager.
    /// Reusable across all AR missions.
    /// </summary>
    public class ARSessionController : MonoBehaviour
    {
        public static ARSessionController Instance { get; private set; }

        [Header("AR Components")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private ARPlaneManager arPlaneManager;
        [SerializeField] private ARRaycastManager arRaycastManager;
        [SerializeField] private ARCameraManager arCameraManager;

        [Header("Regular Camera")]
        [SerializeField] private GameObject regularCameraRig;

        [Header("Plane Detection")]
        [SerializeField] private PlaneDetectionMode planeDetectionMode = PlaneDetectionMode.Horizontal;
        [SerializeField] private bool showPlaneVisualization = true;

        // Events
        public event Action OnSessionReady;
        public event Action OnSessionLost;
        public event Action<ARPlane> OnPlaneDetected;

        // State
        private bool isSessionActive = false;
        private bool isSessionReady = false;
        private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

        public bool IsSessionActive => isSessionActive;
        public bool IsSessionReady => isSessionReady;
        public Camera ARCamera => xrOrigin?.Camera;
        public ARPlaneManager PlaneManager => arPlaneManager;
        public ARRaycastManager RaycastManager => arRaycastManager;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Initially disable AR
            SetARActive(false);
        }

        private void OnEnable()
        {
            if (arPlaneManager != null)
            {
                arPlaneManager.planesChanged += OnPlanesChanged;
            }

            ARSession.stateChanged += OnARSessionStateChanged;
        }

        private void OnDisable()
        {
            if (arPlaneManager != null)
            {
                arPlaneManager.planesChanged -= OnPlanesChanged;
            }

            ARSession.stateChanged -= OnARSessionStateChanged;
        }

        /// <summary>
        /// Enable or disable the AR session
        /// </summary>
        public void SetARActive(bool active)
        {
            isSessionActive = active;

            // Toggle AR components
            if (arSession != null)
                arSession.enabled = active;

            if (xrOrigin != null)
                xrOrigin.gameObject.SetActive(active);

            if (arPlaneManager != null)
            {
                arPlaneManager.enabled = active;
                SetPlanesVisible(active && showPlaneVisualization);
            }

            if (arRaycastManager != null)
                arRaycastManager.enabled = active;

            // Toggle regular camera (inverse of AR)
            if (regularCameraRig != null)
                regularCameraRig.SetActive(!active);

            Debug.Log($"ARSessionController: AR session {(active ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Configure plane detection mode
        /// </summary>
        public void SetPlaneDetectionMode(PlaneDetectionMode mode)
        {
            planeDetectionMode = mode;
            if (arPlaneManager != null)
            {
                arPlaneManager.requestedDetectionMode = mode;
            }
        }

        /// <summary>
        /// Show or hide plane visualizations
        /// </summary>
        public void SetPlanesVisible(bool visible)
        {
            if (arPlaneManager == null) return;

            foreach (var plane in arPlaneManager.trackables)
            {
                plane.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Perform AR raycast from screen point
        /// </summary>
        public bool TryRaycast(Vector2 screenPoint, out Pose hitPose, TrackableType trackableTypes = TrackableType.PlaneWithinPolygon)
        {
            hitPose = Pose.identity;

            if (arRaycastManager == null || !isSessionActive)
                return false;

            if (arRaycastManager.Raycast(screenPoint, raycastHits, trackableTypes))
            {
                hitPose = raycastHits[0].pose;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get the first detected horizontal plane (for spawning objects)
        /// </summary>
        public ARPlane GetFirstHorizontalPlane()
        {
            if (arPlaneManager == null) return null;

            foreach (var plane in arPlaneManager.trackables)
            {
                if (plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    return plane;
                }
            }

            return null;
        }

        /// <summary>
        /// Reset AR session
        /// </summary>
        public void ResetSession()
        {
            if (arSession != null)
            {
                arSession.Reset();
            }
        }

        private void OnARSessionStateChanged(ARSessionStateChangedEventArgs args)
        {
            Debug.Log($"ARSessionController: AR state changed to {args.state}");

            switch (args.state)
            {
                case ARSessionState.Ready:
                case ARSessionState.SessionTracking:
                    if (!isSessionReady)
                    {
                        isSessionReady = true;
                        OnSessionReady?.Invoke();
                    }
                    break;

                case ARSessionState.SessionInitializing:
                    isSessionReady = false;
                    break;

                case ARSessionState.None:
                case ARSessionState.Unsupported:
                    isSessionReady = false;
                    OnSessionLost?.Invoke();
                    break;
            }
        }

        private void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            foreach (var plane in args.added)
            {
                Debug.Log($"ARSessionController: New plane detected - {plane.trackableId}");
                OnPlaneDetected?.Invoke(plane);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
