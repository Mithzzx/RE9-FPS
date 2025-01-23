using UnityEngine;

public class RecoilAnimation : MonoBehaviour
{
    // Rotations
    private Vector3 targetRotation;
    [HideInInspector] public Vector3 currentRotation;

    // Positions
    private Vector3 targetPosition;
    [HideInInspector] public Vector3 currentPosition;

    [Header("Settings")]
    [SerializeField] private float snappiness;
    [SerializeField] private float returnSpeed;
    
    [Header("Position")]
    [SerializeField] private float recoilXPos;
    [SerializeField] private float recoilYPos;
    [SerializeField] private float recoilZPos;
    [Header("Rotation")]
    [SerializeField] private float recoilXRot;
    [SerializeField] private float recoilYRot;
    [SerializeField] private float recoilZRot;


    void Update()
    {
        // Smoothly interpolate rotation back to zero
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.fixedDeltaTime);

        // Smoothly interpolate position back to zero
        targetPosition = Vector3.Lerp(targetPosition, Vector3.zero, returnSpeed * Time.deltaTime);
        currentPosition = Vector3.Lerp(currentPosition, targetPosition, snappiness * Time.deltaTime);
    }

    public void GenerateRecoil()
    {
        // Generate random recoil for rotation
        targetRotation += new Vector3(-recoilXRot, Random.Range(-recoilYRot, recoilYRot), Random.Range(-recoilZRot, recoilZRot));

        // Generate random recoil for position
        targetPosition += new Vector3(Random.Range(-recoilXPos, recoilXPos), Random.Range(0, recoilYPos), Random.Range(-recoilZPos, 0));
    }
}