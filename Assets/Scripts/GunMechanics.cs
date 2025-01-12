using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class GunMechanics : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] InputHandler input;
    [SerializeField] Camera fpsCam;
    [SerializeField] private Transform muzzle;
    RaycastHit hit;
    
    [Header("Characteristics")]
    [SerializeField] float damage;
    [SerializeField] float timeBetweenShooting;
    [SerializeField] float range;
    [SerializeField] float spread;
    [SerializeField] float reloadTime;
    [FormerlySerializedAs("timeBetweenShooting")] [SerializeField] float timeBetweenShots;

    [SerializeField] int magazineSize;
    [SerializeField] int bulletsPerTap;
    [SerializeField] bool allowButtonHold;

    [SerializeField] int bulletsLeft;
    [SerializeField] int bulletsShot;

    private bool shooting;
    private bool canShoot;
    private bool reloading;

    [Header("Bullet Holes")]
    [SerializeField] GameObject bulletHole;

    [Header("Muzzle Flash")] 
    [SerializeField] private GameObject muzzleFlash;

    private void Awake()
    {
        bulletsLeft = magazineSize;
        canShoot = true;
    }

    private void Update()
    {
        shooting = allowButtonHold ? input.AttackHeld : // holding the attack button
            input.AttackTriggered; // triggering the attack button

        if (input.ReloadTriggered && bulletsLeft < magazineSize && !reloading) // Assuming ReloadTriggered is a property in InputHandler for triggering the reload action
        {
            StartCoroutine(Reload());
        }

        if (shooting && canShoot && !reloading && bulletsLeft > 0)
        {
            bulletsShot = bulletsPerTap;
            //Creating Muzzle Flash
            GameObject muzzleFlashInstance = Instantiate(muzzleFlash, muzzle.position, muzzle.rotation) as GameObject;
            Destroy(muzzleFlashInstance, 4);

            Fire();
        }
    }

    private void Fire()
    {
        //Disabling Firing
        canShoot = false;
        
        //spread
        float x = UnityEngine.Random.Range(-spread, spread);
        float y = UnityEngine.Random.Range(-spread, spread);
        
        //Calculating direction with spread
        Vector3 direction = fpsCam.transform.forward + new Vector3(x, y, 0);

        //creating Bullet
        if (Physics.Raycast(fpsCam.transform.position,direction,out hit,range))
        {
            GameObject bulletHoleInstance = Instantiate(bulletHole, hit.point, new Quaternion()) as GameObject;
            bulletHoleInstance.transform.LookAt(hit.point + hit.normal);
            Destroy(bulletHoleInstance, 20);
        }
        
        //counting bullets
        bulletsLeft--;
        bulletsShot--;
        
        //StartDelay
        Invoke(nameof(ResetShot), timeBetweenShooting);
        if (bulletsShot > 0 && bulletsLeft > 0)
        {
            Invoke(nameof(Fire), timeBetweenShots);
        }
    }

    private void ResetShot()
    {
        canShoot = true;
    }

    IEnumerator Reload()
    {
        reloading = true;
        Debug.Log("Reloading");
        yield return new WaitForSeconds(reloadTime);
        bulletsLeft = magazineSize;
        reloading = false;
    }
}