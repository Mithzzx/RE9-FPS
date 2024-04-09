using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    Rigidbody rb;
    InputControler inputs;
    new AnimationController animation;
    [SerializeField]Transform Fpscamera;

    [Header("Movement")]
    [SerializeField] float walkSpeed = 2.5f;
    [SerializeField] float sprintSpeed = 5f;
    [SerializeField] float crouchSpeed = 1.8f;
    [SerializeField] float jumpForce = 2f;
    [SerializeField] Transform orintation;
    Vector3 moveDirection;

    [Header("Mouse Sensitivity")]
    [SerializeField] float xClamp = 85f;
    [SerializeField] float mouseX = 2f;
    [SerializeField] float mouseY = 2f;
    float xRotation;
    float yRotation;

    [Header("Colliders")]
    [SerializeField] CapsuleCollider standCollider;
    [SerializeField] CapsuleCollider crouchCollider;

    [SerializeField] bool isGrounded;
    [SerializeField] bool jumped;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        inputs = GetComponent<InputControler>();
        animation = GetComponent<AnimationController>();
    }

    void Update()
    {

        //mouse

        float mx = inputs.Look().x * Time.deltaTime * mouseX;
        float my = inputs.Look().y * Time.deltaTime * mouseY;

        yRotation += mx;
        xRotation -= my;
        xRotation = Math.Clamp(xRotation, -xClamp, xClamp);

        transform.rotation = Quaternion.Euler(0, yRotation, 0);
        Fpscamera.rotation = Quaternion.Euler(xRotation, yRotation, 0);


        if (inputs.Crouch())
        {
            standCollider.enabled = false;
            crouchCollider.enabled = true;
        }
        else
        {
            standCollider.enabled = true;
            crouchCollider.enabled = false;
        }

        //Ground Check
        CheackGrounded();

        if (inputs.Jump() && isGrounded)
        {
            rb.AddForce(transform.up * jumpForce, ForceMode.Force);
            jumped = true;
        }
        else jumped = false;
    }

    private void FixedUpdate()
    {
        float moveSpeed = walkSpeed;
        if (inputs.Sprint()) moveSpeed = sprintSpeed;
        if (inputs.Crouch()) moveSpeed = crouchSpeed;
        Moveplayer(moveSpeed);

        animation.ProcessAnimation(inputs.Movement(), inputs.Sprint(), inputs.Crouch());
    }

    private void Moveplayer(float moveSpeed)
    {
        moveDirection = orintation.forward * inputs.Movement().y + orintation.right * inputs.Movement().x;

        rb.AddForce(moveDirection * moveSpeed * 10f, ForceMode.Force);
    }

    private void CheackGrounded()
    {
        if (Physics.Raycast(transform.position + new Vector3(0,0.93f,0), Vector3.down, 1f)) isGrounded = true;
        else isGrounded = false;
    }
}
