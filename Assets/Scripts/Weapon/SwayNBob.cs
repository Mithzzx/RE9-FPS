using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;

public class SwayNBob : MonoBehaviour
{
    [SerializeField] InputControler input;
    [SerializeField] MovementController mover;

    [Header("Sway")]
    public float step = 0.01f;
    public float maxStepDistance = 0.06f;
    Vector3 swayPos;

    [Header("Sway Rotation")]
    public float rotationStep = 4f;
    public float maxRotationStep = 5f;
    Vector3 swayEulerRot;

    public float smooth = 10f;
    float smoothRot = 12f;

    [Header("Bobbing")]
    public float speedCurve;
    float curveSin { get => Mathf.Sin(speedCurve); }
    float curveCos { get => Mathf.Cos(speedCurve); }

    public Vector3 travelLimit = Vector3.one * 0.025f;
    public Vector3 bobLimit = Vector3.one * 0.01f;
    Vector3 bobPosition;

    public float bobExaggeration;

    [Header("Bob Rotation")]
    public Vector3 multiplier;
    Vector3 bobEulerRotation;

    private void Update()
    {
        Sway();
        SwayRotation();
        BobOffset();
        BobRotation();

        CompositePositionRotation();
    }

    void Sway()
    {
        Vector3 invertLook = input.Look() * -step;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxStepDistance, maxStepDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance, maxStepDistance);

        swayPos = invertLook;
    }

    void SwayRotation()
    {
        Vector2 invertLook = input.Look() * -rotationStep;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep, maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep, maxRotationStep);
        swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x * -1.5f);
    }

    void BobOffset()
    {
        speedCurve += Time.deltaTime * (mover.isGrounded ? (input.Movement().x + input.Movement().y) * bobExaggeration : 1f) + 0.01f;

        bobPosition.x = (curveCos * bobLimit.x * (mover.isGrounded ? 1 : 0))
            - (input.Movement().x * travelLimit.x);

        bobPosition.y = (curveSin * bobLimit.y)
            - (input.Movement().y*travelLimit.y);

        bobPosition.z =
            -(input.Movement().y * travelLimit.z);
    }

    void BobRotation()
    {
        bobEulerRotation.x = (input.Movement() != Vector2.zero ? multiplier.x * (Mathf.Sin(2 * speedCurve)) : multiplier.x * (Mathf.Sin(2 * speedCurve) / 2));
        bobEulerRotation.y = (input.Movement() != Vector2.zero ? multiplier.y * curveCos : 0);
        bobEulerRotation.z = (input.Movement() != Vector2.zero ? multiplier.z * curveCos * input.Movement().x : 0);
    }


    void CompositePositionRotation()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition,
            swayPos + bobPosition,
            Time.deltaTime * smooth);

        transform.localRotation = Quaternion.Slerp(transform.localRotation,
            Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation),
            Time.deltaTime * smoothRot);
    }
}