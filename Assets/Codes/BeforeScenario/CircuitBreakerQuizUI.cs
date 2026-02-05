using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages the Circuit Breaker quiz UI panel with multiple choice questions.
/// Attach this to the CircuitBreakerMissionPanel GameObject.
/// </summary>
public class CircuitBreakerQuizUI : MonoBehaviour
{
    [Header("Cutscene Camera (Option A)")]
    [SerializeField] private Camera cutsceneCamera;
    [SerializeField] private float cutsceneFOV = 35f;

    [Header("Breaker Cutscene")]
    [SerializeField] private Camera mainCamera;               // gameplay camera (your isometric camera)
    [SerializeField] private Transform cameraCutscenePoint;   // where cutscene camera moves to
    [SerializeField] private Transform breakerLookTarget;     // what camera looks at (near lever)
    [SerializeField] private BreakerSwitch breakerSwitch;     // the script on breaker pivot
    [SerializeField] private MonoBehaviour playerMoveScript;  // optional disable
    [SerializeField] private MonoBehaviour playerLookScript;  // optional disable
    [SerializeField] private float camMoveTime = 0.8f;
    [SerializeField] private float camLookTime = 0.2f;
    [SerializeField] private float breakerOffDelay = 0.1f;    // delay before switching off
    [SerializeField] private float holdAfterOff = 0.6f;       // hold shot

    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject questionsContainer; // root object holding question text and answers
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Button confirmButton;

    [Header("Answer Buttons")]
    [SerializeField] private Button answerButtonA;
    [SerializeField] private Button answerButtonB;
    [SerializeField] private Button answerButtonC;

    [Header("Answer Button Texts")]
    [SerializeField] private TMP_Text answerTextA;
    [SerializeField] private TMP_Text answerTextB;
    [SerializeField] private TMP_Text answerTextC;

