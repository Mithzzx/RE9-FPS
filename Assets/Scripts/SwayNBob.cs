using UnityEngine;

public class SwayNBob : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement mover;
    [SerializeField] private InputHandler input;
    [SerializeField] private RecoilAnimation recoil;

    [Header("Sway")]
    [SerializeField] private float step = 0.01f;
    [SerializeField] private float maxStepDistance = 0.06f;
    Vector3 swayPos;

    [Header("Sway Rotation")]
    public float rotationStep = 4f;
    public float maxRotationStep = 5f;
    [SerializeField] private Vector3 swayMultiplier;
    Vector3 swayEulerRot; 

    public float smooth = 10f;
    readonly float smoothRot = 12f;

    [Header("Bobbing")]
    public float speedCurve;

    private float curveSin => Mathf.Sin(speedCurve);
    private float curveCos => Mathf.Cos(speedCurve);

    public Vector3 travelLimit = Vector3.one * 0.025f;
    public Vector3 bobLimit = Vector3.one * 0.01f;
    Vector3 bobPosition;

    public float walkExaggeration;
    public float sprintExaggeration;

    [Header("Bob Rotation")]
    public Vector3 multiplier;
    Vector3 bobEulerRotation;
    
    void Update()
    {
        Sway();
        SwayRotation();
        BobOffset();
        BobRotation();

        CompositePositionRotation();
    }

    void Sway()
    {
        Vector3 invertLook = input.LookInput *-step;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxStepDistance, maxStepDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance, maxStepDistance);

        swayPos = invertLook;
    }

    void SwayRotation()
    {
        Vector2 invertLook = input.LookInput * -rotationStep;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep, maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep, maxRotationStep);
        swayEulerRot = new Vector3(invertLook.y * swayMultiplier.y, invertLook.x * swayMultiplier.x,
            invertLook.x * swayMultiplier.z);
    }

    void CompositePositionRotation()
    {
        Vector3 recoilPosition = recoil.currentPosition;
        Vector3 swayBobPosition = Vector3.Lerp(transform.localPosition, swayPos + bobPosition, Time.deltaTime * smooth);
        transform.localPosition = recoilPosition + swayBobPosition;
        
        Quaternion recoilRotation = Quaternion.Euler(recoil.currentRotation);
        Quaternion swayBobRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation), Time.deltaTime * smoothRot);
        transform.localRotation = recoilRotation * swayBobRotation;
    }

    void BobOffset()
    {
        float bobExaggeration = mover.state == PlayerMovement.MovementState.Walking ? walkExaggeration : sprintExaggeration;

        speedCurve += Time.deltaTime * (mover.isGrounded ? (Mathf.Abs(input.MoveInput.x) + Mathf.Abs(input.MoveInput.y)) * bobExaggeration : 1f) + 0.01f;

        bobPosition.x = (curveCos * bobLimit.x * (mover.isGrounded ? 1 : 0)) - (input.MoveInput.x * travelLimit.x);
        bobPosition.y = (curveSin * bobLimit.y) - (Mathf.Abs(input.MoveInput.y) * travelLimit.y);
        bobPosition.z = -(Mathf.Abs(input.MoveInput.y) * travelLimit.z);
    }

    void BobRotation()
    {
        bobEulerRotation.x = (input.MoveInput != Vector2.zero ? multiplier.x * (Mathf.Sin(2*speedCurve)) : multiplier.x * (Mathf.Sin(2*speedCurve) / 2));
        bobEulerRotation.y = (input.MoveInput != Vector2.zero ? multiplier.y * curveCos : 0);
        bobEulerRotation.z = (input.MoveInput != Vector2.zero ? multiplier.z * curveCos * input.MoveInput.x : 0);
    }
}