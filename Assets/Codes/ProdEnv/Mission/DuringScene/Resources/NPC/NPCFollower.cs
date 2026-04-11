using UnityEngine;

public class NPCFollower : MonoBehaviour
{
    public Transform player;
    public float followDistance = 2f;
    public float moveSpeed = 3f;
    public float rotationSpeed = 8f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string isMovingParameter = "IsMoving";

    [Header("Dialogue")]
    [SerializeField] private NPCDialogueBubble dialogueBubble;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (dialogueBubble == null)
            dialogueBubble = GetComponentInChildren<NPCDialogueBubble>();

        // Auto-assign the player Transform by tag if not set in the Inspector
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("NPCFollower: No GameObject with tag 'Player' found in the scene.");
            }
        }
    }

    private void Update()
    {
        if (player == null) return;

        // Work in horizontal (XZ) space so the NPC stays upright
        Vector3 npcPos = transform.position;
        Vector3 targetPos = player.position;

        // Keep Y the same for movement/rotation to avoid tilting/stretching on slopes
        targetPos.y = npcPos.y;

        Vector3 toTarget = targetPos - npcPos;
        float distance = toTarget.magnitude;
        bool shouldMove = distance > followDistance;
        float normalizedSpeed = 0f;

        if (shouldMove)
        {
            // move toward the player until we reach the follow distance buffer
            Vector3 direction = toTarget.normalized;
            Vector3 moveVector = direction * moveSpeed * Time.deltaTime;
            transform.position += moveVector;

            // rotate smoothly toward player, but stay upright (no pitch/roll)
            Vector3 flatLookDir = new Vector3(direction.x, 0f, direction.z);
            if (flatLookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(flatLookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
            }

            normalizedSpeed = Mathf.Clamp01(moveVector.magnitude / (moveSpeed * Time.deltaTime));
        }

        UpdateAnimation(shouldMove, normalizedSpeed);
    }

    public void SpeakLine(string text, float duration = -1f)
    {
        if (dialogueBubble != null)
            dialogueBubble.ShowLine(text, duration);
    }

    public void HideDialogueBubble()
    {
        if (dialogueBubble != null)
            dialogueBubble.HideImmediate();
    }

    private void UpdateAnimation(bool isMoving, float normalizedSpeed)
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(speedParameter) && AnimatorHasParameter(speedParameter, AnimatorControllerParameterType.Float))
            animator.SetFloat(speedParameter, normalizedSpeed);

        if (!string.IsNullOrEmpty(isMovingParameter) && AnimatorHasParameter(isMovingParameter, AnimatorControllerParameterType.Bool))
            animator.SetBool(isMovingParameter, isMoving);
    }

    private bool AnimatorHasParameter(string paramName, AnimatorControllerParameterType type)
    {
        foreach (var param in animator.parameters)
        {
            if (param.type == type && param.name == paramName)
                return true;
        }
        return false;
    }
}