using UnityEngine;

[ExecuteAlways]
public class AutoWireSystem : MonoBehaviour
{
    public int segments = 20;
    public int wireCount = 4;

    public float sag = 2f;
    public float spread = 0.3f;

    public float poleHeightOffset = 3f; // fallback if no WirePoint
    public float windStrength = 0.1f;
    public float windSpeed = 2f;

    void Update()
    {
        if (!Application.isPlaying)
        {
            GenerateWires();
        }
    }

    void GenerateWires()
    {
        // Clear old wires
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        GameObject[] poles = GameObject.FindGameObjectsWithTag("Pole");

        for (int i = 0; i < poles.Length; i++)
        {
            Transform nearest = null;
            float minDist = Mathf.Infinity;

            for (int j = 0; j < poles.Length; j++)
            {
                if (i == j) continue;

                float dist = Vector3.Distance(poles[i].transform.position, poles[j].transform.position);

                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = poles[j].transform;
                }
            }

            if (nearest != null)
            {
                Transform a = GetWirePoint(poles[i].transform);
                Transform b = GetWirePoint(nearest);

                CreateWire(a, b);
            }
        }
    }

    Transform GetWirePoint(Transform pole)
    {
        Transform wp = pole.Find("WirePoint");

        if (wp != null)
            return wp;

        // fallback if no WirePoint
        GameObject temp = new GameObject("TempWirePoint");
        temp.transform.position = pole.position + Vector3.up * poleHeightOffset;
        return temp.transform;
    }

    void CreateWire(Transform a, Transform b)
    {
        for (int w = 0; w < wireCount; w++)
        {
            GameObject wire = new GameObject("Wire_" + w);
            wire.transform.parent = transform;

            LineRenderer lr = wire.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.black;
            lr.endColor = Color.black;
            lr.widthMultiplier = 0.05f;
            lr.positionCount = segments;

            float offset = (w - wireCount / 2f) * spread;

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);

                Vector3 pos = Vector3.Lerp(a.position, b.position, t);

                float sagFactor = Mathf.Sin(t * Mathf.PI);

                // sag
                pos.y -= sagFactor * sag;

                // spread (middle only)
                pos.z += offset * sagFactor;

                // wind effect
                float wave = Mathf.Sin(Time.time * windSpeed + i * 0.3f) * windStrength;
                pos.x += wave * sagFactor;

                lr.SetPosition(i, pos);
            }
        }
    }
}