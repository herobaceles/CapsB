using UnityEngine;

public class WaterRipple : MonoBehaviour
{
    public float speed = 0.1f;
    Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        Vector2 offset = new Vector2(Time.time * speed, Time.time * speed);
        rend.material.SetTextureOffset("_BaseMap", offset);
        rend.material.SetTextureOffset("_BumpMap", offset);
    }
}