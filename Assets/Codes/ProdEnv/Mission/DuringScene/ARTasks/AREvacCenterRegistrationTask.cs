using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI-based task for registering at the evacuation center.
/// When the player enters the evac-center trigger, this panel appears.
/// The player must confirm key checklist items before continuing.
/// </summary>
public class AREvacCenterRegistrationTask : ARTaskBase
{
    [Header("Registration Checklist")]
    [Tooltip("All of these toggles must be ON to complete the task.")]
    [SerializeField] private List<Toggle> requiredToggles = new List<Toggle>();

    [Header("Required Text Fields")]
    [Tooltip("All of these input fields must have non-empty text to complete the task.")]
    [SerializeField] private List<TMP_InputField> requiredFields = new List<TMP_InputField>();

    [Header("Submit Button")]
    [SerializeField] private Button submitButton;

    [Header("Feedback UI")]
    [SerializeField] private TMP_Text statusText;
    [TextArea(2, 3)]
    [SerializeField] private string incompleteMessage = "Please review and complete all registration details.";
    [TextArea(2, 3)]
    [SerializeField] private string completeMessage = "Registration complete. You and your family are now accounted for at the evacuation center.";

    private bool listenersHooked;

    protected override void OnTaskShow()
    {
        // Reset all toggles (used as visual indicators, not player input)
        foreach (var toggle in requiredToggles)
        {
            if (toggle != null)
            {
                toggle.isOn = false;
                toggle.interactable = false; // toggles are auto-updated based on fields
            }
        }

        // Clear all required text fields
        foreach (var field in requiredFields)
        {
            if (field != null)
            {
                field.text = string.Empty;
            }
        }

        if (statusText != null)
        {
            statusText.text = incompleteMessage;
        }

        if (submitButton != null)
        {
            submitButton.interactable = false;
        }

        HookListeners();
        UpdateFormState();

        Debug.Log($"AREvacCenterRegistrationTask [{taskId}]: Shown and reset.");
    }

    protected override void OnTaskHide()
    {
        UnhookListeners();
        Debug.Log($"AREvacCenterRegistrationTask [{taskId}]: Hidden.");
    }

    protected override bool ValidateCompletion()
    {
        // All toggles (if any) must be ON
        if (requiredToggles != null && requiredToggles.Count > 0)
        {
            foreach (var toggle in requiredToggles)
            {
                if (toggle == null)
                    continue;

                if (!toggle.isOn)
                    return false;
            }
        }

        // All fields (if any) must have non-empty, non-whitespace text
        if (requiredFields != null && requiredFields.Count > 0)
        {
            foreach (var field in requiredFields)
            {
                if (field == null)
                    continue;

                if (string.IsNullOrWhiteSpace(field.text))
                    return false;
            }
        }

        return true;
    }

    private void HookListeners()
    {
        if (listenersHooked)
            return;

        foreach (var toggle in requiredToggles)
        {
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(OnToggleValueChanged);
            }
        }

        foreach (var field in requiredFields)
        {
            if (field != null)
            {
                field.onValueChanged.AddListener(OnFieldValueChanged);
            }
        }

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(OnSubmitClicked);
        }

        listenersHooked = true;
    }

    private void UnhookListeners()
    {
        if (!listenersHooked)
            return;

        foreach (var toggle in requiredToggles)
        {
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }
        }

        foreach (var field in requiredFields)
        {
            if (field != null)
            {
                field.onValueChanged.RemoveListener(OnFieldValueChanged);
            }
        }

        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(OnSubmitClicked);
        }

        listenersHooked = false;
    }

    private void OnToggleValueChanged(bool _)
    {
        UpdateFormState();
    }

    private void OnFieldValueChanged(string _)
    {
        UpdateFormState();
    }

    private void UpdateFormState()
    {
        // Auto-update toggles based on corresponding input fields (by index)
        int pairCount = Mathf.Min(requiredToggles != null ? requiredToggles.Count : 0,
                                   requiredFields != null ? requiredFields.Count : 0);

        for (int i = 0; i < pairCount; i++)
        {
            var field = requiredFields[i];
            var toggle = requiredToggles[i];

            if (toggle == null)
                continue;

            bool filled = field != null && !string.IsNullOrWhiteSpace(field.text);
            toggle.isOn = filled;
        }

        bool allComplete = ValidateCompletion();

        if (submitButton != null)
        {
            submitButton.interactable = allComplete;
        }

        if (statusText != null)
        {
            statusText.text = allComplete ? completeMessage : incompleteMessage;
        }
    }

    private void OnSubmitClicked()
    {
        if (!isActive || isCompleted)
            return;

        // Final validation guard
        if (!ValidateCompletion())
        {
            if (statusText != null)
            {
                statusText.text = incompleteMessage;
            }
            return;
        }

        Debug.Log($"AREvacCenterRegistrationTask [{taskId}]: Registration confirmed.");

        // Let the base class handle dialogue, mission notification, and hide animation
        CompleteTaskWrapper();
    }

    /// <summary>
    /// Wrapper to access the protected CompleteTask() from a button event.
    /// </summary>
    private void CompleteTaskWrapper()
    {
        // Use CheckCompletion to keep behavior consistent with other tasks
        CheckCompletion();
    }
}
