using UnityEngine;

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
    [SerializeField] private float distance = 10f;
    [SerializeField] private float height = 10f;
    [SerializeField] private float angle = 45f; // Rotation around Y axis

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
    private Vector3 currentLookAhead;
    private Vector3 currentPosition;
    private IsometricPlayerController playerController;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private void Start()
    {
        if (target == null && autoFindPlayer)
        {
            FindPlayer();
        }

        if (target != null)
        {
            // Initialize position immediately
            currentPosition = CalculateDesiredPosition();
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
        // Calculate look ahead based on player movement
        if (playerController != null && playerController.IsMoving)
        {
            Vector3 velocity = playerController.Velocity.normalized;
            Vector3 targetLookAhead = velocity * lookAheadDistance;
            currentLookAhead = Vector3.Lerp(currentLookAhead, targetLookAhead, lookAheadSpeed * Time.deltaTime);
        }
        else
        {
            currentLookAhead = Vector3.Lerp(currentLookAhead, Vector3.zero, lookAheadSpeed * Time.deltaTime);
        }

        // Calculate desired position
        Vector3 desiredPosition = CalculateDesiredPosition();

        // Apply bounds if enabled
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, minBounds.y, maxBounds.y);
        }

        // Smooth follow
        currentPosition = Vector3.Lerp(currentPosition, desiredPosition, followSpeed * Time.deltaTime);
        transform.position = currentPosition;
    }

    private Vector3 CalculateDesiredPosition()
    {
        // Calculate offset based on isometric angle
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(
            Mathf.Sin(rad) * distance,
            height,
            Mathf.Cos(rad) * distance
        );

        // Target position with look ahead
        Vector3 targetPos = target.position + currentLookAhead;

        return targetPos + offset;
    }

    private void LookAtTarget()
    {
        Vector3 lookTarget = target.position + currentLookAhead;
        transform.LookAt(lookTarget);
    }

    private void HandleZoom()
    {
        if (!allowZoom) return;

        // Pinch zoom for mobile
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            float prevDistance = (touch0PrevPos - touch1PrevPos).magnitude;
            float currentDistance = (touch0.position - touch1.position).magnitude;

            float delta = prevDistance - currentDistance;
            distance = Mathf.Clamp(distance + delta * zoomSpeed * 0.01f, minDistance, maxDistance);
            height = distance; // Keep height proportional
        }

        // Scroll wheel for editor
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSpeed * 10f, minDistance, maxDistance);
            height = distance;
        }
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

        currentLookAhead = Vector3.zero;
        currentPosition = CalculateDesiredPosition();
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
        Vector3 desiredPos = CalculateDesiredPosition();
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
