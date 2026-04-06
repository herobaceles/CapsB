using UnityEngine;

[ExecuteAlways]
public class MultiWire : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;

    public int wireCount = 5;
    public int segments = 20;

    public float sagMin = 1.5f;
    public float sagMax = 3f;

    public float spread = 0.3f; // horizontal spread

    private LineRenderer[] lines;

    void Update()
    {
        if (pointA == null || pointB == null) return;

        if (lines == null || lines.Length != wireCount)
        {
            // Clear old wires
            foreach (Transform child in transform)
            {
                DestroyImmediate(child.gameObject);
            }

            lines = new LineRenderer[wireCount];

            for (int i = 0; i < wireCount; i++)
            {
                GameObject wire = new GameObject("Wire_" + i);
                wire.transform.parent = transform;

                LineRenderer lr = wire.AddComponent<LineRenderer>();
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.widthMultiplier = 0.05f;
                lr.positionCount = segments;

                lines[i] = lr;
            }
        }

        // Update wires
        for (int w = 0; w < wireCount; w++)
        {
            LineRenderer lr = lines[w];

            // Stable sag per wire
            float sag = Mathf.Lerp(sagMin, sagMax, w / (float)wireCount);

            float offset = (w - wireCount / 2f) * spread;

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);

                Vector3 pos = Vector3.Lerp(pointA.position, pointB.position, t);

                // Sag
                float sagFactor = Mathf.Sin(t * Mathf.PI);
                pos.y -= sagFactor * sag;

                // ✅ Offset ONLY in the middle (fix)
                pos.x += offset * sagFactor;

                lr.SetPosition(i, pos);
            }
        }
    }
}