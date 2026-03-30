using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class FloodWave : MonoBehaviour
{
    [Header("Wave Appearance")]
    [Tooltip("Maximum vertical displacement of the water surface.")]
    [Range(0f, 0.25f)]
    public float waveHeight = 0.03f;

    [Tooltip("How fast the ripples move.")]
    [Range(0f, 3f)]
    public float waveSpeed = 0.6f;

    [Tooltip("How wide the ripples are. Lower = smoother, wider waves.")]
    [Range(0.1f, 3f)]
    public float waveScale = 0.7f;

    [Header("Local Ripple Around Target (Optional)")]
    [Tooltip("If assigned, ripples will be strongest at this target and fade out to zero at rippleRadius.")]
    public Transform rippleTarget;

    [Tooltip("Radius in world units where ripples are visible around the target. 0 = affect whole mesh like before.")]
    public float rippleRadius = 0f;

    private Mesh mesh;
    private Vector3[] baseVertices;
    private Vector3[] vertices;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        baseVertices = mesh.vertices;
        vertices = new Vector3[baseVertices.Length];
    }

    void Update()
    {
        float time = Time.time * waveSpeed;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = baseVertices[i];

            // If a rippleTarget and radius are set, compute distance falloff so
            // vertices farther away are affected less (or not at all).
            float radiusWeight = 1f;
            if (rippleTarget != null && rippleRadius > 0.01f)
            {
                // Convert local vertex to world position to compare with target
                Vector3 worldVertex = transform.TransformPoint(vertex);
                Vector2 vertexXZ = new Vector2(worldVertex.x, worldVertex.z);
                Vector2 targetXZ = new Vector2(rippleTarget.position.x, rippleTarget.position.z);
                float distance = Vector2.Distance(vertexXZ, targetXZ);

                // 1 at center, 0 at or beyond rippleRadius (with smooth falloff)
                float t = Mathf.Clamp01(distance / rippleRadius);
                radiusWeight = 1f - t;
                radiusWeight = radiusWeight * radiusWeight; // soften edges (quadratic falloff)
            }

            // Gentle, low-amplitude ripples suitable for flood water.
            // Two blended sine waves keep the motion soft and continuous.
            float wave1 = Mathf.Sin(vertex.x * waveScale + time * 0.7f);
            float wave2 = Mathf.Sin(vertex.z * waveScale * 0.8f + time * 1.1f);

            // Combine waves in [-1, 1], then remap to [0, 1]
            float combined = (wave1 + wave2) * 0.5f;          // [-1, 1]
            float normalized = (combined * 0.5f) + 0.5f;      // [0, 1]

            // Offset only upwards from the base height so water never dips into terrain
            float offset = normalized * waveHeight * radiusWeight;
            vertex.y = baseVertices[i].y + offset;

            vertices[i] = vertex;
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}