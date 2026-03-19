using UnityEngine;
using UnityEngine.InputSystem;

public class BreakerInteraction : MonoBehaviour
{
    [SerializeField] private GameObject breakerOnVisual;
    [SerializeField] private GameObject breakerOffVisual;

    [Header("Optional animation")]
    [SerializeField] private Animator switchAnimator;
    [SerializeField] private string flipTriggerName = "Flip";
    [SerializeField] private bool completeOnAnimationEvent = false;

    private bool isOn = true;
    private bool hasCompleted = false;

    private void Start()
    {
        UpdateVisual();

        // Prevent the Animator from auto-playing on spawn; we will
        // enable it and play the flip state only when the switch is pressed.
        if (switchAnimator != null)
        {
            switchAnimator.enabled = false;
        }
    }

    private void Update()
    {
        // Touch input (Unity Input System)
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            TryRaycast(touchPosition);
        }
        // Mouse input (Unity Input System)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            TryRaycast(mousePosition);
        }
    }

    private void TryRaycast(Vector2 screenPosition)
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        if (hits == null || hits.Length == 0) return;

        foreach (var hit in hits)
        {
            // Prefer accepting clicks only on the "On" switch visual (or its children)
            if (breakerOnVisual != null)
            {
                Transform target = breakerOnVisual.transform;
                Transform hitTransform = hit.transform;

                if (hitTransform == target || hitTransform.IsChildOf(target))
                {
                    ToggleBreaker();
                    return;
                }
            }
            else
            {
                // Fallback: accept clicks on this object or any of its children
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    ToggleBreaker();
                    return;
                }
            }
        }
    }

    private void ToggleBreaker()
    {
        if (!isOn || hasCompleted) return;

        isOn = false;
        UpdateVisual();

        if (switchAnimator != null)
        {
            // Enable the Animator only when we actually toggle the switch
            switchAnimator.enabled = true;

            // Treat flipTriggerName as the name of the flip state to play
            if (!string.IsNullOrEmpty(flipTriggerName))
            {
                switchAnimator.Play(flipTriggerName, 0, 0f);
            }
            else
            {
                // Fallback: play the default state
                switchAnimator.Play(0, 0, 0f);
            }
        }
    }

    private void UpdateVisual()
    {
        if (breakerOnVisual != null)
            breakerOnVisual.SetActive(isOn);

        if (breakerOffVisual != null)
            breakerOffVisual.SetActive(!isOn);
    }

    private void CompleteTask()
    {
        if (hasCompleted) return;

        hasCompleted = true;

        if (BreakerTaskManager.Instance != null)
        {
            BreakerTaskManager.Instance.CompleteBreakerTask();
        }
    }

    // Optional: called from an Animation Event at the end of the flip animation
    public void OnSwitchFlipAnimationComplete()
    {
        if (completeOnAnimationEvent)
        {
            CompleteTask();
        }
    }
}
