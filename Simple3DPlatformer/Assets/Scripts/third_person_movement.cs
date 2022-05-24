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
    public float jumpHeight = 1.7f;
    public float coyoteTime = 0.1f;
    public float jumpComboMaxTime = 0.25f;
    float fallingTime, jumpComboTime;
    int comboCounter;
    float[] comboMultipliers = {1f, 1.3f, 2f};
    bool isGrounded, isJumping, wasJumping;
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
    public float baseMovementSpeed = 10f;
    public float movementSpeed = 10f;
    public float runningSpeed = 20f;
    public float walkMultiplier;
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
            - at least 4 more raycasts in diagonals to improe edge detection
            - double jump
            - crouch
    ************************************************************************************************************/
    void Update()
    { // Using a few raycasts to test if player is grounded
        isGrounded = shootRayCasts();
        if(isGrounded)
        { // Once player touches Ground, test if there's a combo in process
            if(wasJumping) 
            { // Increase the combo counter when landing except after finishing combo
                if(comboCounter < 2) comboCounter++;
                else comboCounter = 0;
            }
            // Reset all jumping variables but combo
            grav = baseGrav;
            wasJumping = false;
            fallingTime = 0f;
            velocity.y = -2f;
            // Resolve combo
            if(comboCounter > 0)
            { // Start timer to concatenate jumps
                jumpComboTime += Time.deltaTime;
                if(jumpComboTime > jumpComboMaxTime)
                { // Reset combo when jump is missed
                    comboCounter = 0;
                    jumpComboTime = 0;
                }
            }
        } else { // In case Player is on the air
            // Falling
            if(controller.velocity.y < 0f)
            {
                if(isJumping)
                { // At jumping ending, reset jumping state and apply super gravity (JUICE)
                    isJumping = false;
                    wasJumping = true;
                    grav = superGrav;
                }else if(!wasJumping)
                { // If falling off an edge, start counting falling time for coyote time (JUICE)
                    fallingTime += Time.deltaTime;
                }
            }
        }// Running input given, start counting time
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
                if(canJump()) applyJump(); 
                buttonBuffer.Dequeue();
            }
        }
        // Movement and orientation
        Vector3 direction = new Vector3(inputMovement.x, 0f, inputMovement.y).normalized;
        Vector3 moveDir = Vector3.zero;
        if(direction.magnitude > 0f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVel, turnSmoothness);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }
        // Calculate walking/running multiplier
        walkMultiplier = 0.4f + (Mathf.Abs(inputMovement.x) + Mathf.Abs(inputMovement.y))/2f;
        if(walkMultiplier > 1) walkMultiplier = 1;
        // Apply vertical velocity, Input movement and consequential character orientation
        controller.Move((velocity + moveDir.normalized * movementSpeed * walkMultiplier) * Time.deltaTime);
    }

    /************************************************************************************************************
    JUMP
    ************************************************************************************************************/
    void Jump()
    {
        buttonBuffer.Enqueue('S');
        Invoke("pullFromBuffer", bufferDelay);
    }
    bool canJump() { return isGrounded || (!isJumping && !wasJumping && fallingTime < coyoteTime); }
    void applyJump()
    {
        isJumping = true;
        grav = baseGrav;
        velocity.y = Mathf.Sqrt(jumpHeight * comboMultipliers[comboCounter] * -2f * grav);
        jumpComboTime = 0f;
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