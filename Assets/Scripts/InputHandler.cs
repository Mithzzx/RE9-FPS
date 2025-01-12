using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [Header("Input Action Assets")]
    [SerializeField] private InputActionAsset input;
    
    [Header("Action map name references")]
    [SerializeField] private string actionMapName = "FirstPerson";
    
    [Header("Action name references")]
    [SerializeField] private string move = "Move";
    [SerializeField] private string look = "Look";
    [SerializeField] private string attack = "Attack";
    [SerializeField] private string attackHeld = "Attack Held";
    [SerializeField] private string aim = "Aim";
    [SerializeField] private string reload = "Reload";
    [SerializeField] private string jump = "Jump";
    [SerializeField] private string sprint = "Sprint";
    [SerializeField] private string crouch = "Crouch";
    [SerializeField] private string slide = "Slide";
    [SerializeField] private string pause = "Pause";
    [SerializeField] private string grapple = "Grapple";
    
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction attackHeldAction;
    private InputAction attackAction;
    private InputAction aimAction;
    private InputAction reloadAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction slideAction;
    private InputAction pauseAction;
    private InputAction grappleAction;
    
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool AttackTriggered { get; private set; }
    public bool AttackHeld { get; private set; }
    public bool AimTriggered { get; private set; }
    public bool ReloadTriggered { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool SprintTriggered { get; private set; }
    public bool CrouchTriggered { get; private set; }
    public bool SlideTriggered { get; private set; }
    public bool PauseTriggered { get; private set; }
    public bool GrappleTriggered { get; private set; }
    
    public static InputHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("InputHandler instance already exists. Destroying this instance.");
            Destroy(gameObject);
        }
        
        moveAction = input.FindActionMap(actionMapName).FindAction(move);
        lookAction = input.FindActionMap(actionMapName).FindAction(look);
        attackAction = input.FindActionMap(actionMapName).FindAction(attack);
        attackHeldAction = input.FindActionMap(actionMapName).FindAction(attackHeld);
        aimAction = input.FindActionMap(actionMapName).FindAction(aim);
        reloadAction = input.FindActionMap(actionMapName).FindAction(reload);
        jumpAction = input.FindActionMap(actionMapName).FindAction(jump);
        sprintAction = input.FindActionMap(actionMapName).FindAction(sprint);
        crouchAction = input.FindActionMap(actionMapName).FindAction(crouch);
        slideAction = input.FindActionMap(actionMapName).FindAction(slide);
        pauseAction = input.FindActionMap(actionMapName).FindAction(pause);
        grappleAction = input.FindActionMap(actionMapName).FindAction(grapple);
        RegisterInputActions();
    }
    
    private void RegisterInputActions()
    {
        moveAction.performed += context => MoveInput = context.ReadValue<Vector2>();
        moveAction.canceled += context => MoveInput = Vector2.zero;
        
        lookAction.performed += context => LookInput = context.ReadValue<Vector2>();
        lookAction.canceled += context => LookInput = Vector2.zero;

        attackAction.performed += context => AttackTriggered = true;
        
        attackHeldAction.performed += context => AttackHeld = !AttackHeld;
        aimAction.performed += context => AimTriggered = !AimTriggered;
        jumpAction.performed += context => JumpTriggered = !JumpTriggered;
        sprintAction.performed += context => SprintTriggered = !SprintTriggered;
        
        reloadAction.performed += context => ReloadTriggered = true;
        crouchAction.performed += context => CrouchTriggered = true;
        slideAction.performed += context => SlideTriggered = true;
        pauseAction.performed += context => PauseTriggered = true;
        grappleAction.performed += context => GrappleTriggered = true;
    }
    
    private void LateUpdate()
    {
        // Reset triggers after they've been read for a single frame
        AttackTriggered = false;
        ReloadTriggered = false;
        CrouchTriggered = false;
        SlideTriggered = false;
        PauseTriggered = false;
        GrappleTriggered = false;
    }
    
    private void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        attackAction.Enable();
        aimAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();
        crouchAction.Enable();
        slideAction.Enable();
        pauseAction.Enable();
        grappleAction.Enable();
    }
    
    private void OnDisable()
    {
        moveAction.Disable();
        lookAction.Disable();
        attackAction.Disable();
        aimAction.Disable();
        jumpAction.Disable();
        sprintAction.Disable();
        crouchAction.Disable();
        slideAction.Disable();
        pauseAction.Disable();
        grappleAction.Disable();
    }
    
}
