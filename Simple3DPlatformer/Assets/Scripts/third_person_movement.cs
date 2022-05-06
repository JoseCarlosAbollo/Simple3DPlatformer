using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class third_person_movement : MonoBehaviour
{
    ControllerInput controls;
    Vector2 inputMovement;
    Queue<char> buttonBuffer;

    public CharacterController controller;
    public Transform cam;
    public Transform groundCheck;
    public LayerMask groundMask;

    public float groundDistance = 0.1f;
    public float grav = -100f;
    public float baseGrav = -100f;
    public float superGrav = -250f;
    public float jumpHeight = 2f;
    public float dashLength = 2f;
    public float coyoteTime = 0.1f;
    public float dashTime = 0.5f;
    public float bufferDelay = 0.15f;

    public float baseMovementSpeed = 8f;
    public float movementSpeed = 8f;
    public float runningSpeed = 20f;

    public float turnSmoothness = 0.1f;

    float turnVel;
    float fallingTime, runningTime;
    bool isGrounded, isMoving, isJumping, isRunning;
    Vector3 velocity;
    void Awake()
    {
        controls = new ControllerInput();
        buttonBuffer = new Queue<char>();
        controls.Gameplay.LStick.performed += ctx => inputMovement = ctx.ReadValue<Vector2>();
        controls.Gameplay.LStick.canceled += ctx => inputMovement = Vector2.zero;
        controls.Gameplay.SouthButton.performed += ctx => Jump();
        controls.Gameplay.SouthButton.canceled += ctx => grav = superGrav;
        controls.Gameplay.EastButton.performed += ctx => isRunning = true;
        controls.Gameplay.EastButton.canceled += ctx => applyDashRun();
    }
    void OnEnable()
    {
        controls.Gameplay.Enable();
    }
    void OnDisable()
    {
        controls.Gameplay.Disable();
    }


    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if(isGrounded)
        {
            grav = baseGrav;
            fallingTime = 0f;
            velocity.y = -2f;
        }else
        {
            if(isJumping && controller.velocity.y < 0f)
            {
                grav = superGrav;
            }
            fallingTime += Time.deltaTime;
        }
        if(isRunning){runningTime += Time.deltaTime;}
        //RUNNING
        if(runningTime > dashTime)
        {
            //APPLY GRADUAL INCREASE IN MOVEMENT
            if(movementSpeed < runningSpeed) { movementSpeed += 10f * Time.deltaTime; }
            else { movementSpeed = runningSpeed; }
        }
        velocity.y += grav/2f * Time.deltaTime;

        if(buttonBuffer.Count > 0)
        {
            if(isGrounded && buttonBuffer.Peek() == 'S') { applyJump(); buttonBuffer.Dequeue(); }
        }
        //MOVEMENT AND ORIENTATION
        Vector3 direction = new Vector3(inputMovement.x, 0f, inputMovement.y).normalized;
        Vector3 moveDir = Vector3.zero;
        if(direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVel, turnSmoothness);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }
        //APPLY GRAVITY, MOVEMENTE, AND ORIENTATION
        controller.Move((velocity + moveDir.normalized * movementSpeed) * Time.deltaTime);
    }

    // JUMP
    void Jump()
    {
        buttonBuffer.Enqueue('S');
        Invoke("pullFromBuffer", bufferDelay);
    }
    void applyJump()
    {
        if(fallingTime < coyoteTime)
        {
            isJumping = true;
            fallingTime = coyoteTime;
            grav = baseGrav;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * grav);
        }
    }

    //DASH OR RUN
    void applyDashRun()
    {
        if(runningTime <= dashTime){dash();}
        movementSpeed = baseMovementSpeed;
        isRunning = false;
        runningTime = 0f;
    }
    void dash()
    {

    }


    void pullFromBuffer()
    {
        if(buttonBuffer.Count > 0)
        {
            buttonBuffer.Dequeue();
        }
    }
}