    [Header("Answer Images (for highlighting)")]
    [SerializeField] private Image answerImageA;
    [SerializeField] private Image answerImageB;
    [SerializeField] private Image answerImageC;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.8f, 0.3f, 1f); // Green
    [SerializeField] private Color incorrectColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Red

    [Header("Question Data")]
    [SerializeField] private string question = "This controls all the power in your house. What is the right move?";
    [SerializeField] private string answerA = "A. Turn off the Main Switch";
    [SerializeField] private string answerB = "B. Leave it on to keep the lights on";
    [SerializeField] private string answerC = "C. Go unplug the TV first";
    [SerializeField] private int correctAnswerIndex = 0; // 0 = A, 1 = B, 2 = C

    [Header("Feedback Messages")]
    [SerializeField] private string correctFeedback = "Exactly! Cutting the main power is the most important step to prevent short circuits or electrocution. Safety first!";
    [SerializeField] private string incorrectFeedbackB = "No, Edward! That's dangerous. You must always turn off the main power source first before doing anything else.";
    [SerializeField] private string incorrectFeedbackC = "No, Edward! That's dangerous. You must always turn off the main power source first before doing anything else.";

    [Header("Feedback UI")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float feedbackDisplayDuration = 3f;

    private int selectedAnswer = -1;
    private bool hasAnswered = false;

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

        // Auto-find the Questions container under panelRoot if not assigned in Inspector
        if (questionsContainer == null && panelRoot != null)
        {
            Transform found = panelRoot.transform.Find("Questions");
            if (found != null) questionsContainer = found.gameObject;
            else
            {
                found = FindChildByNameRecursive(panelRoot.transform, "Questions");
                if (found != null) questionsContainer = found.gameObject;
            }

            Debug.Log($"[CircuitBreakerQuizUI] questionsContainer auto-assigned: {questionsContainer != null}");
        }
        Debug.Log("[CircuitBreakerQuizUI] Panel enabled and listeners set up");
    }

    private void OnDisable()
    {
        if (answerButtonA != null)
            answerButtonA.onClick.RemoveAllListeners();
        if (answerButtonB != null)
            answerButtonB.onClick.RemoveAllListeners();
        if (answerButtonC != null)
            answerButtonC.onClick.RemoveAllListeners();
        if (confirmButton != null)
            confirmButton.onClick.RemoveAllListeners();
    }

    public void ShowQuiz()
    {
        Debug.Log("[CircuitBreakerQuizUI] ShowQuiz called");
        Debug.Log($"[CircuitBreakerQuizUI] panelRoot assigned: {panelRoot != null}");

        if (panelRoot != null)
        {
            Debug.Log($"[CircuitBreakerQuizUI] panelRoot gameObject: {panelRoot.name}");
            Debug.Log($"[CircuitBreakerQuizUI] panelRoot active before: {panelRoot.activeSelf}");
            Debug.Log($"[CircuitBreakerQuizUI] panelRoot activeInHierarchy before: {panelRoot.activeInHierarchy}");

            panelRoot.SetActive(true);

            Debug.Log($"[CircuitBreakerQuizUI] panelRoot active after: {panelRoot.activeSelf}");
            Debug.Log($"[CircuitBreakerQuizUI] panelRoot activeInHierarchy after: {panelRoot.activeInHierarchy}");

            Canvas canvas = panelRoot.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"[CircuitBreakerQuizUI] Canvas found: {canvas.gameObject.name}");
                Debug.Log($"[CircuitBreakerQuizUI] Canvas renderMode: {canvas.renderMode}");
                Canvas.ForceUpdateCanvases();
            }
            else
            {
                Debug.LogWarning("[CircuitBreakerQuizUI] ⚠️ No Canvas parent found! Panel must be child of a Canvas!");
            }
        }
        else
        {
            Debug.LogError("[CircuitBreakerQuizUI] ❌ panelRoot is not assigned!");
            return;
        }

        ResetQuiz();
        SetupQuestion();
        Debug.Log("[CircuitBreakerQuizUI] Quiz setup complete and visible");
    }

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

        // Ensure question UI elements are visible again when resetting
        if (questionsContainer != null) questionsContainer.SetActive(true);
        if (questionText != null) questionText.gameObject.SetActive(true);
        if (answerButtonA != null) answerButtonA.gameObject.SetActive(true);
        if (answerButtonB != null) answerButtonB.gameObject.SetActive(true);
        if (answerButtonC != null) answerButtonC.gameObject.SetActive(true);
        if (confirmButton != null) confirmButton.gameObject.SetActive(true);
        if (answerImageA != null) answerImageA.gameObject.SetActive(true);
        if (answerImageB != null) answerImageB.gameObject.SetActive(true);
        if (answerImageC != null) answerImageC.gameObject.SetActive(true);
    }

    private void SetupQuestion()
    {
        if (questionText != null)
            questionText.text = question;

        if (answerTextA != null)
            answerTextA.text = answerA;
        if (answerTextB != null)
            answerTextB.text = answerB;
        if (answerTextC != null)
            answerTextC.text = answerC;
    }

    private void SelectAnswer(int answerIndex)
    {
        if (hasAnswered) return;

        selectedAnswer = answerIndex;

        if (answerImageA != null) answerImageA.color = normalColor;
        if (answerImageB != null) answerImageB.color = normalColor;
        if (answerImageC != null) answerImageC.color = normalColor;

        switch (answerIndex)
        {
            case 0:
                if (answerImageA != null) answerImageA.color = selectedColor;
                break;
            case 1:
                if (answerImageB != null) answerImageB.color = selectedColor;
                break;
            case 2:
                if (answerImageC != null) answerImageC.color = selectedColor;
                break;
        }

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void OnConfirmClicked()
    {
        if (hasAnswered || selectedAnswer == -1) return;

        hasAnswered = true;

        bool isCorrect = (selectedAnswer == correctAnswerIndex);

        if (isCorrect) HandleCorrectAnswer();
        else HandleIncorrectAnswer();
    }

    private void HandleCorrectAnswer()
    {
        Debug.Log("[CircuitBreakerQuiz] Correct answer selected!");

        // Hide the question UI so the cutscene is visible immediately
        HideQuestionUI();

        ShowFeedback(correctFeedback);

        StartCoroutine(CorrectAnswerWithBreakerCutscene());
    }

    private void HandleIncorrectAnswer()
    {
        switch (selectedAnswer)
        {
            case 0:
                if (answerImageA != null) answerImageA.color = incorrectColor;
                break;
            case 1:
                if (answerImageB != null) answerImageB.color = incorrectColor;
                break;
            case 2:
                if (answerImageC != null) answerImageC.color = incorrectColor;
                break;
        }

        Debug.Log("[CircuitBreakerQuiz] Incorrect answer selected!");

        string feedback = selectedAnswer == 1 ? incorrectFeedbackB : incorrectFeedbackC;
        ShowFeedback(feedback);

        StartCoroutine(AllowRetryAfterDelay());
    }

    private void ShowFeedback(string message)
    {
        if (feedbackPanel != null && feedbackText != null)
        {
            feedbackText.text = message;
            feedbackPanel.SetActive(true);
        }
    }

    private IEnumerator AllowRetryAfterDelay()
    {
        yield return new WaitForSeconds(feedbackDisplayDuration);

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        hasAnswered = false;
        selectedAnswer = -1;

        if (confirmButton != null)
            confirmButton.interactable = false;

        if (answerImageA != null) answerImageA.color = normalColor;
        if (answerImageB != null) answerImageB.color = normalColor;
        if (answerImageC != null) answerImageC.color = normalColor;
    }

    // --- CUTSCENE COROUTINES (OPTION A: SECOND CAMERA) ---

    private IEnumerator CorrectAnswerWithBreakerCutscene()
    {
        Debug.Log(">>> CircuitBreaker Cutscene STARTED");

        // Cache gameplay camera (the one currently rendering)
        Camera gameplayCam = mainCamera != null ? mainCamera : Camera.main;
        if (gameplayCam == null)
        {
            Debug.LogError("No gameplay camera found (mainCamera and Camera.main are null).");
            yield break;
        }

        // Disable follow on gameplay cam so it doesn't fight while we swap
        IsometricFollowCamera gameplayFollow = gameplayCam.GetComponent<IsometricFollowCamera>();
        if (gameplayFollow != null) gameplayFollow.enabled = false;

        // Lock input (optional)
        if (playerMoveScript != null) playerMoveScript.enabled = false;
        if (playerLookScript != null) playerLookScript.enabled = false;

        // Choose which camera to animate: cutscene camera if provided, otherwise gameplay cam
        Camera activeCam = gameplayCam;

        if (cutsceneCamera != null)
        {
            // Turn on cutscene cam, turn off gameplay cam
            cutsceneCamera.gameObject.SetActive(true);
            gameplayCam.gameObject.SetActive(false);

            cutsceneCamera.orthographic = false;
            cutsceneCamera.fieldOfView = cutsceneFOV;

            activeCam = cutsceneCamera;
        }
        else
        {
            // Fallback: if no cutscene cam assigned, force gameplay cam into perspective
            activeCam.orthographic = false;
            activeCam.fieldOfView = cutsceneFOV;
        }

        Debug.Log($"ACTIVE CAM={activeCam.name} cut={(cameraCutscenePoint ? cameraCutscenePoint.name : "NULL")} look={(breakerLookTarget ? breakerLookTarget.name : "NULL")} sw={(breakerSwitch ? breakerSwitch.name : "NULL")} timeScale={Time.timeScale}");

        bool useUnscaled = Time.timeScale == 0f;

        if (activeCam != null && cameraCutscenePoint != null)
        {
            if (useUnscaled)
                yield return MoveSpecificCameraTo_Unscaled(activeCam, cameraCutscenePoint.position, cameraCutscenePoint.rotation, camMoveTime);
            else
                yield return MoveSpecificCameraTo(activeCam, cameraCutscenePoint.position, cameraCutscenePoint.rotation, camMoveTime);
        }

        if (activeCam != null && breakerLookTarget != null)
        {
            if (useUnscaled)
                yield return LookSpecificCameraAt_Unscaled(activeCam, breakerLookTarget.position, camLookTime);
            else
                yield return LookSpecificCameraAt(activeCam, breakerLookTarget.position, camLookTime);
        }

        if (useUnscaled) yield return new WaitForSecondsRealtime(breakerOffDelay);
        else yield return new WaitForSeconds(breakerOffDelay);

        if (breakerSwitch != null) breakerSwitch.TurnOff();

        if (useUnscaled) yield return new WaitForSecondsRealtime(holdAfterOff);
        else yield return new WaitForSeconds(holdAfterOff);

        if (useUnscaled) yield return new WaitForSecondsRealtime(feedbackDisplayDuration);
        else yield return new WaitForSeconds(feedbackDisplayDuration);

        // Hide quiz panel
        if (panelRoot != null)
            panelRoot.SetActive(false);

        // Notify manager (success)
        if (BeforeSceneManager.Instance != null)
            BeforeSceneManager.Instance.OnCircuitBreakerQuizCompleted(true);

        // Swap back to gameplay camera
        if (cutsceneCamera != null)
        {
            cutsceneCamera.gameObject.SetActive(false);
            gameplayCam.gameObject.SetActive(true);
        }

        // Unlock input (optional)
        if (playerMoveScript != null) playerMoveScript.enabled = true;
        if (playerLookScript != null) playerLookScript.enabled = true;

        // Re-enable follow
        if (gameplayFollow != null) gameplayFollow.enabled = true;
    }

    // --- Helpers that move/rotate a SPECIFIC camera (not the mainCamera field) ---

    private IEnumerator MoveSpecificCameraTo(Camera cam, Vector3 targetPos, Quaternion targetRot, float duration)
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

    private IEnumerator LookSpecificCameraAt(Camera cam, Vector3 worldTarget, float duration)
    {
        Quaternion startRot = cam.transform.rotation;

        Vector3 dir = (worldTarget - cam.transform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        cam.transform.rotation = targetRot;
    }

    private IEnumerator MoveSpecificCameraTo_Unscaled(Camera cam, Vector3 targetPos, Quaternion targetRot, float duration)
    {
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            cam.transform.position = Vector3.Lerp(startPos, targetPos, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        cam.transform.position = targetPos;
        cam.transform.rotation = targetRot;
    }

    private IEnumerator LookSpecificCameraAt_Unscaled(Camera cam, Vector3 worldTarget, float duration)
    {
        Quaternion startRot = cam.transform.rotation;

        Vector3 dir = (worldTarget - cam.transform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
            cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        cam.transform.rotation = targetRot;
    }

    // Hide only the question elements (leave feedback panel visible)
    private void HideQuestionUI()
    {
        if (questionsContainer != null) questionsContainer.SetActive(false);

        if (questionText != null) questionText.gameObject.SetActive(false);

        if (answerButtonA != null) answerButtonA.gameObject.SetActive(false);
        if (answerButtonB != null) answerButtonB.gameObject.SetActive(false);
        if (answerButtonC != null) answerButtonC.gameObject.SetActive(false);

        if (confirmButton != null) confirmButton.gameObject.SetActive(false);

        if (answerImageA != null) answerImageA.gameObject.SetActive(false);
        if (answerImageB != null) answerImageB.gameObject.SetActive(false);
        if (answerImageC != null) answerImageC.gameObject.SetActive(false);
    }

    // Search children recursively for a child with the given name (case-sensitive)
    private Transform FindChildByNameRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindChildByNameRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    public void HideQuiz()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}
