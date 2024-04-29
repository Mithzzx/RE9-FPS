using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recoil : MonoBehaviour
{
    //Rotations
    Vector3 targetRotation;
    Vector3 currentRotation;

    //Script
    [SerializeField] GunSystem gun;

    //Hipfire Recoil
    [SerializeField] float recoilX;
    [SerializeField] float recoilY;
    [SerializeField] float recoilZ;

    //Settings
    [SerializeField] float snappiness;
    [SerializeField] float returnSpeed;

    //AimRecoil
    [Header("Aim Recoil")]
    [SerializeField] float aimrecoilX;
    [SerializeField] float aimrecoilY;
    [SerializeField] float aimrecoilZ;


    void Update()
    {
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.fixedDeltaTime);
        transform.localRotation = Quaternion.Euler(currentRotation);
    }

    public void RecoilFire()
    {
        if(gun.isaiming) targetRotation += new Vector3(aimrecoilX, Random.Range(-aimrecoilY,aimrecoilY), Random.Range(-aimrecoilZ, aimrecoilZ));
        else targetRotation += new Vector3(recoilX, Random.Range(-recoilY, recoilY), Random.Range(-recoilZ, recoilZ));
    }

}
