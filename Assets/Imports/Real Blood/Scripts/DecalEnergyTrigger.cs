using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Provides energy to FeetDecalSpawner on Unity TriggerEnter event
    /// </summary>
    public class DecalEnergyTrigger : MonoBehaviour
    {
        [SerializeField] private float energyAmount = 100f;

        private void OnTriggerEnter(Collider collision)
        {
            FeetDecalSpawner feetDecalSpawner = collision.GetComponentInChildren<FeetDecalSpawner>();

            if (feetDecalSpawner != null)
            {
                feetDecalSpawner.Energy = energyAmount;
            }
        }
    }
}