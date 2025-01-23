using UnityEngine;

public class RecoilAnimation : MonoBehaviour
{
    //Rotations
    public Vector3 targetRotation;
    Vector3 currentRotation;

    [Header("Settings")]
    [SerializeField] float recoilX;
    [SerializeField] float recoilY;
    [SerializeField] float recoilZ;
    [SerializeField] float snappiness;
    [SerializeField] float returnSpeed;


    void Update()
    {
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.fixedDeltaTime);
        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    public void GenerateRecoil()
    {
        targetRotation += new Vector3(-recoilX, Random.Range(-recoilY,recoilY), Random.Range(-recoilZ, recoilZ));
    }
}