using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Knife.RealBlood.LiquidSurface
{
    /// <summary>
    /// Simple GameObject pool that can create and destroy objects
    /// </summary>
    [Serializable]
    public class SimpleGameObjectPool
    {
        [SerializeField] private int poolSize = 100;
        [SerializeField] private GameObject template;

        private Queue<GameObject> pool = new Queue<GameObject>();

        public void Create()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject particle = Object.Instantiate(template, template.transform.parent);
                particle.SetActive(false);
                pool.Enqueue(particle);
            }
        }

        public GameObject GetObject()
        {
            return pool.Dequeue();
        }

        public void Destroy(MonoBehaviour coroutineRunner, GameObject particle, float delay)
        {
            coroutineRunner.StartCoroutine(DestroyWithDelay(particle, delay));
        }

        public IEnumerator DestroyWithDelay(GameObject particle, float delay)
        {
            yield return new WaitForSeconds(delay);
            particle.SetActive(false);
            particle.transform.SetParent(template.transform.parent);
            pool.Enqueue(particle);
        }
    }
}