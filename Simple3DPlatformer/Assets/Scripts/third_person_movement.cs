using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class third_person_movement : MonoBehaviour
{
    /************************************************************************************************************
    IMPUT VARIABLES
    ************************************************************************************************************/
    ControllerInput controls;
    Vector2 inputMovement;
    Queue<char> buttonBuffer;
    public float bufferDelay = 0.15f;
    /************************************************************************************************************
    JUMP VARIABLES
    ************************************************************************************************************/
    Vector3 velocity;
    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundDistance = 0.1f;
    public float grav = -100f;
    public float baseGrav = -100f;
    public float superGrav = -250f;
    public float jumpHeight = 2f;
    public float coyoteTime = 0.1f;
    float fallingTime, jumpComboTime;
    bool isGrounded, isJumping;
    /************************************************************************************************************
    DASH VARIABLES
    ************************************************************************************************************/
    public float dashLength = 2f;
    public float dashTime = 0.5f;
    /************************************************************************************************************
    WALK/RUN VARIABLES
    ************************************************************************************************************/
    public CharacterController controller;
    public Transform cam;
    public float baseMovementSpeed = 8f;
    public float movementSpeed = 8f;
    public float runningSpeed = 20f;
    float runningTime;
    public float turnSmoothness = 0.1f;
    float turnVel;
    bool isMoving, isRunning;
    /************************************************************************************************************
    INPUT SETUP
    ************************************************************************************************************/
    void Awake()
    {
        controls = new ControllerInput();
        buttonBuffer = new Queue<char>(); // Input buffer queue
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
    /************************************************************************************************************
    BEHAVIOUR
        TO DO:
            - control walking velocity with stick input
            - at least 4 more raycasts in diagonals to improe edge detection
            - now you can double jump due to coyoteTime after a jump, need to differenciate between falling off an
            edge and falling from a jump (maybe also checking a jumpsLimit counter)
    ************************************************************************************************************/
    void Update()
    { // Using a few raycasts to test if player is grounded
        isGrounded = shootRayCasts();
        if(isGrounded)
        { // Once player touches Ground, reset all jumping variables but combo
            grav = baseGrav;
            fallingTime = 0f;
            velocity.y = -2f;
        } else {
            if(isJumping && controller.velocity.y < 0f) 
            { // At fall, reset jumping state and apply super gravity (JUICE)
                isJumping = false;
                grav = superGrav;
            } // If falling off an edge or finishing jumping, start counting falling time for coyote time (JUICE)
            if(!isJumping) fallingTime += Time.deltaTime;
        }// Running input given, start 
        if(isRunning) { runningTime += Time.deltaTime; }
        // Discerning Running (holding) from dashing (pressing)
        if(runningTime > dashTime)
        { // Apply increase in movement speed gradually
            if(movementSpeed < runningSpeed) { movementSpeed += 10f * Time.deltaTime; }
            else { movementSpeed = runningSpeed; }
        } // Apply gravity to vertical velocity
        velocity.y += grav/2f * Time.deltaTime;        
        // Check Input Buffer
        if(buttonBuffer.Count > 0)
        {
            if(buttonBuffer.Peek() == 'S')
            { // Apply jump when requirements are met
                if(isGrounded || (!isJumping && fallingTime < coyoteTime)) applyJump(); 
                buttonBuffer.Dequeue();
            }
        }
        // Movement and orientation
        Vector3 direction = new Vector3(inputMovement.x, 0f, inputMovement.y).normalized;
        Vector3 moveDir = Vector3.zero;
        if(direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVel, turnSmoothness);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }
        // Apply vertical velocity, Input movement and consequential character orientation
        controller.Move((velocity + moveDir.normalized * movementSpeed) * Time.deltaTime);
    }

    /************************************************************************************************************
    JUMP
    ************************************************************************************************************/
    void Jump()
    {
        buttonBuffer.Enqueue('S');
        Invoke("pullFromBuffer", bufferDelay);
    }
    void applyJump()
    {
        if(!isJumping && fallingTime < coyoteTime)
        {
            isJumping = true;
            grav = baseGrav;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * grav);
        }
    }
    /************************************************************************************************************
    DASH OR RUN
    ************************************************************************************************************/
    void applyDashRun()
    {
        if(runningTime <= dashTime){dash();}
        movementSpeed = baseMovementSpeed;
        isRunning = false;
        runningTime = 0f;
    }
    void dash()
    {
        runningTime = 0f;
    }



    void pullFromBuffer()
    {
        if(buttonBuffer.Count > 0)
        {
            buttonBuffer.Dequeue();
        }
    }
    bool shootRayCasts(){
        Vector3 posRight = (groundCheck.position + Vector3.right*controller.radius);
        Vector3 posLeft = (groundCheck.position + Vector3.left*controller.radius);
        Vector3 posBack = (groundCheck.position + Vector3.back*controller.radius);
        Vector3 posFront = (groundCheck.position + Vector3.forward*controller.radius);

        return 
        Physics.Raycast(groundCheck.position, Vector3.down, 0.05f) ||
        Physics.Raycast(posRight, Vector3.down, 0.05f) ||
        Physics.Raycast(posLeft, Vector3.down, 0.05f) ||
        Physics.Raycast(posBack, Vector3.down, 0.05f) ||
        Physics.Raycast(posFront, Vector3.down, 0.05f);
    }
    /************************************************************************************************************
    GIZMOS
    ************************************************************************************************************/
    void OnDrawGizmos()
    {
        /*
        Vector3 posRight = (groundCheck.position + Vector3.right*controller.radius);
        Vector3 posLeft = (groundCheck.position + Vector3.left*controller.radius);
        Vector3 posBack = (groundCheck.position + Vector3.back*controller.radius);
        Vector3 posFront = (groundCheck.position + Vector3.forward*controller.radius);
        Vector3 origin, to;
        origin = posRight;
        to = origin + Vector3.down*0.05f;
        Gizmos.DrawLine(origin, to);
        origin = posLeft;
        to = origin + Vector3.down*0.05f;
        Gizmos.DrawLine(origin, to);
        origin = posBack;
        to = origin + Vector3.down*0.05f;
        Gizmos.DrawLine(origin, to);
        origin = posFront;
        to = origin + Vector3.down*0.05f;
        Gizmos.DrawLine(origin, to);
        */
    }
}