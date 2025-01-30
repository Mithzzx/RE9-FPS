using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Spawn decals on Unity Particle Collision event
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class DecalsOnParticleCollision : MonoBehaviour, ISpawnerContainer
    {
        /// <summary>
        /// Collision events buffer
        /// </summary>
        public List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
        /// <summary>
        /// Decal prefab
        /// </summary>
        public GameObject DecalPrefab;
        /// <summary>
        /// Decals prefabs array
        /// </summary>
        public GameObject[] DecalPrefabs;
        /// <summary>
        /// True - get random prefab from array when need spawn decal, false - DecalPrefab
        /// </summary>
        public bool randomPrefab = false;
        /// <summary>
        /// Set random rotation for decal instance
        /// </summary>
        public bool randomRotation = true;
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
        /// Duration of curve (timer will started from OnEnable event)
        /// </summary>
        public float duration = 1f;
        /// <summary>
        /// Scale curve over time
        /// </summary>
        public AnimationCurve scaleOverTime = AnimationCurve.Constant(0, 1, 1);
        /// <summary>
        /// Destroy delay for decals instances
        /// </summary>
        public float destroyDelay = 20f;

        private ParticleSystem system;

        /// <summary>
        /// ISpawner.Instantiate is used instead of Object.Instantiate
        /// </summary>
        public ISpawner Spawner
        {
            get
            {
                return _spawner;
            }
            set
            {
                spawner = value as MonoBehaviour;
                _spawner = value;
            }
        }

        private ISpawner _spawner;

        private float enabledTime = 0;
        private float scale;

        private void Awake()
        {
            system = GetComponent<ParticleSystem>();
            if (spawner != null)
                Spawner = spawner as ISpawner;
        }

        private void OnEnable()
        {
            enabledTime = Time.time;
        }

        private void Update()
        {
            float fraction = (Time.time - enabledTime) / duration;
            scale = scaleOverTime.Evaluate(fraction);
        }

        private void OnParticleCollision(GameObject other)
        {
            int numCollisionEvents = system.GetCollisionEvents(other, collisionEvents);

            for (int i = 0; i < numCollisionEvents; i++)
            {
                if (!ShouldSpawn(collisionEvents[i]))
                    continue;

                Vector3 pos = collisionEvents[i].intersection;
                Vector3 normal = collisionEvents[i].normal;

                GameObject prefab = DecalPrefab;
                if (randomPrefab)
                {
                    prefab = DecalPrefabs[Random.Range(0, DecalPrefabs.Length)];
                }

                Quaternion rotation = Quaternion.identity;
                if (randomRotation)
                {
                    rotation = Quaternion.AngleAxis(Random.Range(0f, 360f), normal) * Quaternion.LookRotation(-normal);
                }
                else
                {
                    rotation = Quaternion.LookRotation(-normal, Vector3.up);
                }

                GameObject decal;

                if (Spawner == null)
                {
                    decal = Instantiate(prefab, pos + normal * 0.0002f, rotation);
                }
                else
                {
                    decal = Spawner.Instantiate(prefab, pos + normal * 0.0002f, rotation);
                }

                decal.transform.localScale *= Random.Range(minScale, maxScale) * scale;
                Destroy(decal, destroyDelay);
            }
        }

        protected virtual bool ShouldSpawn(ParticleCollisionEvent particleCollisionEvent)
        {
            return true;
        }
    }
}