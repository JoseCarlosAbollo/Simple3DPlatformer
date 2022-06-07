using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerInput : MonoBehaviour
{
    [SerializeField]
    playerMain main;

    /************************************************************************************************************
    IMPUT VARIABLES
    ************************************************************************************************************/
    ControllerInput controls;
    public Vector2 inputMovement;
    public Queue<char> buttonBuffer;
    public float bufferDelay = 0.15f;
    void Awake()
    {
        controls = new ControllerInput();
        buttonBuffer = new Queue<char>();
        controls.Gameplay.LStick.performed += ctx => inputMovement = ctx.ReadValue<Vector2>();
        controls.Gameplay.LStick.canceled += ctx => inputMovement = Vector2.zero;
        controls.Gameplay.SouthButton.performed += ctx => main.movementScript.StartCoroutine("Jump");
        controls.Gameplay.SouthButton.canceled += ctx => main.movementScript.StartCoroutine("CancelJump");
        controls.Gameplay.EastButton.performed += ctx => main.movementScript.StartCoroutine("Run");
        controls.Gameplay.EastButton.canceled += ctx => main.movementScript.StartCoroutine("CancelRun");
        controls.Gameplay.L2.performed += ctx => main.movementScript.StartCoroutine("Crouch");
        controls.Gameplay.L2.canceled += ctx => main.movementScript.StartCoroutine("cancelCrouch");
    }
    void OnEnable()
    {
        controls.Gameplay.Enable();
    }
    void OnDisable()
    {
        controls.Gameplay.Disable();
    }
    public void programDequeue()
    {
        Invoke("pullFromBuffer", bufferDelay);
    }
    void pullFromBuffer()
    {
        if(buttonBuffer.Count > 0)
        {
            buttonBuffer.Dequeue();
        }
    }
}