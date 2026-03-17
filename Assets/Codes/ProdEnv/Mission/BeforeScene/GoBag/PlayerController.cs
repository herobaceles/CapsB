using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private bool movementEnabled = true;

    // Example movement logic
    void Update()
    {
        if (!movementEnabled)
            return;

        // Replace with your actual movement code
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(h, 0, v);
        transform.Translate(move * Time.deltaTime * 5f);
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }
}
