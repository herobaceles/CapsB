using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AppliancesQuizUI : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Question")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private string question = "Now, look at those appliances on the floor. The floodwater will destroy them and they could still be a hazard. What should you do?";

    [Header("Answer Buttons")]
    [SerializeField] private Button answerButtonA;
    [SerializeField] private Button answerButtonB;
    [SerializeField] private Button answerButtonC;

    [Header("Answer Texts")]
    [SerializeField] private TMP_Text answerTextA;
    [SerializeField] private TMP_Text answerTextB;
    [SerializeField] private TMP_Text answerTextC;

    [Header("Questions Panel")]
    [SerializeField] private GameObject questionsPanel;

    [SerializeField] private string answerA = "A. Unplug them and move them to a high shelf or the second floor.";
    [SerializeField] private string answerB = "B. Cover them with a plastic bag.";
    [SerializeField] private string answerC = "C. Do nothing, the power is off.";

    [Header("Answer Images")]
    [SerializeField] private Image answerImageA;
    [SerializeField] private Image answerImageB;
    [SerializeField] private Image answerImageC;

    [Header("Confirm")]
    [SerializeField] private Button confirmButton;

    [Header("Feedback")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private string correctFeedback = "Correct!";
    [SerializeField] private string wrongFeedback = "Wrong answer.";
    [SerializeField] private float feedbackDisplayDuration = 1.5f;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    [Header("Correct Answer Index")]
    [SerializeField] private int correctAnswerIndex = 0;

    [Header("Appliance Cutscene")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera cutsceneCamera;
    [SerializeField] private Transform cameraCutscenePoint;
    [SerializeField] private Transform applianceLookTarget;
    [SerializeField] private float cutsceneFOV = 35f;
    [SerializeField] private float camMoveTime = 0.8f;
    [SerializeField] private float camLookTime = 0.2f;
    [SerializeField] private float holdAfterMove = 1.0f;

    [Header("Appliance Animation (Optional)")]
    [SerializeField] private Animator appliancesAnimator;
    [SerializeField] private string moveAnimationTrigger = "MoveUp";

    [Header("Player Control (Optional)")]
    [SerializeField] private MonoBehaviour playerMoveScript;
    [SerializeField] private MonoBehaviour playerLookScript;
    [SerializeField] private GameObject playerRoot;
    [SerializeField] private bool hidePlayerUsingSetActive = true;
    [SerializeField] private string playerTag = "Player";

    [Header("Appliance Movement (Scripted)")]
    [SerializeField] private Transform applianceToMove;
    [SerializeField] private Transform applianceDestination;
    [SerializeField] private float applianceMoveDuration = 1.0f;
    [SerializeField] private bool verboseMoveLogs = false;
    [SerializeField] private bool leaveApplianceUnparentedAfterMove = false;
    [SerializeField] private bool cameraFollowDuringMove = true;
    [SerializeField] private Vector3 cameraFollowOffset = new Vector3(0f, 1.5f, -2f);

    private int selectedAnswer = -1;
    private bool hasAnswered = false;

    // =========================
    // Unity Lifecycle
    // =========================

    private void OnEnable()
    {
        if (answerButtonA != null)
            answerButtonA.onClick.AddListener(() => SelectAnswer(0));

        if (answerButtonB != null)
            answerButtonB.onClick.AddListener(() => SelectAnswer(1));

        if (answerButtonC != null)
            answerButtonC.onClick.AddListener(() => SelectAnswer(2));

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            confirmButton.interactable = false;
        }

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        ResetQuiz();
    }

    private void OnDisable()
    {
        if (answerButtonA != null) answerButtonA.onClick.RemoveAllListeners();
        if (answerButtonB != null) answerButtonB.onClick.RemoveAllListeners();
        if (answerButtonC != null) answerButtonC.onClick.RemoveAllListeners();
        if (confirmButton != null) confirmButton.onClick.RemoveAllListeners();
    }

    // =========================
    // Quiz Logic
    // =========================

    private void ResetQuiz()
    {
        selectedAnswer = -1;
        hasAnswered = false;

        if (confirmButton != null)
            confirmButton.interactable = false;

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        if (answerImageA != null) answerImageA.color = normalColor;
        if (answerImageB != null) answerImageB.color = normalColor;
        if (answerImageC != null) answerImageC.color = normalColor;
    }

    private void SelectAnswer(int index)
    {
        if (hasAnswered)
            return;

        selectedAnswer = index;

        if (answerImageA != null) answerImageA.color = normalColor;
        if (answerImageB != null) answerImageB.color = normalColor;
        if (answerImageC != null) answerImageC.color = normalColor;

        if (index == 0 && answerImageA != null)
            answerImageA.color = selectedColor;

        if (index == 1 && answerImageB != null)
            answerImageB.color = selectedColor;

        if (index == 2 && answerImageC != null)
            answerImageC.color = selectedColor;

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void OnConfirmClicked()
    {
        if (hasAnswered || selectedAnswer < 0)
            return;

        hasAnswered = true;

        if (selectedAnswer == correctAnswerIndex)
            HandleCorrectAnswer();
        else
            HandleWrongAnswer();
    }

    private void HandleWrongAnswer()
    {
        ShowFeedback(wrongFeedback);
        Invoke(nameof(ResetQuiz), feedbackDisplayDuration);
    }

    private void HandleCorrectAnswer()
    {
        HideQuestionUI();
        ShowFeedback(correctFeedback);
        StartCoroutine(CorrectAnswerWithApplianceCutscene());
    }

    // =========================
    // UI Helpers
    // =========================

    private void ShowFeedback(string msg)
    {
        if (feedbackText != null)
            feedbackText.text = msg;

        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);
    }

    private void HideQuestionUI()
    {
        if (questionText != null) questionText.gameObject.SetActive(false);

        if (answerButtonA != null) answerButtonA.gameObject.SetActive(false);
        if (answerButtonB != null) answerButtonB.gameObject.SetActive(false);
        if (answerButtonC != null) answerButtonC.gameObject.SetActive(false);

        if (confirmButton != null) confirmButton.gameObject.SetActive(false);

        if (answerImageA != null) answerImageA.gameObject.SetActive(false);
        if (answerImageB != null) answerImageB.gameObject.SetActive(false);
        if (answerImageC != null) answerImageC.gameObject.SetActive(false);
    }

    // Make the quiz visible and reset UI state so other managers can call it.
    public void ShowQuiz()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (questionText != null)
        {
            questionText.gameObject.SetActive(true);
            questionText.text = question;
        }

        if (answerButtonA != null) answerButtonA.gameObject.SetActive(true);
        if (answerButtonB != null) answerButtonB.gameObject.SetActive(true);
        if (answerButtonC != null) answerButtonC.gameObject.SetActive(true);

        if (answerTextA != null) answerTextA.text = answerA;
        if (answerTextB != null) answerTextB.text = answerB;
        if (answerTextC != null) answerTextC.text = answerC;

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
            confirmButton.interactable = false;
        }

        if (answerImageA != null) answerImageA.gameObject.SetActive(true);
        if (answerImageB != null) answerImageB.gameObject.SetActive(true);
        if (answerImageC != null) answerImageC.gameObject.SetActive(true);

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        ResetQuiz();
    }

    // =========================
    // Cutscene Logic
    // =========================

    private IEnumerator CorrectAnswerWithApplianceCutscene()
    {
        Camera gameplayCam = mainCamera != null ? mainCamera : Camera.main;
        if (gameplayCam == null)
            yield break;

        if (playerMoveScript != null) playerMoveScript.enabled = false;
        if (playerLookScript != null) playerLookScript.enabled = false;

        // Hide player visuals during cutscene. The player may spawn at runtime, so try to locate it if `playerRoot` isn't set.
        GameObject runtimePlayer = playerRoot;
        if (runtimePlayer == null)
        {
            if (!string.IsNullOrEmpty(playerTag))
            {
                try { runtimePlayer = GameObject.FindWithTag(playerTag); } catch { runtimePlayer = null; }
            }

            if (runtimePlayer == null)
                runtimePlayer = GameObject.Find("Player");
        }

        bool prevPlayerActive = false;
        Renderer[] playerRenderers = null;
        bool[] prevRendererStates = null;
        if (runtimePlayer != null)
        {
            if (hidePlayerUsingSetActive)
            {
                prevPlayerActive = runtimePlayer.activeSelf;
                runtimePlayer.SetActive(false);
            }
            else
            {
                playerRenderers = runtimePlayer.GetComponentsInChildren<Renderer>(true);
                prevRendererStates = new bool[playerRenderers.Length];
                for (int i = 0; i < playerRenderers.Length; i++)
                {
                    prevRendererStates[i] = playerRenderers[i].enabled;
                    playerRenderers[i].enabled = false;
                }
            }
        }

        Camera activeCam = gameplayCam;

        // Hide the questions panel during the cutscene (store previous state to restore later)
        bool prevQuestionsPanelActive = false;
        if (questionsPanel != null)
        {
            prevQuestionsPanelActive = questionsPanel.activeSelf;
            questionsPanel.SetActive(false);
        }

        if (cutsceneCamera != null)
        {
            cutsceneCamera.gameObject.SetActive(true);
            gameplayCam.gameObject.SetActive(false);

            cutsceneCamera.orthographic = false;
            cutsceneCamera.fieldOfView = cutsceneFOV;
            activeCam = cutsceneCamera;
        }
        else
        {
            gameplayCam.orthographic = false;
            gameplayCam.fieldOfView = cutsceneFOV;
        }

        if (cameraCutscenePoint != null)
            yield return MoveCameraTo(activeCam,
                cameraCutscenePoint.position,
                cameraCutscenePoint.rotation,
                camMoveTime);

        // Cache camera original transform so we can restore later if needed
        Vector3 camOriginalPos = activeCam.transform.position;
        Quaternion camOriginalRot = activeCam.transform.rotation;

        if (applianceLookTarget != null)
            yield return LookCameraAt(activeCam,
                applianceLookTarget.position,
                camLookTime);

        // If a scripted destination is provided, move the appliance there.
        if (applianceToMove != null && applianceDestination != null)
        {
            // optionally start camera follow concurrently
            if (cameraFollowDuringMove && activeCam != null)
            {
                StartCoroutine(FollowCameraDuringMove(activeCam, applianceToMove, cameraFollowOffset, applianceMoveDuration));
            }

            yield return MoveApplianceToDestination(applianceToMove, applianceDestination.position, applianceMoveDuration);
        }

        // Optionally trigger animator as well (keeps backward compatibility)
        if (appliancesAnimator != null)
            appliancesAnimator.SetTrigger(moveAnimationTrigger);

        yield return new WaitForSeconds(holdAfterMove);
        yield return new WaitForSeconds(feedbackDisplayDuration);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (BeforeSceneManager.Instance != null)
            BeforeSceneManager.Instance.OnAppliancesQuizCompleted(true);

        if (cutsceneCamera != null)
        {
            cutsceneCamera.gameObject.SetActive(false);
            gameplayCam.gameObject.SetActive(true);
        }

        // If we used the gameplay camera (no separate cutscene camera), restore its original transform
        if (cutsceneCamera == null && gameplayCam != null)
        {
            gameplayCam.transform.position = camOriginalPos;
            gameplayCam.transform.rotation = camOriginalRot;
        }

        // Restore player visuals
        if (runtimePlayer != null)
        {
            if (hidePlayerUsingSetActive)
            {
                runtimePlayer.SetActive(prevPlayerActive);
            }
            else if (playerRenderers != null && prevRendererStates != null)
            {
                for (int i = 0; i < playerRenderers.Length; i++)
                {
                    if (playerRenderers[i] != null)
                        playerRenderers[i].enabled = prevRendererStates[i];
                }
            }
        }

        // Restore questions panel visibility
        if (questionsPanel != null)
        {
            questionsPanel.SetActive(prevQuestionsPanelActive);
        }

        if (playerMoveScript != null) playerMoveScript.enabled = true;
        if (playerLookScript != null) playerLookScript.enabled = true;
    }

    // =========================
    // Camera Helpers
    // =========================

    private IEnumerator MoveCameraTo(Camera cam, Vector3 targetPos, Quaternion targetRot, float duration)
    {
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        cam.transform.position = targetPos;
        cam.transform.rotation = targetRot;
    }

    private IEnumerator LookCameraAt(Camera cam, Vector3 worldTarget, float duration)
    {
        Quaternion startRot = cam.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(
            (worldTarget - cam.transform.position).normalized,
            Vector3.up);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        cam.transform.rotation = targetRot;
    }

    private IEnumerator MoveApplianceToDestination(Transform appliance, Vector3 targetPosition, float duration)
    {
        if (appliance == null)
            yield break;

        Debug.Log($"[AppliancesQuizUI] Starting scripted move of '{appliance.name}' to {targetPosition} over {duration}s");

        Vector3 startPos = appliance.position;
        Quaternion startRot = appliance.rotation;
        float elapsed = 0f;

        // Handle Rigidbody: prefer using MovePosition if available, but do NOT change isKinematic
        Rigidbody rb = appliance.GetComponent<Rigidbody>();
        bool hadRigidbody = rb != null;

        // Disable any Animator attached to the appliance during the scripted move
        Animator localAnimator = appliance.GetComponent<Animator>();
        bool localAnimatorWasEnabled = false;
        if (localAnimator != null)
        {
            localAnimatorWasEnabled = localAnimator.enabled;
            localAnimator.enabled = false;
        }

        // Also consider the shared appliancesAnimator (if it targets the same object)
        bool sharedAnimatorWasEnabled = false;
        if (appliancesAnimator != null && appliancesAnimator.gameObject == appliance.gameObject)
        {
            sharedAnimatorWasEnabled = appliancesAnimator.enabled;
            appliancesAnimator.enabled = false;
        }

        // Temporarily detach from parent to avoid parent-driven constraints
        Transform originalParent = appliance.parent;
        if (originalParent != null)
            appliance.SetParent(null, true);

        // Temporarily disable Static flag if set (static objects may not reflect runtime transform changes)
        bool prevStatic = appliance.gameObject.isStatic;
        if (prevStatic)
            appliance.gameObject.isStatic = false;

        if (hadRigidbody && rb != null)
        {
            // Some other systems may be setting velocities on this Rigidbody.
            // To ensure the scripted move takes effect, temporarily make the body kinematic
            // and interpolate the transform directly.
            bool prevKinematic = rb.isKinematic;
            rb.isKinematic = true;

            elapsed = 0f;
            float logTimer = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                logTimer += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
                Vector3 nextPos = Vector3.Lerp(startPos, targetPosition, t);
                appliance.position = nextPos;

                if (verboseMoveLogs && logTimer >= 0.1f)
                {
                    Debug.Log($"[AppliancesQuizUI] Moving '{appliance.name}' pos={appliance.position} target={targetPosition} t={t:F2}");
                    logTimer = 0f;
                }

                yield return null;
            }

            appliance.position = targetPosition;

            // Restore previous kinematic state
            rb.isKinematic = prevKinematic;
        }
        else
        {
            // Fallback to transform-based interpolation if no Rigidbody
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
                appliance.position = Vector3.Lerp(startPos, targetPosition, t);
                yield return null;
            }

            appliance.position = targetPosition;
        }

        // Restore parent (unless user requested to keep it unparented for debugging)
        if (!leaveApplianceUnparentedAfterMove)
        {
            if (originalParent != null)
                appliance.SetParent(originalParent, true);
        }

        // Restore static flag
        if (prevStatic)
            appliance.gameObject.isStatic = prevStatic;

        // Restore Animator and Rigidbody states
        if (localAnimator != null)
            localAnimator.enabled = localAnimatorWasEnabled;

        if (appliancesAnimator != null && appliancesAnimator.gameObject == appliance.gameObject)
            appliancesAnimator.enabled = sharedAnimatorWasEnabled;

        // No need to restore kinematic here because we never changed it in this implementation.

        Debug.Log($"[AppliancesQuizUI] Finished scripted move of '{appliance.name}'");
    }

    private IEnumerator FollowCameraDuringMove(Camera cam, Transform appliance, Vector3 offset, float duration)
    {
        if (cam == null || appliance == null)
            yield break;

        Vector3 startPos = cam.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
            Vector3 targetPos = appliance.position + offset;
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            cam.transform.LookAt(appliance.position);
            yield return null;
        }

        cam.transform.position = appliance.position + offset;
        cam.transform.LookAt(appliance.position);
    }
}
