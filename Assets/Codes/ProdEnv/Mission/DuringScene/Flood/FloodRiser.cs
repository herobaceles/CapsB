using UnityEngine;

public class FloodRiser : MonoBehaviour
{
    public float riseSpeed = 0.15f;
    public float maxHeight = 2.5f;

    [Header("Wave Effect")]
    public float waveAmplitude = 0.03f;
    public float waveFrequency = 1.5f;
    private float baseY;

    void Start()
    {
        baseY = transform.position.y;
    }

    void Update()
    {
        float targetY = baseY;
        if (transform.position.y < maxHeight)
        {
            targetY = Mathf.Min(transform.position.y + riseSpeed * Time.deltaTime, maxHeight);
            baseY = targetY; // Update baseY as the water rises
        }
        // Gentle flood wave effect using Perlin noise
        float wave = Mathf.PerlinNoise(Time.time * waveFrequency, 0f) * waveAmplitude - (waveAmplitude * 0.5f);
        transform.position = new Vector3(
            transform.position.x,
            targetY + wave,
            transform.position.z
        );
    }
}