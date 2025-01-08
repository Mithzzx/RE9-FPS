 using System;
 using UnityEngine;

public class GrapplingRope : MonoBehaviour
{
    [Header("References")] 
    private Spring spring;
    [SerializeField] private Transform gunTip;
    [SerializeField] private LineRenderer lr;
    [SerializeField] private Grappling grappling;
    
    [Header("settings")]
    [SerializeField] private int quality;
    [SerializeField] private float damper;
    [SerializeField] private float strength;
    [SerializeField] private float velocity;
    [SerializeField] private float waveCount;
    [SerializeField] private float waveHeight;
    [SerializeField] private AnimationCurve affectCurve;

    private void Awake()
    {
        spring = new Spring();
        spring.SetTarget(0);
    }

    private void LateUpdate()
    {
        if(grappling.grappling)
            lr.SetPosition(0,gunTip.position);
    }
}
