using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires the backpack icon to the go-bag panel overlay during the response
/// phase. Also notifies the mission manager for tutorial tracking.
/// </summary>
[RequireComponent(typeof(Button))]
public class DuringBackpackButton : MonoBehaviour
{
    [SerializeField] private Button targetButton;
    [SerializeField] private DuringGoBagPanel goBagPanel;

    private void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (targetButton != null)
            targetButton.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        if (targetButton != null)
            targetButton.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        if (goBagPanel == null)
            goBagPanel = FindObjectOfType<DuringGoBagPanel>(true);

        if (goBagPanel == null)
        {
            Debug.LogWarning("DuringBackpackButton: Missing go-bag panel reference.");
            return;
        }

        goBagPanel.TogglePanel();

        // Notify mission manager for tutorial tracking
        if (DuringMissionManager.Instance != null)
        {
            DuringMissionManager.Instance.NotifyBackpackOpened();
        }
    }
}
