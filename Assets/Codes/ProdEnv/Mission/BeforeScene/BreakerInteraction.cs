using UnityEngine;
using UnityEngine.InputSystem;

public class BreakerInteraction : MonoBehaviour
{
    [SerializeField] private GameObject breakerOnVisual;
    [SerializeField] private GameObject breakerOffVisual;

    private bool isOn = true;

    private void Start()
    {
        UpdateVisual();
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
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            // Accept clicks on this object or any of its children
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                ToggleBreaker();
            }
        }
    }

    private void ToggleBreaker()
    {
        if (!isOn) return;

        isOn = false;
        UpdateVisual();

        if (BreakerTaskManager.Instance != null)
        {
            BreakerTaskManager.Instance.CompleteBreakerTask();
        }
    }

    private void UpdateVisual()
    {
        if (breakerOnVisual != null)
            breakerOnVisual.SetActive(isOn);

        if (breakerOffVisual != null)
            breakerOffVisual.SetActive(!isOn);
    }
}
