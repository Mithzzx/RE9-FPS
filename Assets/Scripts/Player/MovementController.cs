using System;
using UnityEngine;

namespace Player
{
    public class MovementController : MonoBehaviour
    {
        Rigidbody rb;
        InputController inputs;
        new AnimationController animation;
        [SerializeField]Transform Fpscamera;

        [Header("Movement")]
        [SerializeField] public bool canMove = true;
        [SerializeField] float walkSpeed = 2.5f;
        [SerializeField] float sprintSpeed = 5f;
        [SerializeField] float crouchSpeed = 1.8f;
        [SerializeField] Transform orintation;
        Vector3 moveDirection;

        [Header("Jump")]
        [SerializeField] float jumpForce = 2f;
        [SerializeField] float jumpCoolDown;
        [SerializeField] float airMultiplier;
        [SerializeField] public bool readyToJump = true;

        [Header("SlopeMovement")]
        [SerializeField] float maxSlopAngle = 80f;
        [SerializeField] bool onslpoe;
        bool exitingSlope;
        RaycastHit slopHit;


        [Header("Mouse Sensitivity")]
        [SerializeField] float xClamp = 85f;
        [SerializeField] float mouseX = 2f;
        [SerializeField] float mouseY = 2f;
        float xRotation;
        float yRotation;

        [Header("Colliders")]
        [SerializeField] CapsuleCollider standCollider1;
        [SerializeField] CapsuleCollider standCollider2;
        [SerializeField] CapsuleCollider crouchCollider;
        

        [SerializeField] public bool isGrounded;
        [SerializeField] float groundDrag;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            inputs = GetComponent<InputController>();
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
            Fpscamera.localRotation = Quaternion.Euler(xRotation, 0, 0);


            if (inputs.Crouch())
            {
                standCollider1.enabled = false;
                standCollider2.enabled = false;
                crouchCollider.enabled = true;
            }
            else
            {
                standCollider1.enabled = true;
                standCollider2.enabled = true;
                crouchCollider.enabled = false;
            }

            //Ground Check
            CheackGrounded();

            //Grag
            if (isGrounded) rb.drag = groundDrag;
            else rb.drag = 0f;

            if (inputs.Jump() && isGrounded && readyToJump)
            {
                readyToJump = false;
                Jump();

                Invoke("ResetJump", jumpCoolDown);
            }
            animation.ProcessAnimation(inputs.Movement(), inputs.Sprint(), inputs.Crouch(), inputs.Jump());
        }

        private void FixedUpdate()
        {
            if (canMove)
            {
                float moveSpeed = walkSpeed;
                if (inputs.Sprint()) moveSpeed = sprintSpeed;
                if (inputs.Crouch()) moveSpeed = crouchSpeed;
                Moveplayer(moveSpeed);
                SpeedControl(moveSpeed);
            } 
        }

        private void Moveplayer(float moveSpeed)
        {
            moveDirection = orintation.forward * inputs.Movement().y + orintation.right * inputs.Movement().x;
            //inslop
            if (OnSlope() && !exitingSlope)
            {
                rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);

                if (rb.velocity.y>0)
                {
                    rb.AddForce(Vector3.down * 40f, ForceMode.Force);
                }
            }
            //in ground
            else if (isGrounded) rb.AddForce(moveDirection * moveSpeed * 10f, ForceMode.Force);
            //in air
            else if (!isGrounded) rb.AddForce(moveDirection * walkSpeed * airMultiplier * 10f, ForceMode.Force);

            //turn off gravity on slope
            rb.useGravity = !OnSlope();
            onslpoe = OnSlope();
        }

        private void CheackGrounded()
        {
            if (Physics.Raycast(transform.position + new Vector3(0,0.98f,0), Vector3.down, 1.3f)) isGrounded = true;
            else isGrounded = false;
        }

        private void SpeedControl(float moveSpeed)
        {
            if (OnSlope() && !exitingSlope)
            {
                if (rb.velocity.magnitude > moveSpeed)
                    rb.velocity = rb.velocity.normalized * moveSpeed;
            }

            else
            {
                Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

                // limit velocity if needed
                if (flatVel.magnitude > moveSpeed)
                {
                    Vector3 limitedVel = flatVel.normalized * moveSpeed;
                    rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
                }
            }
        }

        private void Jump()
        {
            exitingSlope = true;

            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }

        private void ResetJump()
        {
            exitingSlope = false;
            readyToJump = true;
        }

        private bool OnSlope()
        {
            if (Physics.Raycast(transform.position,Vector3.down,out slopHit,1.3f))
            {
                float angle = Vector3.Angle(Vector3.up, slopHit.normal);
                isGrounded = true;
                return angle < maxSlopAngle && angle != 0;
            }
            return false;
        }

        private Vector3 GetSlopeMoveDirection()
        {
            return Vector3.ProjectOnPlane(moveDirection, slopHit.normal).normalized;
        }
    }
}
