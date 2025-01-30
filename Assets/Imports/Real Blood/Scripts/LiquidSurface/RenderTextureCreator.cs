using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Knife.RealBlood.LiquidSurface
{
    /// <summary>
    /// Simple rendertexture creator to create RenderTexture with selected resolution and format
    /// </summary>
    [Serializable]
    public class RenderTextureCreator
    {
        [SerializeField] private Resolution maskResolution = Resolution.High;
        [SerializeField] private RenderTextureFormat maskFormat = RenderTextureFormat.ARGB32;

        public RenderTexture RenderTexture { get; private set; }

        private enum Resolution
        {
            Low = 256,
            Medium = 512,
            High = 1024,
            VeryHigh = 2048,
            Ultra = 4096
        }

        public void Create()
        {
            int maskRes = (int)maskResolution;
            RenderTexture = new RenderTexture(maskRes, maskRes, 0, maskFormat);
        }

        public void Destroy()
        {
            Object.DestroyImmediate(RenderTexture, true);
        }
    }
}