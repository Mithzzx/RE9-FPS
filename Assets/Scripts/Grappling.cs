using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Grappling : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private MovementController pm;
    [SerializeField] private InputHandler input;
    [SerializeField] public Transform cam;
    [SerializeField] public Transform gunTip;
    [SerializeField] public LineRenderer lr;
    [SerializeField] public LayerMask whatIsGrappleable;
    
    
    [Header("Grappling Settings")]
    [SerializeField] private float maxGrappleDistance = 100f;
    [FormerlySerializedAs("grappleDelayTimer")] [SerializeField] private float grappleDelayTime = 1f;
    [SerializeField] private float overshootYAxis;
    [SerializeField] public bool canGrapple;
    
    private Vector3 grapplePoint;
    
    [Header("Cooldowns")]
    [SerializeField] public float grapplingCd;
    [SerializeField] private float grapplingCdTimer;
    
    private bool grappling;
    
    private void Start()
    {
        pm = GetComponent<MovementController>();
        input = GetComponent<InputHandler>();
    }

    private void Update()
    {
        if (canGrapple && input.AttackTriggered) StartGrapple();
        if (grapplingCdTimer>0) grapplingCdTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
         if (grappling)
            lr.SetPosition(0, gunTip.position);
    }

    private void StartGrapple()
    {
        if (grapplingCdTimer > 0) return;

        grappling = true;

        pm.freeze = true;

        RaycastHit hit;
        if(Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;

            Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        }
        else
        {
            grapplePoint = cam.position + cam.forward * maxGrappleDistance;

            Invoke(nameof(StopGrapple), grappleDelayTime);
        }

        lr.enabled = true;
        lr.SetPosition(1, grapplePoint);
    }

    private void ExecuteGrapple()
    {
        pm.freeze = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        pm.JumpToPosition(grapplePoint, highestPointOnArc);

        Invoke(nameof(StopGrapple), 1f);
    }

    public void StopGrapple()
    {
        pm.freeze = false;

        grappling = false;

        grapplingCdTimer = grapplingCd;

        lr.enabled = false;
    }
}
