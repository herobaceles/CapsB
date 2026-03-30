using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BoatController : MonoBehaviour
{
    private float horizontal;
    private float vertical;
    public Transform motorPosition;
    public float speed;
    public AnimationCurve accelerationCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

    public float turnSpeed;
    public float tiltForce;

    private float rotation;
    private Rigidbody rb;
    private bool underWater;

    public GameObject turnHelper;
    public Floater floater;

    public float elapsedTime, elapsedTimeBack;

    public float dragUnder, dragOver;

    private InputAction moveAction;

    void Awake()
    {
        moveAction = new InputAction(
            "Move",
            InputActionType.Value,
            null,
            "Vector2");

        var wasd = moveAction.AddCompositeBinding("2DVector");
        wasd.With("Up", "<Keyboard>/w");
        wasd.With("Down", "<Keyboard>/s");
        wasd.With("Left", "<Keyboard>/a");
        wasd.With("Right", "<Keyboard>/d");

        var arrows = moveAction.AddCompositeBinding("2DVector");
        arrows.With("Up", "<Keyboard>/upArrow");
        arrows.With("Down", "<Keyboard>/downArrow");
        arrows.With("Left", "<Keyboard>/leftArrow");
        arrows.With("Right", "<Keyboard>/rightArrow");

        moveAction.AddBinding("<Gamepad>/leftStick");
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        moveAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
    }


    private void FixedUpdate()
    {
        //prevent upside down
        rotation = Vector3.Angle(Vector3.up, transform.TransformDirection(Vector3.up));
        if (rotation > 70f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), 5f * Time.deltaTime);

        // Check if in water
        if (floater.underwater)
        {
            underWater = true;
            rb.drag = dragUnder;
        }
        else
        {
            underWater = false;
            rb.drag = dragOver;
        }


        if (underWater && turnHelper != null)
        {
            rb.AddTorque(transform.up * horizontal * 100f * turnSpeed * Time.deltaTime); //turning

            if (vertical > 0.1f)
            {
                float evaluatedCurve = accelerationCurve.Evaluate(elapsedTime);
                rb.AddForce(turnHelper.transform.forward * speed * evaluatedCurve * 0.05f * vertical * Time.deltaTime * 300f, ForceMode.Force);  //moving
                rb.AddTorque(transform.right * tiltForce * -vertical * Time.deltaTime, ForceMode.Force); //optional tilt 
            }
            if (vertical < -0.1f)
            {
                float evaluatedCurve = accelerationCurve.Evaluate(elapsedTimeBack);
                rb.AddForce(turnHelper.transform.forward * speed * evaluatedCurve * 0.02f * vertical * Time.deltaTime * 300f, ForceMode.Force);  //moving  
            }

        }
    }


    // Update is called once per frame
    void Update()
    {
        Vector2 move = moveAction.ReadValue<Vector2>();
        horizontal = move.x;
        vertical = move.y;

        if (vertical <= 0f && elapsedTime > 0f)
        {
            elapsedTime -= Time.deltaTime;

        }
        if (vertical >= 0f && elapsedTimeBack > 0f)
        {

            elapsedTimeBack -= Time.deltaTime;
        }
        if (vertical >= 0.1f && elapsedTime < 1f)
            elapsedTime += Time.deltaTime;
        if (vertical <= -0.1f && elapsedTimeBack < 1f)
            elapsedTimeBack += Time.deltaTime;
    }
}
