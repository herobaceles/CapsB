using UnityEngine;

public class MeshWave : MonoBehaviour
{
    public float waveHeight = 0.1f;
    public float waveSpeed = 1f;
    public float waveLength = 2f;

    private Mesh mesh;
    private Vector3[] originalVerts;
    private Vector3[] displacedVerts;

    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null)
        {
            Debug.LogError("[MeshWave] No MeshFilter found on object.");
            enabled = false;
            return;
        }

        // Ensure we operate on an instance of the mesh (avoid modifying sharedMesh / ProBuilder runtime mesh)
        Mesh source = mf.sharedMesh;
        if (source == null)
        {
            Debug.LogError("[MeshWave] MeshFilter has no mesh (sharedMesh is null).");
            enabled = false;
            return;
        }

        mesh = Instantiate(source);
        mf.mesh = mesh;
        mesh.MarkDynamic();

        originalVerts = mesh.vertices;
        displacedVerts = new Vector3[originalVerts.Length];
    }

    void Update()
    {
        if (mesh == null || originalVerts == null || displacedVerts == null) return;

        for (int i = 0; i < displacedVerts.Length; i++)
        {
            Vector3 v = originalVerts[i];
            float wave = Mathf.Sin(Time.time * waveSpeed + (v.x + v.z) * waveLength) * waveHeight;

            // Preserve original x/z, set y to wave offset (or add to original y if desired)
            displacedVerts[i] = new Vector3(v.x, v.y + wave, v.z);
        }

        mesh.vertices = displacedVerts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
