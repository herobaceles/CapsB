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
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool shouldMove = distance > followDistance;
        float normalizedSpeed = 0f;

        if (shouldMove)
        {
            // move toward the player until we reach the follow distance buffer
            Vector3 direction = (player.position - transform.position).normalized;
            Vector3 moveVector = direction * moveSpeed * Time.deltaTime;
            transform.position += moveVector;

            // rotate smoothly toward player
            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);

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