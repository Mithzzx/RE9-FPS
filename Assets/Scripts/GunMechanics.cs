using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class GunMechanics : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private InputHandler input;
    [SerializeField] private RecoilAnimation recoilAnimation;
    [SerializeField] private GunsDemo gunsDemo;
    [SerializeField] private Camera fpsCam;
    [SerializeField] private PlayerCam mainCam;
    [SerializeField] private Transform muzzle;
    private RaycastHit hit;
    
    [Header("Characteristics")]
    [SerializeField] public float damage;
    [SerializeField] private float impactForce = 30f;
    [SerializeField] private float timeBetweenShooting;
    [SerializeField] private float range;
    [SerializeField] private float spread;
    [SerializeField] private float reloadTime;
    [SerializeField] private float timeBetweenShots;

    [SerializeField] private int magazineSize;
    [SerializeField] private int bulletsPerTap;
    [SerializeField] private bool allowButtonHold;

    [SerializeField] private int bulletsLeft;
    [SerializeField] private int bulletsShot;

    private bool shooting;
    private bool canShoot;
    private bool reloading;

    [Header("Muzzle Flash")] 
    [SerializeField] private GameObject muzzleFlash;

    [Header("Recoil")]
    [Range(0, 7f)] public float recoilAmountY = 5.14f;
    [Range(0, 3f)] public float recoilAmountX = 1.48f;
    [SerializeField] private float maxRecoilTime = 4;
    private float currentRecoilXPos;
    private float currentRecoilYPos;
    private float timePressed;
    

    private void Awake()
    {
        bulletsLeft = magazineSize;
        canShoot = true;
    }

    private void Start()
    {
        gunsDemo = GetComponentInParent<GunsDemo>();
    }

    private void Update()
    {
        shooting = allowButtonHold ? input.AttackHeld : // holding the attack button
            input.AttackTriggered; // triggering the attack button

        if (input.ReloadTriggered && bulletsLeft < magazineSize && !reloading) // Assuming ReloadTriggered is a property in InputHandler for triggering the reload action
        {
            StartCoroutine(Reload());
        }

        if (shooting)
        {
            //Calculating how long firing
            timePressed += Time.deltaTime;
            timePressed = timePressed >= maxRecoilTime? maxRecoilTime : timePressed;
        }
        else
        {
            //Resetting timePressed
            timePressed = 0;
        }

        if (shooting && canShoot && !reloading && bulletsLeft > 0)
        {
            bulletsShot = bulletsPerTap;
            //Creating Muzzle Flash
            GameObject muzzleFlashInstance = Instantiate(muzzleFlash, muzzle.position, muzzle.rotation);
            Destroy(muzzleFlashInstance, 4);
            
            Fire();
        }
    }

    private void Fire()
    {
        // Disabling Firing
        canShoot = false;

        // Recoil
        RecoilMath();
        
        // RecoilAnimation
        recoilAnimation.GenerateRecoil();
        
        // Spread
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);
        

        // Calculating direction with spread
        Vector3 direction = fpsCam.transform.forward + new Vector3(x, y, 0);

        // Creating Bullet
        if (Physics.Raycast(fpsCam.transform.position, direction, out hit, range))
        {
            //bullet hit effect
            string hitTag = hit.collider.gameObject.tag;
            GameObject bulletHoleInstance = Instantiate(gunsDemo.GetBulletHole(hitTag), hit.point, new Quaternion());
            bulletHoleInstance.transform.LookAt(hit.point + hit.normal);
            Destroy(bulletHoleInstance, 20);
            
            //Adding force to the object
            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForce(direction * impactForce, ForceMode.Impulse);
            }
            
            //hit enemy
            var hitBox = hit.collider.GetComponent<HitBox>();
            if (hitBox) hitBox.OnRaycastHit(this, direction);
        }

        // Counting bullets
        bulletsLeft--;
        bulletsShot--;

        // Start delay
        StartCoroutine(ResetShotCoroutine());
        if (bulletsShot > 0 && bulletsLeft > 0)
        {
            StartCoroutine(FireCoroutine());
        }
    }

    private IEnumerator ResetShotCoroutine()
    {
        yield return new WaitForSeconds(timeBetweenShooting);
        canShoot = true;
    }

    private IEnumerator FireCoroutine()
    {
        yield return new WaitForSeconds(timeBetweenShots);
        Fire();
    }

    private IEnumerator Reload()
    {
        reloading = true;
        yield return new WaitForSeconds(reloadTime);
        bulletsLeft = magazineSize;
        reloading = false;
    }

    private void RecoilMath()
    {
        currentRecoilXPos = ((Random.Range(-1f,1f) * 0.5f) / 2) * recoilAmountX;
        currentRecoilYPos = ((Random.value * 0.5f) / 2) * (timePressed < maxRecoilTime ? recoilAmountY : (recoilAmountY/4));
        mainCam.xRotation -= currentRecoilYPos;
        mainCam.yRotation -= currentRecoilXPos;
    }
}