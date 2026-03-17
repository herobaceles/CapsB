using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class PlayerTrailController : MonoBehaviour
{
    [Header("Movement")]
    public float moveThreshold = 0.1f;

    [Header("Trail Colors")]
    public Color normalColor = new Color(0.42f, 0.35f, 0.23f, 0.7f);
    public Color floodColor = new Color(0.3f, 0.6f, 1f, 0.7f);

    [Header("References")]
    public Transform waterSurface; // assign FloodWater here
    public float surfaceOffset = 0.05f;

    private TrailRenderer trail;
    private Vector3 lastPos;
    private bool isInFlood = false;

    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        lastPos = transform.position;
        ApplyColor(normalColor);
    }

    void Update()
    {
        HandleMovementTrail();
        HandleWaterSubmerge();
    }

    void HandleMovementTrail()
    {
        float speed = (transform.position - lastPos).magnitude / Time.deltaTime;
        trail.emitting = speed > moveThreshold;
        lastPos = transform.position;
    }

    void HandleWaterSubmerge()
    {
        if (!isInFlood || waterSurface == null) return;

        float waterY = waterSurface.position.y + surfaceOffset;

        // Move trail to water surface height
        Vector3 pos = transform.position;
        pos.y = waterY;
        trail.transform.position = pos;
    }

    void ApplyColor(Color c)
    {
        trail.startColor = c;
        trail.endColor = new Color(c.r, c.g, c.b, 0f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Flood"))
        {
            isInFlood = true;
            ApplyColor(floodColor);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Flood"))
        {
            isInFlood = false;
            ApplyColor(normalColor);

            // reset trail to player feet
            trail.transform.localPosition = Vector3.zero;
        }
    }
}