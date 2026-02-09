using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BaHanda.AR
{
    /// <summary>
    /// Abstract base class for all AR mission handlers.
    /// Provides common functionality for AR missions.
    /// Inherit and implement mission-specific logic.
    /// </summary>
    public abstract class ARMissionBase : MonoBehaviour, IARMissionHandler
    {
        [Header("AR Session")]
        [SerializeField] protected ARSessionController arSessionController;

        [Header("UI")]
        [SerializeField] protected GameObject arUIRoot;
        [SerializeField] protected GameObject instructionPanel;
        [SerializeField] protected TMPro.TMP_Text instructionText;
        [SerializeField] protected ARObjectiveChecklist objectiveChecklist;

        [Header("Spawning")]
        [SerializeField] protected float minSpawnDistance = 0.5f;
        [SerializeField] protected float maxSpawnDistance = 2f;
        [SerializeField] protected float spawnHeightOffset = 0.1f;

        [Header("Audio")]
        [SerializeField] protected AudioClip arStartSound;
        [SerializeField] protected AudioClip arCompleteSound;
        [SerializeField] protected AudioClip objectiveCompleteSound;

        // State
        protected bool isActive = false;
        protected bool isCompleted = false;
        protected bool isPaused = false;
        protected List<GameObject> spawnedObjects = new List<GameObject>();
        protected ARPlane currentPlane;

        // Interface implementation
        public bool IsActive => isActive;
        public bool IsCompleted => isCompleted;
        public abstract float Progress { get; }

        // Events
        public event Action OnARStarted;
        public event Action OnAREnded;
        public event Action OnARCompleted;
        public event Action<float> OnProgressChanged;

        protected virtual void Awake()
        {
            // Auto-find AR session controller if not assigned
            if (arSessionController == null)
            {
                arSessionController = FindObjectOfType<ARSessionController>();
            }
        }

        protected virtual void Start()
        {
            // Initially hide AR UI
            if (arUIRoot != null)
                arUIRoot.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            CleanupSpawnedObjects();
        }

        #region IARMissionHandler Implementation

        public virtual void StartAR()
        {
            if (isActive)
            {
                Debug.LogWarning($"{GetType().Name}: AR already active");
                return;
            }

            Debug.Log($"{GetType().Name}: Starting AR mission");

            isActive = true;
            isCompleted = false;

            // Enable AR session
            if (arSessionController != null)
            {
                arSessionController.SetARActive(true);
                arSessionController.OnPlaneDetected += OnPlaneDetected;
            }

            // Show AR UI
            if (arUIRoot != null)
                arUIRoot.SetActive(true);

            // Play start sound
            PlaySound(arStartSound);

            // Show initial instruction
            ShowInstruction(GetStartInstruction());

            // Start waiting for plane detection
            StartCoroutine(WaitForPlaneAndSpawn());

            OnARStarted?.Invoke();
        }

        public virtual void EndAR()
        {
            if (!isActive)
                return;

            Debug.Log($"{GetType().Name}: Ending AR mission");

            isActive = false;

            // Unsubscribe from events
            if (arSessionController != null)
            {
                arSessionController.OnPlaneDetected -= OnPlaneDetected;
                arSessionController.SetARActive(false);
            }

            // Hide AR UI
            if (arUIRoot != null)
                arUIRoot.SetActive(false);

            // Cleanup spawned objects
            CleanupSpawnedObjects();

            OnAREnded?.Invoke();
        }

        public virtual void ResetAR()
        {
            Debug.Log($"{GetType().Name}: Resetting AR mission");

            // Cleanup current state
            CleanupSpawnedObjects();
            ResetObjectives();

            isCompleted = false;
            currentPlane = null;

            // Restart if was active
            if (isActive)
            {
                StartCoroutine(WaitForPlaneAndSpawn());
            }

            NotifyProgressChanged();
        }

        public virtual void PauseAR()
        {
            isPaused = true;
            // Disable interactions but keep AR visible
        }

        public virtual void ResumeAR()
        {
            isPaused = false;
        }

        #endregion

        #region Plane Detection & Spawning

        protected virtual IEnumerator WaitForPlaneAndSpawn()
        {
            ShowInstruction("Point your camera at the floor to detect a surface...");

            // Wait for plane detection
            float timeout = 30f;
            float elapsed = 0f;

            while (currentPlane == null && elapsed < timeout)
            {
                elapsed += Time.deltaTime;

                if (arSessionController != null)
                {
                    currentPlane = arSessionController.GetFirstHorizontalPlane();
                }

                yield return null;
            }

            if (currentPlane != null)
            {
                Debug.Log($"{GetType().Name}: Plane detected, spawning objects");
                ShowInstruction(GetSpawningInstruction());
                yield return StartCoroutine(SpawnObjectsOnPlane(currentPlane));
                ShowInstruction(GetMainInstruction());
            }
            else
            {
                Debug.LogWarning($"{GetType().Name}: Plane detection timeout");
                ShowInstruction("Could not detect floor. Try moving your device.");
            }
        }

        protected virtual void OnPlaneDetected(ARPlane plane)
        {
            if (currentPlane == null && plane.alignment == PlaneAlignment.HorizontalUp)
            {
                currentPlane = plane;
            }
        }

        /// <summary>
        /// Override this to spawn mission-specific objects
        /// </summary>
        protected abstract IEnumerator SpawnObjectsOnPlane(ARPlane plane);

        /// <summary>
        /// Get spawn position on plane
        /// </summary>
        protected Vector3 GetSpawnPositionOnPlane(ARPlane plane, float radiusOffset = 0f)
        {
            Camera arCam = arSessionController?.ARCamera ?? Camera.main;
            if (arCam == null) return plane.center;

            // Calculate position in front of camera on the plane
            Vector3 forward = arCam.transform.forward;
            forward.y = 0;
            forward.Normalize();

            float distance = UnityEngine.Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * radiusOffset;

            Vector3 position = arCam.transform.position + forward * distance;
            position.y = plane.center.y + spawnHeightOffset;
            position.x += randomOffset.x;
            position.z += randomOffset.y;

            return position;
        }

        #endregion

        #region Objective Management

        protected abstract void ResetObjectives();

        protected virtual void CompleteAR()
        {
            if (isCompleted) return;

            Debug.Log($"{GetType().Name}: AR mission completed!");

            isCompleted = true;
            PlaySound(arCompleteSound);
            ShowInstruction(GetCompleteInstruction());

            OnARCompleted?.Invoke();
        }

        protected void NotifyProgressChanged()
        {
            OnProgressChanged?.Invoke(Progress);
        }

        #endregion

        #region UI Helpers

        protected void ShowInstruction(string text)
        {
            if (instructionText != null)
                instructionText.text = text;

            if (instructionPanel != null)
                instructionPanel.SetActive(!string.IsNullOrEmpty(text));
        }

        protected void HideInstruction()
        {
            if (instructionPanel != null)
                instructionPanel.SetActive(false);
        }

        #endregion

        #region Audio

        protected void PlaySound(AudioClip clip)
        {
            if (clip == null) return;

            // Use AudioManager if available
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(clip);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
            }
        }

        #endregion

        #region Cleanup

        protected void CleanupSpawnedObjects()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            spawnedObjects.Clear();
        }

        protected void RegisterSpawnedObject(GameObject obj)
        {
            if (obj != null)
                spawnedObjects.Add(obj);
        }

        #endregion

        #region Abstract Instructions (Override in subclass)

        protected abstract string GetStartInstruction();
        protected abstract string GetSpawningInstruction();
        protected abstract string GetMainInstruction();
        protected abstract string GetCompleteInstruction();

        #endregion
    }
}
