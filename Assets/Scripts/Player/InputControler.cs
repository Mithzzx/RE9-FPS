using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class InputControler : MonoBehaviour
    {
        [SerializeField] PlayerInputActions inputs;

        InputAction move;
        InputAction look;
        InputAction attack;
        InputAction aim;
        InputAction sprint;
        InputAction pause;
        InputAction crouch;
        InputAction jump;

        bool isAttacking;
        bool isAiming;
        bool isSptinting;
        bool isPaused;
        bool isCrouched;
        bool isJumped;

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

            aim = inputs.FirstPerson.Aim;
            aim.Enable();
            aim.performed += context => isAiming = !isAiming;

            sprint = inputs.FirstPerson.Sprint;
            sprint.Enable();
            sprint.performed += context => isSptinting = !isSptinting;

            pause = inputs.FirstPerson.Pause;
            pause.Enable();
            pause.performed += context => isPaused = !isPaused;

            crouch = inputs.FirstPerson.Crouch;
            crouch.Enable();
            crouch.performed += context => isCrouched = !isCrouched;

            jump = inputs.FirstPerson.Jump;
            jump.Enable();
            jump.performed += context => isJumped = !isJumped;

        }

        private void OnDisable()
        {
            move.Disable();
            look.Disable();
            attack.Disable();
            sprint.Disable();
            pause.Disable();
            crouch.Disable();
            jump.Disable();
        }

        private void Update()
        {
            Movement();
            Look();
        }

        public Vector2 Look() { return look.ReadValue<Vector2>();  }

        public Vector2 Movement() { return move.ReadValue<Vector2>(); }

        public bool Attack() { return isAttacking; }

        public bool Aim()    { return isAiming; }

        public bool Sprint() { return isSptinting; }

        public bool Pause() { return isPaused; }

        public bool Crouch() { return isCrouched; }

        public bool Jump() { return isJumped; }

    }
}
