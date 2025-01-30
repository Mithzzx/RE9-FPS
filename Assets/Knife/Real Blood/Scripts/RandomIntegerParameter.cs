using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Behaviour that apply random integer to renderers OnEnable
    /// </summary>
    public class RandomIntegerParameter : MonoBehaviour
    {
        [SerializeField] private int minValue = 0;
        [SerializeField] private int maxValue = 3;
        [SerializeField] private string propertyName;
        [SerializeField] private Renderer[] targetRenderer;

        private MaterialPropertyBlock propertyBlock;
        private int propertyNameProp;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            propertyNameProp = Shader.PropertyToID(propertyName);
        }

        private void OnEnable()
        {
            for (int i = 0; i < targetRenderer.Length; i++)
            {
                targetRenderer[i].GetPropertyBlock(propertyBlock);
                propertyBlock.SetInt(propertyNameProp, Random.Range(minValue, maxValue));
                targetRenderer[i].SetPropertyBlock(propertyBlock);
            }
        }
    }
}