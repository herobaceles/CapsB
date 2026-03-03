using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires a UI Button to the During mission map toggle. Attach this to the
/// backpack icon so tapping it restores the top-down map.
/// </summary>
[RequireComponent(typeof(Button))]
public class DuringBackpackButton : MonoBehaviour
{
    [SerializeField] private Button targetButton;
    [SerializeField] private DuringMissionManager missionManager;

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
        if (missionManager == null)
            missionManager = DuringMissionManager.Instance;

        if (missionManager != null)
            missionManager.ShowMapFromBackpack();
        else
            Debug.LogWarning("DuringBackpackButton: Missing DuringMissionManager reference.");
    }
}
