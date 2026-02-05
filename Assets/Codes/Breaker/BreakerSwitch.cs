using UnityEngine;

public class BreakerSwitch : MonoBehaviour
{
    public float onAngle = -25f;
    public float offAngle = 25f;
    public float speed = 8f;

    bool isOn = true;
    float targetAngle;

    void Start()
    {
        targetAngle = onAngle;
        SetRotation(onAngle);
    }

    void OnMouseDown()
    {
        Toggle();
    }

    public void Toggle()
    {
        isOn = !isOn;
        targetAngle = isOn ? onAngle : offAngle;
    }

    void Update()
    {
        Vector3 rot = transform.localEulerAngles;
        rot.x = Mathf.LerpAngle(rot.x, targetAngle, Time.deltaTime * speed);
        transform.localEulerAngles = rot;
    }

    void SetRotation(float angle)
    {
        Vector3 rot = transform.localEulerAngles;
        rot.x = angle;
        transform.localEulerAngles = rot;
    }

    public void TurnOff()
    {
        isOn = false;
        targetAngle = offAngle;
    }

    public void TurnOn()
    {
        isOn = true;
        targetAngle = onAngle;
    }
}
