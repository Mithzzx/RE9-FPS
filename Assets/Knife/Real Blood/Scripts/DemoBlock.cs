using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Knife.RealBlood
{
    /// <summary>
    /// Helper behaviour for showcase
    /// </summary>
    public class DemoBlock : MonoBehaviour
    {
        [SerializeField] private TextMeshPro titleLabel;
        [SerializeField] private TextMeshPro indexLabel;
        [SerializeField] private Transform demoPoint;
        [SerializeField] private GameObject prefab;
        [SerializeField] private string title = "EXPLOSION";

        private GameObject spawnedPrefab;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (this == null)
                return;

            if (titleLabel != null)
                titleLabel.text = title;

            if (indexLabel != null)
                indexLabel.text = (transform.GetSiblingIndex() + 1).ToString("D2");
        }

        private void OnEnable()
        {
            if (indexLabel != null)
                indexLabel.text = (transform.GetSiblingIndex() + 1).ToString("D2");
        }

        [ContextMenu("Update")]
        private void UpdateSettings()
        {
            if (Application.isPlaying)
                return;

            if (this == null)
                return;

            if (titleLabel != null)
                titleLabel.text = title;

            if (demoPoint.childCount > 0)
                spawnedPrefab = demoPoint.GetChild(0).gameObject;
            else
                spawnedPrefab = null;

            if (prefab != null)
            {
                if (spawnedPrefab == null)
                {
                    // no spawned prefab, so we need spawn

                    SpawnPrefab();
                }
                else
                {
                    if (!PrefabUtility.GetPrefabInstanceHandle(spawnedPrefab).Equals(prefab)) // check that prefab is new
                    {
                        DestroyImmediate(spawnedPrefab);

                        SpawnPrefab();
                    }
                    else
                    {
                        // we won't respawn object if prefab are same
                    }
                }
            }
            else
            {
                if (spawnedPrefab != null)
                    DestroyImmediate(spawnedPrefab);
            }
        }

        private void SpawnPrefab()
        {
            spawnedPrefab = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            spawnedPrefab.transform.SetParent(demoPoint);
            spawnedPrefab.transform.localPosition = prefab.transform.localPosition;
            spawnedPrefab.transform.localRotation = prefab.transform.localRotation;
            spawnedPrefab.transform.localScale = prefab.transform.localScale;
        }
#endif
    }
}