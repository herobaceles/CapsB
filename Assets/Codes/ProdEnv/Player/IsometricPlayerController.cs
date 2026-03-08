using UnityEngine;

/// <summary>
/// Player controller for isometric movement.
/// Works with joystick input for mobile or keyboard for testing.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class IsometricPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;

    [Header("Joystick (Assign from Asset Store joystick)")]
    [SerializeField] private FixedJoystick joystick; // Works with Joystick Pack
    // If using a different joystick, change this type or use the interface below

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer = -1;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string isMovingParameter = "IsMoving";

    // Components
    private CharacterController characterController;
    private Transform cameraTransform;

    // State
    private Vector3 moveDirection;
    private Vector3 currentVelocity;
    private float verticalVelocity;
    private bool isGrounded;

    // Input
    private Vector2 inputDirection;
    private bool movementEnabled = true;

    public bool IsMoving => inputDirection.sqrMagnitude > 0.01f;
    public Vector3 Velocity => currentVelocity;
    public float CurrentSpeed => currentVelocity.magnitude;
    public bool IsMovementEnabled => movementEnabled;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // Find main camera for isometric direction reference
        cameraTransform = Camera.main?.transform;

        // Ensure player is tagged
        if (!CompareTag("Player"))
        {
            gameObject.tag = "Player";
            // Debug.Log("IsometricPlayerController: Set tag to 'Player'");
        }
    }

    private void Update()
    {
        GatherInput();
        CheckGrounded();
        Move();
        UpdateAnimation();
    }

    private void GatherInput()
    {
        if (!movementEnabled)
        {
            inputDirection = Vector2.zero;
            return;
        }

        // Get input from joystick
        if (joystick != null)
        {
            inputDirection = new Vector2(joystick.Horizontal, joystick.Vertical);
        }
        else
        {
            // Fallback to keyboard for testing using new Input System
            #if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                float x = 0f;
                float y = 0f;
                if (UnityEngine.InputSystem.Keyboard.current.aKey.isPressed || UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed)
                    x -= 1f;
                if (UnityEngine.InputSystem.Keyboard.current.dKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed)
                    x += 1f;
                if (UnityEngine.InputSystem.Keyboard.current.wKey.isPressed || UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed)
                    y += 1f;
                if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed || UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed)
                    y -= 1f;
                inputDirection = new Vector2(x, y);
            }
            else
            {
                inputDirection = Vector2.zero;
            }
            #else
            inputDirection = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );
            #endif
        }

        // Normalize if magnitude > 1
        if (inputDirection.sqrMagnitude > 1f)
            inputDirection.Normalize();
    }

    private void CheckGrounded()
    {
        // Check if grounded using raycast
        isGrounded = Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            groundCheckDistance + 0.1f,
            groundLayer
        );

        // Reset vertical velocity when grounded
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f; // Small downward force to keep grounded
        }
    }

    private void Move()
    {
        // Convert input to isometric world direction
        moveDirection = ConvertToIsometric(inputDirection);

        // Debug: Log input and movement
        if (inputDirection.sqrMagnitude > 0.01f)
        {
            // Debug.Log($"Input: {inputDirection}, MoveDir: {moveDirection}, CurrentVel: {currentVelocity.magnitude}");
        }

        // Calculate target velocity
        Vector3 targetVelocity = moveDirection * moveSpeed;

        // Smooth acceleration/deceleration
        float rate = moveDirection.sqrMagnitude > 0.01f ? acceleration : deceleration;
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, rate * Time.deltaTime);

        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;
        Vector3 finalVelocity = currentVelocity + Vector3.up * verticalVelocity;

        // Move character
        characterController.Move(finalVelocity * Time.deltaTime);

        // Rotate towards movement direction
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// Convert 2D input to isometric 3D direction based on camera orientation
    /// </summary>
    private Vector3 ConvertToIsometric(Vector2 input)
    {
        if (input.sqrMagnitude < 0.01f)
            return Vector3.zero;

        if (cameraTransform == null)
        {
            // Default isometric direction (45 degrees rotated)
            Vector3 forward = new Vector3(1, 0, 1).normalized;
            Vector3 right = new Vector3(1, 0, -1).normalized;
            return (forward * input.y + right * input.x).normalized;
        }

        // Use camera's forward/right projected on ground plane
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        // Flatten to horizontal plane
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        // Calculate world direction
        Vector3 worldDirection = camForward * input.y + camRight * input.x;
        return worldDirection.normalized;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        float speed = currentVelocity.magnitude / moveSpeed; // Normalized 0-1

        if (!string.IsNullOrEmpty(speedParameter))
            animator.SetFloat(speedParameter, speed);

        if (!string.IsNullOrEmpty(isMovingParameter))
        {
            // Only set if parameter exists
            bool hasIsMoving = false;
            foreach (var param in animator.parameters)
            {
                if (param.name == isMovingParameter && param.type == AnimatorControllerParameterType.Bool)
                {
                    hasIsMoving = true;
                    break;
                }
            }
            if (hasIsMoving)
                animator.SetBool(isMovingParameter, IsMoving);
        }
    }

    /// <summary>
    /// Set joystick reference at runtime
    /// </summary>
    public void SetJoystick(FixedJoystick newJoystick)
    {
        joystick = newJoystick;
    }

    /// <summary>
    /// Teleport player to position
    /// </summary>
    public void Teleport(Vector3 position)
    {
        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = true;
        currentVelocity = Vector3.zero;
    }

    /// <summary>
    /// Stop all movement
    /// </summary>
    public void StopMovement()
    {
        currentVelocity = Vector3.zero;
        inputDirection = Vector2.zero;
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        if (!movementEnabled)
            StopMovement();
    }

    private void OnDrawGizmosSelected()
    {
        // Draw ground check
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(
            transform.position + Vector3.up * 0.1f,
            transform.position + Vector3.down * groundCheckDistance
        );
    }
}
