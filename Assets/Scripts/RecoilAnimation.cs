using UnityEngine;
using UnityEngine.Serialization;

public class RecoilAnimation : MonoBehaviour
{
    //Rotations
    Vector3 targetRotation;
    public Vector3 recoilRotation;
    
    [Header("Recoil Settings")]
    [SerializeField] private float snappiness;
    [SerializeField] private float returnSpeed;
    
    [Header("Recoil Position Settings")]
    [SerializeField] private float recoilXPos;
    [SerializeField] private float recoilYPos;
    
    

    [Header("Recoil Rotation Settings")]
    [SerializeField] private float recoilXRot;
    [SerializeField] private float recoilYRot;
    [SerializeField] private float recoilZRot;
    


    void Update()
    {
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        recoilRotation = Vector3.Slerp(recoilRotation, targetRotation, snappiness * Time.fixedDeltaTime);
    }

    public void GenerateRecoil()
    {
        targetRotation += new Vector3(-recoilXRot, Random.Range(-recoilYRot,recoilYRot), Random.Range(-recoilZRot, recoilZRot));
    }
}