using UnityEngine;

public class IsometricFollowCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";

    [Header("Isometric View")]
    [SerializeField] private float followDistance = 13.5f; // zoomed out slightly
    [SerializeField] private Vector3 eulerAngles = new Vector3(30f, 45f, 0f);

    [Header("Framing")]
    [SerializeField] private float focusHeight = 1.2f;
    [SerializeField] private Vector3 worldScreenOffset = new Vector3(0f, -0.5f, 0f); // push character down

    [Header("Follow")]
    [SerializeField] private float followSmooth = 10f;

    private void LateUpdate()
    {
        if (target == null)
        {
            TryFindTarget();
            if (target == null) return;
        }

        Quaternion rot = Quaternion.Euler(eulerAngles);

        Vector3 focusPoint =
            target.position +
            Vector3.up * focusHeight +
            worldScreenOffset;

        Vector3 desiredPos =
            focusPoint - (rot * Vector3.forward) * followDistance;

        transform.rotation = rot;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos,
            Time.deltaTime * followSmooth
        );
    }

    private void TryFindTarget()
    {
        if (string.IsNullOrEmpty(targetTag)) return;
        GameObject go = GameObject.FindGameObjectWithTag(targetTag);
        if (go != null) target = go.transform;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
