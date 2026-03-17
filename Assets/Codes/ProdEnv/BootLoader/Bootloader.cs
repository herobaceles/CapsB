using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Bootloader : MonoBehaviour
{
    [Header("Loading Panel")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressText; // Accepts TextMeshPro or TextMeshProUGUI

    private IEnumerator Start()
    {
        // Ensure camera exists to remove "Display 1 - No cameras rendering"
        if (Camera.main == null)
        {
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }

        // Ensure GameManager exists
        if (GameManager.Instance == null)
        {
            var gmObj = new GameObject("GameManager");
            gmObj.AddComponent<GameManager>();
        }

        // Ensure AudioManager exists
        if (AudioManager.Instance == null)
        {
            var amObj = new GameObject("AudioManager");
            amObj.AddComponent<AudioManager>();
        }

        // Ensure PlayerData exists
        if (PlayerData.Instance == null)
        {
            var pdObj = new GameObject("PlayerData");
            pdObj.AddComponent<PlayerData>();
        }

        // Note: ProdDialogueManager should be set up in the MainMenuProd scene with UI references
        // Do NOT create it here - let the scene's instance with proper UI assignments be the singleton

        // Initialize core systems
        GameManager.Instance.Initialize();
        AudioManager.Instance.Initialize();

        // Show loading panel
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        // Optional: wait 1 second for splash/logo
        yield return new WaitForSeconds(1f);

        // Load Main Menu asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainMenuProd");
        asyncLoad.allowSceneActivation = false;

        // Update progress while loading
        while (!asyncLoad.isDone)
        {
            // Progress goes from 0 to 0.9 while loading, then jumps to 1 when activated
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // Update progress bar
            if (progressBar != null)
                progressBar.value = progress;

            // Update progress text
            if (progressText != null)
                progressText.text = $"{(progress * 100f):0}%";

            // When loading is complete (progress reaches 0.9), activate the scene
            if (asyncLoad.progress >= 0.9f)
            {
                // Show 100% before transitioning
                if (progressBar != null)
                    progressBar.value = 1f;
                if (progressText != null)
                    progressText.text = "100%";

                yield return new WaitForSeconds(0.5f);
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
