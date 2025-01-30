using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Spawn decals on Unity Collision event
    /// </summary>
    public class DecalsOnCollision : MonoBehaviour, ISpawnerContainer
    {
        /// <summary>
        /// Contacts buffer
        /// </summary>
        public ContactPoint[] contacts = new ContactPoint[100];
        /// <summary>
        /// Prefab of decal
        /// </summary>
        public GameObject DecalPrefab;
        /// <summary>
        /// Spawner behaviour
        /// </summary>
        public MonoBehaviour spawner;
        /// <summary>
        /// Minimum random scale of decal instance
        /// </summary>
        public float minScale = 1f;
        /// <summary>
        /// Maximum random scale of decal instance
        /// </summary>
        public float maxScale = 1.5f;
        /// <summary>
        /// Maximum count of instances on one collision event
        /// </summary>
        public int maxCountPerCollisionEnter = 1;
        /// <summary>
        /// Spawn mask
        /// </summary>
        public LayerMask collisionMask;

        /// <summary>
        /// ISpawner.Instantiate is used instead of Object.Instantiate
        /// </summary>
        public ISpawner Spawner { get => spawner as ISpawner; set => spawner = value as MonoBehaviour; }

        private void OnCollisionEnter(Collision collision)
        {
            if (collisionMask == (collisionMask | (1 << collision.gameObject.layer)))
            {
                int numCollisionEvents = collision.GetContacts(contacts);

                int spawnCount = Mathf.Clamp(numCollisionEvents, 0, maxCountPerCollisionEnter);

                for (int i = 0; i < spawnCount; i++)
                {
                    Vector3 pos = contacts[i].point;
                    Vector3 normal = contacts[i].normal;

                    GameObject decal;

                    if (Spawner == null)
                    {
                        decal = Instantiate(DecalPrefab, pos + normal * 0.0002f, Quaternion.AngleAxis(Random.Range(0f, 360f), normal) * Quaternion.LookRotation(-normal));
                    }
                    else
                    {
                        decal = Spawner.Instantiate(DecalPrefab, pos + normal * 0.0002f, Quaternion.AngleAxis(Random.Range(0f, 360f), normal) * Quaternion.LookRotation(-normal));
                    }

                    decal.transform.localScale *= Random.Range(minScale, maxScale);
                    Destroy(decal, 20);
                }
            }
        }
    }
}