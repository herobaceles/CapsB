using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the After-mission "Ountro" panel with three pages:
///   - Before
///   - During
///   - After
/// Provides Previous / Continue navigation and invokes a callback when
/// the player finishes the last page.
/// </summary>
public class OuntroPanelController : MonoBehaviour
{
    [Header("Pages (ordered task steps)")]
    [SerializeField] private GameObject[] pages;

    [Header("Navigation Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button closeButton;

    [Header("Audio")]
    [SerializeField] private AudioClip missionCompleteSound;
    [SerializeField] private bool disableBgmOnActivate = true;

    private int currentIndex = 0;
    private Action onFinished;

    private void Awake()
    {
        // Ensure panel is hidden by default; it will be shown explicitly
        // via StartSequence when needed.
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Starts the outro sequence from the first (Before) page and shows
    /// this panel. When the player finishes the last page, the supplied
    /// callback is invoked.
    /// </summary>
    public void StartSequence(Action finishedCallback)
    {
        onFinished = finishedCallback;

        // Wire up navigation buttons safely each time.
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(OnPreviousClicked);
            previousButton.onClick.AddListener(OnPreviousClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseClicked);
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        gameObject.SetActive(true);

        // Play mission complete sound and disable background music if configured
        if (disableBgmOnActivate && AudioManager.Instance != null)
        {
            AudioManager.Instance.bgmSource.Stop();
        }

        if (missionCompleteSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(missionCompleteSound);
        }

        SetPage(0);
    }

    private void SetPage(int index)
    {
        if (pages == null || pages.Length == 0)
        {
            // No pages configured; immediately finish to avoid soft lock.
            FinishSequence();
            return;
        }

        if (index < 0)
            index = 0;
        if (index >= pages.Length)
            index = pages.Length - 1;

        currentIndex = index;

        for (int i = 0; i < pages.Length; i++)
        {
            var pageObj = pages[i];
            if (pageObj != null)
                pageObj.SetActive(i == currentIndex);
        }

        // Previous is only available after the first page.
        if (previousButton != null)
            previousButton.gameObject.SetActive(currentIndex > 0);
        
        // Show Continue on intermediate pages; show Start on the final page.
        if (continueButton != null && startButton != null && pages != null && pages.Length > 0)
        {
            bool isFinal = currentIndex >= pages.Length - 1;
            continueButton.gameObject.SetActive(!isFinal);
            startButton.gameObject.SetActive(isFinal);
        }
    }

    private void OnContinueClicked()
    {
        if (pages == null || pages.Length == 0)
        {
            FinishSequence();
            return;
        }

        if (currentIndex < pages.Length - 1)
        {
            SetPage(currentIndex + 1);
        }
        else
        {
            FinishSequence();
        }
    }

    private void OnPreviousClicked()
    {
        if (pages == null || pages.Length == 0)
            return;

        if (currentIndex > 0)
        {
            SetPage(currentIndex - 1);
        }
    }

    private void FinishSequence()
    {
        // Hide the panel and notify listeners that the sequence is done.
        gameObject.SetActive(false);

        var callback = onFinished;
        onFinished = null;
        callback?.Invoke();
    }

    private void OnStartClicked()
    {
        FinishSequence();
    }

    private void OnCloseClicked()
    {
        // Hide panel and cancel any pending finished callback without invoking it.
        gameObject.SetActive(false);
        onFinished = null;

        var menu = FindObjectOfType<MainMenuManager>();
        if (menu != null)
        {
            menu.ShowMainMenu();
        }
        else
        {
            Debug.LogWarning("OuntroPanelController: MainMenuManager not found to restore main menu.");
        }
    }
}
