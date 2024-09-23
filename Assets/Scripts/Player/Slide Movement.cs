using System;
using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;

public class SlideMovement : MonoBehaviour
{
    [SerializeField] MovementController movementController;
    [SerializeField] InputController inputController;
    [SerializeField] Rigidbody rb;
    [SerializeField] AnimationController ac;
    [SerializeField] Transform orintation;
    
    [SerializeField] float slideForce = 10f;
    [SerializeField] float slideDuration = 1f;
    float slideTimer;
    bool sliding;

    private void Start()
    {
        movementController = GetComponent<MovementController>();
        inputController = GetComponent<InputController>();
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        if (inputController.Slide() &&
            (inputController.Movement().y > 0.01f || inputController.Movement().x > 0.01f))
        {
            Debug.Log("Slide");
            StartSlide();
        }
    }

    private void FixedUpdate()
    {
        if (sliding)
        {
            SlidingMovement();
        }   
    }

    void StartSlide()
    {
        sliding = true;
        ac.ProcessSlide(sliding);
        slideTimer = slideDuration;
    }

    void SlidingMovement()
    {
        Vector2 inputDirection = orintation.forward * inputController.Movement().y + orintation.right * inputController.Movement().x;
        rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);
        slideTimer -= Time.deltaTime;
        if (slideTimer <= 0)
        {
            StopSlide();
        }
    }
    
    
    void StopSlide()
    {
        sliding = false;
        ac.ProcessSlide(sliding);
    }

}
