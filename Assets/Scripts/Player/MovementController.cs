using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    Rigidbody rb;
    InputControler inputs;
    new AnimationController animation;
    [SerializeField]Transform Fpscamera;

    [SerializeField] float walkSpeed = 2.5f;
    [SerializeField] float sprintSpeed = 5f;
    [SerializeField] float crouchSpeed = 1.8f;

    [Header("Mouse Sensitivity")]
    [SerializeField] float xClamp = 85f;
    [SerializeField] float mouseX = 2f;
    [SerializeField] float mouseY = 2f;
    float xRotation;

    [SerializeField] CapsuleCollider standCollider;
    [SerializeField] CapsuleCollider crouchCollider; 

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        inputs = GetComponent<InputControler>();
        animation = GetComponent<AnimationController>();
    }

    void Update()
    {
        transform.Rotate(Vector3.up, inputs.Look().x * mouseX* Time.deltaTime);

        xRotation -= inputs.Look().y * mouseY;
        xRotation = Mathf.Clamp(xRotation, -xClamp, xClamp);
        Vector3 targetRotation = transform.eulerAngles;
        targetRotation.x = xRotation;

        Fpscamera.eulerAngles = targetRotation;

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
    }
    private void FixedUpdate()
    {
        float moveSpeed = walkSpeed;
        if (inputs.Sprint()) moveSpeed = sprintSpeed;
        if (inputs.Crouch()) moveSpeed = crouchSpeed;

        if (inputs.Movement().magnitude > 0)
        {
            rb.velocity = transform.forward * inputs.Movement().y * moveSpeed +
                        transform.right * inputs.Movement().x * moveSpeed +
                        transform.up * rb.velocity.y;
        }
        else if (rb.velocity.x != 0 || rb.velocity.z !=0)
        {
            rb.velocity = Vector3.up * rb.velocity.y;
        }

        animation.ProcessAnimation(inputs.Movement(), inputs.Sprint(), inputs.Crouch());
    }
}
