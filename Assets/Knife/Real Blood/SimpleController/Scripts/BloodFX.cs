using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood.SimpleController
{
    /// <summary>
    /// FPSDamageFX behaviour to spawn random blood FX on damage
    /// </summary>
    public class BloodFX : FPSDamageFX
    {
        [SerializeField] private GameObject[] damagePrefabs;
        [SerializeField] private Transform damageRoot;
        [SerializeField] private int prewarmCount = 10;
        [SerializeField] private int minCount = 1;
        [SerializeField] private int maxCount = 1;
        [SerializeField] private float destroyDelay = 10f;
        [SerializeField] private Vector2 randomPosition;
        [SerializeField] private Vector2 randomPositionMul = new Vector2(1, 1);
        [SerializeField] private float minRandomScale = 0.8f;
        [SerializeField] private float maxRandomScale = 1.2f;
        [SerializeField] private float minimumIntervalBeforeNext = 0.3f;

        private Queue<GameObject>[] pools;
        private float lastSpawnTime;

        private void Start()
        {
            pools = new Queue<GameObject>[damagePrefabs.Length];

            for (int i = 0; i < pools.Length; i++)
            {
                pools[i] = new Queue<GameObject>();
                for (int j = 0; j < prewarmCount; j++)
                {
                    var instance = Instantiate(damagePrefabs[i], damageRoot);
                    instance.SetActive(false);
                    pools[i].Enqueue(instance);
                }
            }
        }

        private GameObject RandomSpawn()
        {
            int poolIndex = Random.Range(0, pools.Length);

            var pool = pools[poolIndex];
            GameObject instance = null;
            if (pool.Count == 0)
            {
                instance = Instantiate(damagePrefabs[poolIndex], damageRoot);
                pools[poolIndex].Enqueue(instance);
            }

            instance = pool.Dequeue();
            instance.SetActive(true);

            StartCoroutine(DelayedDestroy(instance, poolIndex));

            return instance;
        }

        private IEnumerator DelayedDestroy(GameObject instance, int poolIndex)
        {
            yield return new WaitForSeconds(destroyDelay);
            instance.SetActive(false);
            pools[poolIndex].Enqueue(instance);
        }

        public override void PlayFX(DamageData[] damage)
        {
            if (Time.time < lastSpawnTime + minimumIntervalBeforeNext)
                return;

            lastSpawnTime = Time.time;

            int count = Random.Range(minCount, maxCount);

            for (int i = 0; i < count; i++)
            {
                var instance = RandomSpawn();

                Vector2 randomDirection = Random.insideUnitCircle.normalized;

                //Vector2 randomOffset = new Vector2(Random.Range(-randomPosition.x, randomPosition.x), Random.Range(-randomPosition.y, randomPosition.y));

                randomDirection.x *= randomPositionMul.x;
                randomDirection.y *= randomPositionMul.y;

                Vector2 randomOffset = randomDirection * Random.Range(randomPosition.x, randomPosition.y);

                Vector3 position = instance.transform.localPosition;
                position.x = randomOffset.x;
                position.y = randomOffset.y;
                instance.transform.localPosition = position;

                float randomScale = Random.Range(minRandomScale, maxRandomScale);

                Vector3 scale = instance.transform.localScale;

                scale.x = randomScale;
                scale.y = randomScale;
                scale.z = randomScale;

                instance.transform.localScale = scale;
            }
        }
    }
}