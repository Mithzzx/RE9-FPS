using System;
using UnityEngine;

namespace Knife.RealBlood.LiquidSurface
{
    /// <summary>
    /// Texture applier to renderers with property names
    /// </summary>
    [Serializable]
    public class TextureApplier
    {
        [SerializeField] private PropertyGroupApplier[] appliers;

        public void Apply(Texture texture)
        {
            foreach (var a in appliers)
            {
                a.Apply(texture);
            }
        }

        [Serializable]
        private class PropertyGroupApplier
        {
            [SerializeField] private string propertyName = "_MainTex";
            [SerializeField] private Renderer[] targetRenderers;

            public void Apply(Texture texture)
            {
                int propertyID = Shader.PropertyToID(propertyName);
                MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                foreach (var r in targetRenderers)
                {
                    r.GetPropertyBlock(materialPropertyBlock);
                    materialPropertyBlock.SetTexture(propertyID, texture);
                    r.SetPropertyBlock(materialPropertyBlock);
                }
            }
        }
    }
}