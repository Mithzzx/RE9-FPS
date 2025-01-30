using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Knife.RealBlood
{
    /// <summary>
    /// This behaviour spawn footsteps (for FPS character)
    /// </summary>
    public class FeetDecalSpawner : MonoBehaviour
    {
        [SerializeField] private float width = 0.2f;
        [SerializeField] private float spawnOverDistance = 2f;
        [SerializeField] private float spawnOverAngle = 45f;
        [SerializeField] private float randomOffsetOnAngleSpawn = 0.05f;

        [SerializeField] private GameObject leftFootDecalTemplate;
        [SerializeField] private GameObject rightFootDecalTemplate;

        [SerializeField] private Transform leftRaycastPoint;
        [SerializeField] private Transform rightRaycastPoint;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float raycastHeight = 0.2f;
        [SerializeField] private float raycastLength = 0.3f;
        [SerializeField] private float threshold = 0.002f;
        [SerializeField] private float energyPerSpawn = 5f;
        [SerializeField] private float maxEnergy = 100f;

        private float distance;
        private float angularDistance = 0;

        private Vector3 oldPosition;
        private float oldYAngle = 0;
        private bool currentRight;

        private float energy;

        /// <summary>
        /// Energy amount (every decal spawn consume energy)
        /// </summary>
        public float Energy
        {
            get
            {
                return energy;
            }
            set
            {
                energy = value;
                energy = Mathf.Clamp(energy, 0, maxEnergy);
            }
        }

        private MaterialPropertyBlock materialPropertyBlock;

        private void Start()
        {
            materialPropertyBlock = new MaterialPropertyBlock();
        }

        private void OnValidate()
        {
            if (leftRaycastPoint != null)
                leftRaycastPoint.transform.localPosition = Vector2.left * width / 2f;

            if (rightRaycastPoint != null)
                rightRaycastPoint.transform.localPosition = Vector2.right * width / 2f;
        }

        private void Update()
        {
            if (Energy < energyPerSpawn)
                return;

            Vector3 delta = transform.position - oldPosition;
            float angleDelta = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, oldYAngle));

            distance += delta.magnitude;
            angularDistance += angleDelta;

            if (distance >= spawnOverDistance)
            {
                Spawn(0);
            }
            else if (angularDistance >= spawnOverAngle)
            {
                Spawn(Random.Range(0, randomOffsetOnAngleSpawn));
            }

            oldPosition = transform.position;
            oldYAngle = transform.eulerAngles.y;
        }

        private void Spawn(float randomOffset)
        {
            distance = 0;
            angularDistance = 0;

            GameObject prefab = null;
            Transform raycastPoint = null;

            if (currentRight)
            {
                prefab = rightFootDecalTemplate;
                raycastPoint = rightRaycastPoint;
            }
            else
            {
                prefab = leftFootDecalTemplate;
                raycastPoint = leftRaycastPoint;
            }

            RaycastHit hitInfo;
            bool hasGround = GetPosition(raycastPoint, out hitInfo);

            if (hasGround)
            {
                Vector2 randomDirection = Random.insideUnitCircle.normalized * randomOffset;

                Vector3 offset;
                offset.x = randomDirection.x;
                offset.y = 0;
                offset.z = randomDirection.y;

                var instance = Instantiate(prefab);
                instance.SetActive(true);
                instance.transform.position = hitInfo.point + hitInfo.normal * threshold + offset;
                Quaternion rotation = Quaternion.LookRotation(-hitInfo.normal, transform.forward);
                instance.transform.rotation = rotation;

                var r = instance.GetComponent<Renderer>();
                r.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetFloat("_Alpha", Energy / maxEnergy);
                r.SetPropertyBlock(materialPropertyBlock);

                Destroy(instance, 10f);

                currentRight = !currentRight;
                Energy -= energyPerSpawn;
            }
        }

        private bool GetPosition(Transform raycastPoint, out RaycastHit hitInfo)
        {
            Ray r = new Ray(raycastPoint.position + Vector3.up * raycastHeight, Vector3.down);
            if (Physics.Raycast(r, out hitInfo, raycastHeight + threshold + raycastLength, groundMask))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}