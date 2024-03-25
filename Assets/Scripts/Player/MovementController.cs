using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    Rigidbody rb;
    InputControler inputs;
    new AnimationController animation;

    [SerializeField] float walkSpeed = 2.5f;
    [SerializeField] float sprintSpeed = 5f;

    [SerializeField] float camSencitivity = 2f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        inputs = GetComponent<InputControler>();
        animation = GetComponent<AnimationController>();
    }

    void Update()
    {
        transform.Rotate(Vector3.up, inputs.Look().x * camSencitivity* Time.deltaTime);
    }
    private void FixedUpdate()
    {
        float moveSpeed = walkSpeed;
        if (inputs.Sprint()) moveSpeed = sprintSpeed;

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

        animation.ProcessAnimation(inputs.Movement(), inputs.Sprint());
    }
}
