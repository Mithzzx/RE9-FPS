using System;
using System.Collections;
using System.Collections.Generic;
using Player;
using UnityEngine;

public class GunSystem : MonoBehaviour
{
    [Header("Charaterstics")]
    [SerializeField] float damage;
    [SerializeField] float rateOfFire;
    [SerializeField] float range;
    [SerializeField] float spread;
    [SerializeField] float reloadTime;
    [SerializeField] float timeBetweenShots;

    [SerializeField] int magaineSize;
    [SerializeField] int bullersPerTap;
    [SerializeField] bool allowButtonHold;

    [SerializeField] int bulletsLeft;
    [SerializeField] int bullersShot;

    bool shooting, canshoot = true, reloading;

    [Header("Aim")]
    [SerializeField] public bool isaiming;
    [SerializeField] float aimFOV = 45f;
    [SerializeField] float aimSpeed;
    float timeElapsed;
    [SerializeField] GameObject currentPositionObject;
    [SerializeField] Transform currentPosition;
    [SerializeField] Transform aimPosition;

    [Header("Bullet Holes")]
    [SerializeField] GameObject bulletHole;

    [Header("Reference")]
    [SerializeField] InputControler input;
    [SerializeField] Camera fpscam;
    [SerializeField] Recoil recoil;
    [SerializeField] ParticleSystem[] muzzelFlashs;
    [SerializeField] Light flashLight;
    RaycastHit hit;
    [SerializeField] float shootEff;


    private void Update()
    {
        if(input.Attack())
        {
            ToggleMuzzleFlash(true);
            if (canshoot) StartCoroutine(Fire());
        }
        else ToggleMuzzleFlash(false);

        if (input.Aim() && isaiming == false)
        {
            ToggleAim(aimFOV,currentPosition,aimPosition);
        }
        else if ((input.Aim()== false && isaiming))
        {
            ToggleAim(60, aimPosition, currentPosition);
        }
        
    }

    private void ToggleAim(float Fov,Transform Apos,Transform Bpos)
    {
        Debug.Log(Bpos.localPosition.ToString());
        if(timeElapsed<1)
        {
            fpscam.fieldOfView = Fov;

            timeElapsed += aimSpeed * Time.deltaTime;

            currentPositionObject.transform.localPosition = Vector3.Lerp(Apos.localPosition,
                Bpos.localPosition, timeElapsed);

            currentPositionObject.transform.localRotation = Quaternion.Slerp(Apos.localRotation,
                Bpos.localRotation, timeElapsed);
        }
        else
        {
            currentPositionObject.transform.localPosition = Bpos.localPosition;
            currentPositionObject.transform.localRotation = Bpos.localRotation;
            isaiming = !isaiming;
            timeElapsed = 0;
        }
    }

    private void ToggleMuzzleFlash(bool state)
    {
        foreach (ParticleSystem muzzelFlash in muzzelFlashs)
        {
            var emmisionModule = muzzelFlash.emission;
            emmisionModule.enabled = state;
        }
        flashLight.gameObject.SetActive(state);
    }

    IEnumerator Fire()
    {
        //Disabling Firing
        canshoot = false;

        //creating Bullet
        if (Physics.Raycast(fpscam.transform.position,fpscam.transform.forward,out hit,range))
        {
            GameObject bh = Instantiate(bulletHole, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(bh, 10);
        }

        //Recoil
        recoil.RecoilFire();

        //Recoil Animaton
        Vector3 currentPos = transform.localPosition;
        transform.localPosition = Vector3.Lerp(currentPos, currentPos + new Vector3(0, 0, 0.5f), shootEff*Time.deltaTime);

        //StartDelay
        StartCoroutine(FireRateHandeler());
        yield return null;
    }

    IEnumerator FireRateHandeler()
    {
        //Find out the time to delay , and enable firing
        float timeToDelay = 1 / rateOfFire;
        yield return new WaitForSeconds(timeToDelay);
        canshoot = true;
    }
}
