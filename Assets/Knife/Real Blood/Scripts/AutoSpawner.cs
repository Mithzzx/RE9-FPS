using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// This class provide spawning of prefab instances with interval
    /// </summary>
    public class AutoSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private float interval = 3f;
        [SerializeField] private float destroyDelay = 5f;

        /// <summary>
        /// Spawn interval (seconds)
        /// </summary>
        public float Interval { get => interval; set => interval = value; }
        /// <summary>
        /// Is spawning paused
        /// </summary>
        public bool IsPause { get => isPause; set => isPause = value; }

        private bool isPause = false;

        private void OnEnable()
        {
            StartCoroutine(Spawn());
        }

        private IEnumerator Spawn()
        {
            while (true)
            {
                if (IsPause)
                {
                    yield return null;
                    continue;
                }

                var instance = Instantiate(prefab, transform.position, transform.rotation);

                Destroy(instance, destroyDelay);

                yield return new WaitForSeconds(Interval);
            }
        }
    }
}