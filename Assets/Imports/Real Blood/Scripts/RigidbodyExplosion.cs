using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Knife.RealBlood
{
    /// <summary>
    /// Behaviour that apply explosion forces (or random) to rigidbodies list OnEnable event
    /// </summary>
    public class RigidbodyExplosion : MonoBehaviour, IResettable
    {
        [SerializeField] private bool explodeAllChildren = true;
        [SerializeField] private bool randomDirection = false;
        [SerializeField] private float randomPointMin = 0f;
        [SerializeField] private float randomPointMax = 0.2f;
        [SerializeField] private float minForce = 10;
        [SerializeField] private float maxForce = 20;
        [SerializeField] private float minTorque = 0;
        [SerializeField] private float maxTorque = 0;
        [SerializeField] private Transform explosionPoint;
        [SerializeField] private float explosionRadius = 1f;
        [SerializeField] private float enableDelay = 0f;

        private List<TransformParameters> transformParameters = new List<TransformParameters>();
        [SerializeField]
        private List<Rigidbody> bodies = new List<Rigidbody>();

        private bool isInitialized = false;

        public void Initialize()
        {
            if (isInitialized)
                return;

            if (explodeAllChildren)
            {
                bodies.AddRange(GetComponentsInChildren<Rigidbody>());
            }

            foreach (var b in bodies)
            {
                transformParameters.Add(new TransformParameters(b.transform));
                b.gameObject.SetActive(false);
            }

            if (explosionPoint == null)
                explosionPoint = transform;

            isInitialized = true;
        }

        private void OnEnable()
        {
            Initialize();
            StartCoroutine(DelayedExplode());
        }

        private IEnumerator DelayedExplode()
        {
            CancelExplode();
            foreach (var b in bodies)
            {
                b.isKinematic = true;
                b.gameObject.SetActive(false);
            }
            yield return new WaitForSeconds(enableDelay);
            foreach (var b in bodies)
            {
                b.isKinematic = false;
                b.transform.SetParent(null);
                b.gameObject.SetActive(true);
            }
            Explode();
        }

        private void OnDisable()
        {
            if (this == null)
                return;

            // we can't use SetParent when object are deactivating
            //CancelExplode();
        }

        private void CancelExplode()
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                transformParameters[i].Apply(bodies[i].transform);
                bodies[i].gameObject.SetActive(false);
            }
        }

        public void Explode()
        {
            if (randomDirection)
            {
                foreach (var b in bodies)
                {
                    b.gameObject.SetActive(true);
                    b.AddForce(Random.Range(minForce, maxForce) * Random.onUnitSphere);
                    b.AddTorque(Random.onUnitSphere * Random.Range(minTorque, maxTorque));
                }
            }
            else
            {
                Vector3 randomOffset = Random.onUnitSphere * Random.Range(randomPointMin, randomPointMax);
                foreach (var b in bodies)
                {
                    b.gameObject.SetActive(true);
                    b.AddExplosionForce(UnityEngine.Random.Range(minForce, maxForce), explosionPoint.position + randomOffset, explosionRadius);
                    b.AddTorque(Random.onUnitSphere * Random.Range(minTorque, maxTorque));
                }
            }
        }

        public void ResetComponent()
        {
            CancelExplode();
        }

        private class TransformParameters
        {
            private Transform parent;
            private Vector3 localPosition;
            private Quaternion localRotation;
            private Vector3 localScale;

            public TransformParameters(Transform t)
            {
                parent = t.parent;
                localPosition = t.localPosition;
                localRotation = t.localRotation;
                localScale = t.localScale;
            }

            public void Apply(Transform t)
            {
                t.SetParent(parent);
                t.localPosition = localPosition;
                t.localRotation = localRotation;
                t.localScale = localScale;
            }
        }
    }
}