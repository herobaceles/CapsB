using UnityEngine;

public class PlayerMovementJoystick : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Joystick joystick;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float rotateSpeed = 12f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";

    private CharacterController controller;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
            Debug.LogError("PlayerMovementJoystick needs a CharacterController on the same GameObject.");

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Auto-find joystick if not assigned
        if (joystick == null)
            joystick = FindObjectOfType<Joystick>(true);
    }

    private void Update()
    {
        if (controller == null || !controller.enabled || !gameObject.activeInHierarchy)
            return;

        // Try to find joystick if UI loads late
        if (joystick == null)
        {
            joystick = FindObjectOfType<Joystick>(true);
            if (joystick == null) return;
        }

        float x = joystick.Horizontal;
        float z = joystick.Vertical;

        Vector3 input = new Vector3(x, 0f, z);

        // Clamp diagonal speed
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        // Camera-relative movement
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 moveDir = input;

        if (cam != null)
        {
            Vector3 camForward = cam.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cam.right;
            camRight.y = 0f;
            camRight.Normalize();

            moveDir = camRight * input.x + camForward * input.z;
        }

        // Rotate toward movement direction
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * rotateSpeed
            );
        }

        // Gravity handling
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDir * moveSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        // 🎯 ANIMATION CONTROL (THIS IS THE KEY PART)
        if (animator != null)
        {
            float speed = input.magnitude;
            animator.SetFloat(speedParam, speed);
        }
    }

    private void OnDisable()
    {
        verticalVelocity = 0f;

        // Force Idle when disabled
        if (animator != null)
            animator.SetFloat(speedParam, 0f);
    }

    // Optional setters
    public void SetJoystick(Joystick newJoystick)
    {
        joystick = newJoystick;
    }

    public void SetController(CharacterController newController)
    {
        controller = newController;
    }
}
