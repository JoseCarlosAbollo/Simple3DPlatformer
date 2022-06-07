using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerCollisions : MonoBehaviour
{
    [SerializeField]
    playerMain main;
    public Transform groundCheck;
    //public LayerMask groundMask;
    public float groundDistance = 0.04f;
    float rad;
    void Start()
    {
        rad = main.movementScript.controller.radius;
    }
    void Update()
    {
        main.movementScript.isGrounded = shootRayCasts();
    }
    bool shootRayCasts(){
        Vector3 posRight = (groundCheck.position + Vector3.right * rad);
        Vector3 posLeft = (groundCheck.position + Vector3.left * rad);
        Vector3 posBack = (groundCheck.position + Vector3.back * rad);
        Vector3 posFront = (groundCheck.position + Vector3.forward * rad);

        return 
        Physics.Raycast(groundCheck.position, Vector3.down, groundDistance) ||
        Physics.Raycast(posRight, Vector3.down, groundDistance) ||
        Physics.Raycast(posLeft, Vector3.down, groundDistance) ||
        Physics.Raycast(posBack, Vector3.down, groundDistance) ||
        Physics.Raycast(posFront, Vector3.down, groundDistance);
    }
}
