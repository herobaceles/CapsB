using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the AR session and sub-task dispatch for the After phase.
///
/// Receives a MissionMode from AfterMissionManager, activates AR, and delegates to
/// the appropriate recovery sub-system (HiddenDangerSpawner for hazard/cleanup modes,
/// or no spawner for DamageAssessment which uses MissionData.startQuiz).
///
/// Single completion path: DisableAR() always calls
/// AfterMissionManager.Instance.NotifyARTaskComplete() so task progression is consistent.
/// </summary>
public class AfterRecoveryARController : MonoBehaviour
{
    public static AfterRecoveryARController Instance { get; private set; }

    // -----------------------------------------------------------------------
    // Inspector fields
    // -----------------------------------------------------------------------

    [Header("Sub-task Handlers")]
    [SerializeField] private HiddenDangerSpawner hiddenDangerSpawner;

    [Header("AR UI")]
    [Tooltip("Root GameObject containing all After-phase AR UI. Shown while AR is active.")]
    [SerializeField] private GameObject arUIRoot;

    [Header("Camera")]
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private bool disableGameplayCameraInAR = true;

    // -----------------------------------------------------------------------
    // Private state
    // -----------------------------------------------------------------------

    private bool arActive;

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // -----------------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------------

    /// <summary>
    /// Activates AR and starts the recovery sub-task for <paramref name="mode"/>.
    /// Called by AfterMissionManager when a trigger fires for an AR-bound task.
    /// </summary>
    public void EnableARRecovery(MissionMode mode)
    {
        if (arActive)
        {
            Debug.LogWarning("AfterRecoveryARController: EnableARRecovery called while AR is already active.");
            return;
        }

        arActive = true;
        Debug.Log($"AfterRecoveryARController: Starting AR recovery — mode: {mode}");

        if (ARRuntimeContext.Instance != null)
            ARRuntimeContext.Instance.SetARActive(true);

        if (disableGameplayCameraInAR)
            StartCoroutine(DisableGameplayCameraWhenARReady());

        if (arUIRoot != null)
            arUIRoot.SetActive(true);

        DispatchByMode(mode);
    }

    /// <summary>
    /// Deactivates AR, restores gameplay camera, hides AR UI, and notifies
    /// AfterMissionManager that the current task is complete.
    /// Called by HiddenDangerSpawner when all items are cleared, or directly
    /// for modes that need no spawner.
    /// </summary>
    public void DisableAR()
    {
        if (!arActive) return;

        arActive = false;
        Debug.Log("AfterRecoveryARController: Disabling AR, returning to gameplay.");

        if (hiddenDangerSpawner != null)
            hiddenDangerSpawner.StopSpawning();

        if (ARRuntimeContext.Instance != null)
            ARRuntimeContext.Instance.SetARActive(false);

        if (disableGameplayCameraInAR && gameplayCamera != null)
            gameplayCamera.gameObject.SetActive(true);

        if (arUIRoot != null)
            arUIRoot.SetActive(false);

        AfterMissionManager.Instance?.NotifyARTaskComplete();
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private void DispatchByMode(MissionMode mode)
    {
        switch (mode)
        {
            case MissionMode.CleanupGear:
            case MissionMode.DisinfectHouse:
            case MissionMode.HazardScan:
                StartHiddenDangerSession();
                break;

            case MissionMode.DamageAssessment:
                // Structural assessment is handled by MissionData.startQuiz.
                // No spawning needed; the quiz gate advances the task automatically.
                Debug.Log("AfterRecoveryARController: DamageAssessment — delegated to MissionData start quiz.");
                break;

            default:
                Debug.LogWarning($"AfterRecoveryARController: Unhandled MissionMode '{mode}'. Completing AR task immediately.");
                DisableAR();
                break;
        }
    }

    private void StartHiddenDangerSession()
    {
        if (hiddenDangerSpawner == null)
        {
            Debug.LogError("AfterRecoveryARController: HiddenDangerSpawner not assigned. Cannot start spawning.");
            DisableAR();
            return;
        }

        hiddenDangerSpawner.StartSpawning();
    }

    private IEnumerator DisableGameplayCameraWhenARReady()
    {
        if (gameplayCamera == null)
            yield break;

        float timeout = 3.0f;
        while (timeout > 0f)
        {
            Camera arCamera = ARRuntimeContext.Instance != null ? ARRuntimeContext.Instance.ResolveARCamera() : null;
            if (arCamera != null && arCamera.gameObject.activeInHierarchy && arCamera.enabled)
            {
                gameplayCamera.gameObject.SetActive(false);
                yield break;
            }
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning("AfterRecoveryARController: AR camera not ready in time. Keeping gameplay camera active.");
    }
}
