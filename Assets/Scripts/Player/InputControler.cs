using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputControler : MonoBehaviour
{
    [SerializeField] PlayerInputActions inputs;

    InputAction move;
    InputAction look;
    InputAction attack;
    InputAction sprint;

    bool isAttacking;
    bool isSptinting;

    private void Awake()
    {
        inputs = new PlayerInputActions();
    }

    private void OnEnable()
    {
        move = inputs.FirstPerson.Move;
        move.Enable();

        look = inputs.FirstPerson.Look;
        look.Enable();

        attack = inputs.FirstPerson.Attack;
        attack.Enable();
        attack.performed += context => isAttacking = !isAttacking;

        sprint = inputs.FirstPerson.Sprint;
        sprint.Enable();
        sprint.performed += context => isSptinting = !isSptinting;

    }

    private void OnDisable()
    {
        move.Disable();
        look.Disable();
        attack.Disable();
        sprint.Disable();
    }

    private void Update()
    {
        Movement();
        Look();
    }

    public Vector2 Look() { return look.ReadValue<Vector2>();  }

    public Vector2 Movement() { return move.ReadValue<Vector2>(); }

    public bool Attack() { return isAttacking; }

    public bool Sprint() { return isSptinting; }
    

}
