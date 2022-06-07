using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;



public class playerMovement : MonoBehaviour
{
    /************************************************************************************************************
    GENERAL
    ************************************************************************************************************/
    [SerializeField]
    playerMain main;
    [SerializeField]
    public CharacterController controller;
    [SerializeField]
    public Transform cam;
    /************************************************************************************************************
    JUMP VARIABLES
    ************************************************************************************************************/
    Vector3 velocity;
    public float grav = -100f;
    public float baseGrav = -100f;
    public float superGrav = -250f;
    public float jumpHeight = 1.7f;
    public float coyoteTime = 0.1f;
    public float jumpComboMaxTime = 0.25f;
    float fallingTime, jumpComboTime;
    int comboCounter;
    float[] comboMultipliers = {1f, 1.3f, 2f};
    public bool isGrounded;
    bool isJumping, wasJumping;
    /************************************************************************************************************
    DASH VARIABLES
    ************************************************************************************************************/
    public float dashLength = 2f;
    public float dashTime = 0.1f;
    /************************************************************************************************************
    WALK/RUN VARIABLES
    ************************************************************************************************************/
    public float baseMovementSpeed = 10f;
    public float movementSpeed = 10f;
    public float runningSpeed = 20f;
    public float walkMultiplier, runMultiplier;
    float runningTime;
    public float turnSmoothness = 0.1f;
    float turnVel;
    bool isMoving, isRunning;
    /************************************************************************************************************
    CROUCHING VARIABLES
    ************************************************************************************************************/
    public MeshFilter capsule;
    public float crouchingSpeed = 5f;
    float crouchScale, crouchHeight;
    bool isCrouching;
    /************************************************************************************************************
    BEHAVIOUR
        BUGS:
            - check bug w inputBuffer (some jumps dont register)
            - no ground detection in stairs
            - pressing B while standing increases movementSpeed (you can start running w/o moving)
        TO DO:
            - custom editor
            - copy Mario's cam angles, distances and FOV
            - at least 4 more raycasts in diagonals to improve edge detection
            - max speed + crouch = slide
            - crouch + jump = high jump
            - dash
            - wall run
            - wall jump
    ************************************************************************************************************/
    void Update()
    { 
        // playerCollisions script checks if player is grounded
        if(isGrounded)
        { // Once player touches Ground, test if there's a combo in process
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
            if(controller.velocity.y < 0f)
            {// Falling
                if(isJumping)
                { // At jumping ending, reset jumping state and apply super gravity (JUICE)
                    isJumping = false;
                    wasJumping = true;
                    if(comboCounter < 2) comboCounter++;
                    else comboCounter = 0;
                }else if(!wasJumping)
                { // If falling off an edge, start counting falling time for coyote time (JUICE)
                    fallingTime += Time.deltaTime;
                }
                grav = superGrav;
            }
        }// Running input given, start counting time
        if(isRunning) { runningTime += Time.deltaTime; }
        // Discerning Running (holding) from dashing (pressing)
        if(runningTime > dashTime)
        { // Apply increase in movement speed (JUICE)
            if(runMultiplier < 2) { runMultiplier += 0.2f; }
            else { runMultiplier = 2; }
        } // Apply gravity to vertical velocity
        
        /*if(isRunning) { runningTime += Time.deltaTime; }
        // Discerning Running (holding) from dashing (pressing)
        if(runningTime > dashTime)
        { // Apply increase in movement speed (JUICE)
            if(movementSpeed < runningSpeed) { movementSpeed += 10f * Time.deltaTime; }
            else { movementSpeed = runningSpeed; }
        }*/
        
        
        velocity.y += grav/2f * Time.deltaTime;        
        // Check Input Buffer
        if(main.inputScript.buttonBuffer.Count > 0)
        {
            if(main.inputScript.buttonBuffer.Peek() == 'S')
            { // Apply jump when requirements are met
                if(canJump()) applyJump();
                main.inputScript.buttonBuffer.Dequeue();
            }
        }
        // Movement and orientation
        Vector3 direction = new Vector3(main.inputScript.inputMovement.x, 0f, main.inputScript.inputMovement.y).normalized;
        Vector3 moveDir = Vector3.zero;
        if(direction.magnitude > 0f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVel, turnSmoothness);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }
        // Calculate walking/running multiplier
        if(!isRunning)
        {
            walkMultiplier = 0.4f + (Mathf.Abs(main.inputScript.inputMovement.x) + Mathf.Abs(main.inputScript.inputMovement.y))/2f;
            if(walkMultiplier > 1) walkMultiplier = 1;
        }
        else walkMultiplier = 1;
        // Apply vertical velocity, Input movement and consequential character orientation
        controller.Move((velocity + moveDir.normalized * movementSpeed * walkMultiplier) * Time.deltaTime);
    }
    /************************************************************************************************************
    JUMP
    ************************************************************************************************************/
    void Jump()
    {
        main.inputScript.buttonBuffer.Enqueue('S');
        main.inputScript.programDequeue();
    }
    bool canJump() { return isGrounded || justFellFromEdge(); }
    bool justFellFromEdge(){ return (!isJumping && !wasJumping && fallingTime < coyoteTime); }
    void applyJump()
    {
        isJumping = true;
        grav = baseGrav;
        velocity.y = Mathf.Sqrt(jumpHeight * comboMultipliers[comboCounter] * -2f * grav);
        jumpComboTime = 0f;
    }
    void CancelJump()
    {
        grav = superGrav;
    }
    /************************************************************************************************************
    DASH OR RUN
    ************************************************************************************************************/
    void Run()
    {
        isRunning = true;
    }
    void CancelRun()
    {
        if(runningTime <= dashTime){dash();}
        runMultiplier = 1;
        isRunning = false;
        runningTime = 0f;
    }
    void dash()
    {
        runningTime = 0f;
    }
    /************************************************************************************************************
    DASH OR RUN
    ************************************************************************************************************/
    void Crouch()
    {
        isCrouching = true;
        movementSpeed = crouchingSpeed;
        crouchScale = capsule.transform.localScale.y;
        crouchHeight = capsule.transform.localPosition.y;
        capsule.transform.localScale = new Vector3(capsule.transform.localScale.x, crouchScale/2f, capsule.transform.localScale.z);
        capsule.transform.localPosition = new Vector3(capsule.transform.localPosition.x, crouchHeight - 0.5f, capsule.transform.localPosition.z);
    }
    void cancelCrouch()
    {
        isCrouching = false;
        movementSpeed = baseMovementSpeed;
        capsule.transform.localScale = new Vector3(capsule.transform.localScale.x, crouchScale, capsule.transform.localScale.z);
        capsule.transform.localPosition = new Vector3(capsule.transform.localPosition.x, crouchHeight, capsule.transform.localPosition.z);
    }
}