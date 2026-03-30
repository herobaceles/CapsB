using UnityEngine;

public class WaterStep : MonoBehaviour
{
    public WaterDetector detector;
    public ParticleSystem splash;
    public Transform footPoint;

    public float moveThreshold = 0.05f;
    private Vector3 lastPos;

    void Start()
    {
        lastPos = transform.position;
    }

    void Update()
    {
        float move = Vector3.Distance(transform.position, lastPos);

        if (detector.isInWater && move > moveThreshold)
        {
            splash.transform.position = footPoint.position;

            if (!splash.isPlaying)
                splash.Play();
        }

        lastPos = transform.position;
    }
}