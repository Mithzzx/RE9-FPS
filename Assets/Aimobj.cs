using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Aimobj : MonoBehaviour
{
    [SerializeField] GameObject toAim;

    void Update()
    {
        transform.position = toAim.transform.position;
    }
}
