using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Isometric camera that follows the player.
/// Maintains fixed angle and smooth following.
/// </summary>
public class IsometricCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Isometric Settings")]
    [SerializeField] private float distance = 14f; // Increased distance for wider view
    [SerializeField] private float height = 14f;   // Increased height for wider view
    [SerializeField] private float angle = 45f; // Rotation around Y axis
    [SerializeField] private float forwardOffset = 6f; // Moves camera forward to center player vertically

    [Header("Following")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSpeed = 3f;

    [Header("Bounds (Optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds = new Vector2(-50, -50);
    [SerializeField] private Vector2 maxBounds = new Vector2(50, 50);

    [Header("Zoom")]
    [SerializeField] private bool allowZoom = false;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private float zoomSpeed = 2f;

    // State
    private Vector3 currentPosition;
    private IsometricPlayerController playerController;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    // Expose current camera settings for tasks that temporarily adjust the view
    public float CurrentDistance => distance;
    public float CurrentAngle => angle;

    private void Start()
    {
        if (target == null && autoFindPlayer)
        {
            FindPlayer();
        }

        if (target != null)
        {
            // Initialize position immediately
            currentPosition = CalculateDesiredPositionNoLookAhead();
            transform.position = currentPosition;
            LookAtTarget();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            if (autoFindPlayer)
                FindPlayer();
            return;
        }

        HandleZoom();
        FollowTarget();
        LookAtTarget();
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            playerController = player.GetComponent<IsometricPlayerController>();
            Debug.Log("IsometricCameraController: Found player");
        }
    }

    private void FollowTarget()
    {
        // Calculate desired position
        Vector3 desiredPosition = CalculateDesiredPositionNoLookAhead();

        // Apply bounds if enabled
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, minBounds.y, maxBounds.y);
        }

        // Direct follow (no smoothing) to eliminate wobble
        currentPosition = desiredPosition;
        transform.position = currentPosition;
    }

    private Vector3 CalculateDesiredPositionNoLookAhead()
    {
        // Calculate offset based on isometric angle
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Sin(rad) * distance,
            height,
            Mathf.Cos(rad) * distance
        );
        // Add forward offset to center player vertically in view
        Vector3 forward = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)).normalized;
        Vector3 targetPos = target.position + forward * forwardOffset;
        return targetPos + offset;
    }

    private void LookAtTarget()
    {
        // No look ahead for stable camera
        transform.LookAt(target.position);
    }

    private void HandleZoom()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // Input System zoom (mouse scroll)
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance = Mathf.Clamp(distance - scroll * zoomSpeed * 0.1f, minDistance, maxDistance);
                height = distance;
            }
        }
#else
        // Legacy Input zoom (mouse scroll)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed * 10f, minDistance, maxDistance);
            height = distance;
        }
#endif
    }

    /// <summary>
    /// Set camera angle (rotation around Y axis)
    /// </summary>
    public void SetAngle(float newAngle)
    {
        angle = newAngle;
    }

    /// <summary>
    /// Set camera distance and height
    /// </summary>
    public void SetDistance(float newDistance)
    {
        distance = Mathf.Clamp(newDistance, minDistance, maxDistance);
        height = distance;
    }

    /// <summary>
    /// Snap camera immediately to target
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        currentPosition = CalculateDesiredPositionNoLookAhead();
        transform.position = currentPosition;
        LookAtTarget();
    }

    /// <summary>
    /// Shake camera (for effects)
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private System.Collections.IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.position = currentPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        // Draw camera setup
        Gizmos.color = Color.cyan;
        Vector3 desiredPos = CalculateDesiredPositionNoLookAhead();
        Gizmos.DrawLine(target.position, desiredPos);
        Gizmos.DrawWireSphere(desiredPos, 0.5f);

        // Draw bounds
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(
                (minBounds.x + maxBounds.x) / 2f,
                target.position.y,
                (minBounds.y + maxBounds.y) / 2f
            );
            Vector3 size = new Vector3(
                maxBounds.x - minBounds.x,
                1f,
                maxBounds.y - minBounds.y
            );
            Gizmos.DrawWireCube(center, size);
        }
    }
}
