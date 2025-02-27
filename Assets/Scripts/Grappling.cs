using UnityEngine;

public class Grappling : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private PlayerMovement pm;
    [SerializeField] private InputHandler input;
    [SerializeField] private Swinging sd;
    [SerializeField] private Transform playerCam;
    [SerializeField] private Transform gunTip;
    [SerializeField] private LayerMask whatIsGrappleable;
    [SerializeField] private LineRenderer lr;

    [Header("Grappling")] 
    [SerializeField] private bool canGrapple = true;
    [SerializeField] private float grappleDelayTime = 0.1f;
    [SerializeField] private float overshootYAxis;
    
    private Vector3 grapplePoint;
    
    [Header("Cooldowns")]
    [SerializeField] private float grappleCooldown = 1f;
    [SerializeField] private float grappleCooldownTimer;
    
    public bool grappling;

    private void Start()
    {
        pm = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (canGrapple && input.GrappleTriggered) StartGrapple();

        if (grappleCooldownTimer > 0) grappleCooldownTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        if(grappling)
            lr.SetPosition(0,gunTip.position);
    }

    private void StartGrapple()
    {
        pm.freeze = true;
        
        if (grappleCooldownTimer > 0) return;

        grappling = true;
        
        
        grapplePoint = sd.predictionPoint.position;
            
        Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        
        lr.enabled = true;
        lr.SetPosition(1,grapplePoint);
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
        
        grappleCooldownTimer = grappleCooldown;

        lr.enabled = false;
    }
    
    
}
