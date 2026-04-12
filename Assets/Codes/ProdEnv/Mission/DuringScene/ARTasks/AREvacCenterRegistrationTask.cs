using UnityEngine;

/// <summary>
/// UI-based task for registering at the evacuation center.
/// When the player enters the evac-center trigger, this panel appears.
/// The player must confirm key checklist items before continuing.
/// </summary>
public class AREvacCenterRegistrationTask : ARTaskBase
{
    protected override void OnTaskShow()
    {
        // Visual for the evacuation center registration is shown by the base class.
        // This task is now non-interactive; dialogue explains the concept.
        Debug.Log($"AREvacCenterRegistrationTask [{taskId}]: Shown (non-interactive explanation).");

        // Immediately mark this task as completed; no form interaction is needed.
        CheckCompletion();
    }

    protected override void OnTaskHide()
    {
        Debug.Log($"AREvacCenterRegistrationTask [{taskId}]: Hidden.");
    }

    protected override bool ValidateCompletion()
    {
        // Non-interactive: once the base flow reaches completion, this task
        // is always considered complete (dialogue-only explanation).
        return true;
    }
}
