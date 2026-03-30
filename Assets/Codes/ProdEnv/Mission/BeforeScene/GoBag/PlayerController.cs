using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private bool movementEnabled = true;

    // Example movement logic
    void Update()
    {
        if (!movementEnabled)
            return;

        float h = 0f;
        float v = 0f;

        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
        }
        #else
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");
        #endif

        Vector3 movement = new Vector3(h, 0, v) * 5f * Time.deltaTime;
        transform.Translate(movement, Space.World);
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }
}
