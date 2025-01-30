using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Helper behaviour collects all spawned objects to clear it in future by ResetComponent
    /// </summary>
    public class Spawner : MonoBehaviour, IResettable, ISpawner
    {
        [SerializeField] private Transform spawnerContainersRoot;

        private List<GameObject> spawnedObjects = new List<GameObject>();

        public GameObject Instantiate(GameObject template, Vector3 position, Quaternion rotation)
        {
            var instance = Object.Instantiate(template, position, rotation);

            spawnedObjects.Add(instance);

            return instance;
        }

        private void Awake()
        {
            var containers = spawnerContainersRoot.GetComponentsInChildren<ISpawnerContainer>(true);
            for (int i = 0; i < containers.Length; i++)
            {
                containers[i].Spawner = this;
            }
        }

        public void ResetComponent()
        {
            foreach (var g in spawnedObjects)
            {
                if (g != null)
                {
                    Destroy(g);
                }
            }

            spawnedObjects.Clear();
        }
    }
}