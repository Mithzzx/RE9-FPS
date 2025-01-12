using System;
using UnityEngine;

public class GrappleGun : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private Grappling grapplingScript;
    [SerializeField] private Swinging swingingScript;

    private void OnEnable()
    {
        grapplingScript.enabled = true;
        swingingScript.enabled = true;
    }

    private void OnDisable()
    {
        grapplingScript.enabled = false;
        swingingScript.enabled = false;
    }
}
