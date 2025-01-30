using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// InstancedRendererColorApplier value randomizer
    /// </summary>
    [RequireComponent(typeof(InstancedRendererColorApplier))]
    [ExecuteAlways]
    public class InstancedRendererColorApplierRandomizer : MonoBehaviour
    {
        [SerializeField] private Color color1 = Color.white;
        [SerializeField] private Color color2 = Color.red;
        [SerializeField] private int colorIndex;

        private InstancedRendererColorApplier colorApplier;

        private void Awake()
        {
            colorApplier = GetComponent<InstancedRendererColorApplier>();
        }

        private void OnValidate()
        {
            if (this == null)
                return;

            colorApplier = GetComponent<InstancedRendererColorApplier>();
            UpdateColor();
        }

        private void OnEnable()
        {
            colorApplier = GetComponent<InstancedRendererColorApplier>();
            UpdateColor();
        }

        private void UpdateColor()
        {
            Vector3 position = transform.localPosition;

            System.Random random = new System.Random((int)((position.x + position.y * position.y + position.z * position.z * position.z) * 1000));

            colorApplier.SetValue(colorIndex, Color.Lerp(color1, color2, (float)random.NextDouble()));
        }
    }
}