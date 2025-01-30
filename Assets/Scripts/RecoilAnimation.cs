using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class RecoilAnimation : MonoBehaviour
{
    [SerializeField] private Transform gunMesh;
    // Initial transforms
    private Vector3 initialPosition;
    private Vector3 initialRotation;

    // Rotations
    private Vector3 targetRotation;
    [HideInInspector] public Vector3 currentRotation;

    // Positions
    private Vector3 targetPosition;
    [HideInInspector] public Vector3 currentPosition;

    [Header("Settings")]
    [SerializeField] private float snappiness;
    [SerializeField] private float returnSpeed;

    [Header("Position Ranges")]
    [SerializeField] private Vector2 recoilXPosRange;
    [SerializeField] private Vector2 recoilYPosRange;
    [SerializeField] private Vector2 recoilZPosRange;

    [Header("Rotation Ranges")]
    [SerializeField] private Vector2 recoilXRotRange;
    [SerializeField] private Vector2 recoilYRotRange;
    [SerializeField] private Vector2 recoilZRotRange;

    void Start()
    {
        // Store initial transforms
        initialPosition = gunMesh.localPosition;
        initialRotation = gunMesh.localRotation.eulerAngles;
    }

    void Update()
    {
        // Smoothly interpolate rotation back to initial rotation
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.fixedDeltaTime);

        // Smoothly interpolate position back to initial position
        targetPosition = Vector3.Lerp(targetPosition, Vector3.zero, returnSpeed * Time.deltaTime);
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, snappiness * Time.deltaTime);

        // Apply the recoil to the gunMesh
        gunMesh.localRotation = Quaternion.Euler(initialRotation + currentRotation);
        gunMesh.localPosition = initialPosition + currentPosition;
    }

    public void GenerateRecoil()
    {
        // Generate random recoil for rotation
        targetRotation += new Vector3(
            Random.Range(recoilXRotRange.x, recoilXRotRange.y),
            Random.Range(recoilYRotRange.x, recoilYRotRange.y),
            Random.Range(recoilZRotRange.x, recoilZRotRange.y)
        );

        // Generate random recoil for position
        targetPosition += new Vector3(
            Random.Range(recoilXPosRange.x, recoilXPosRange.y),
            Random.Range(recoilYPosRange.x, recoilYPosRange.y),
            Random.Range(recoilZPosRange.x, recoilZPosRange.y)
        );
    }
}