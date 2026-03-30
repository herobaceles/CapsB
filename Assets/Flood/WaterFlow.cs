using UnityEngine;

public class WaterFlow : MonoBehaviour
{
    public Renderer waterRenderer;
    public Vector2 flowDirection = new Vector2(0.05f, 0.02f);
    public string textureProperty = "_BaseMap";

    private Vector2 offset;

    void Update()
    {
        if (waterRenderer == null) return;

        offset += flowDirection * Time.deltaTime;
        waterRenderer.material.SetTextureOffset(textureProperty, offset);
    }
}