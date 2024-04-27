using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Bullet Holes")]
    [SerializeField] GameObject bulletHole;

    [Header("Reference")]
    [SerializeField] InputControler input;
    [SerializeField] Camera fpscam;
    [SerializeField] ParticleSystem[] muzzelFlashs;
    [SerializeField] Light flashLight;
    RaycastHit hit;

    private void Update()
    {
        if(input.Attack())
        {
            ToggleMuzzleFlash(true);
            if (canshoot) StartCoroutine(Fire());
        }
        else ToggleMuzzleFlash(false);
        
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
            Debug.Log("hooting");
            GameObject bh = Instantiate(bulletHole, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(bh, 4);
        }

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
