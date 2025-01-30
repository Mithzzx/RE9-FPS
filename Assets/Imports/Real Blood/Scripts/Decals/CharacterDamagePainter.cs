using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Knife.RealBlood.Decals
{
    /// <summary>
    /// Damage painter behaviour on renderers
    /// </summary>
    public class CharacterDamagePainter : MonoBehaviour
    {
        [SerializeField] private ProjectingRenderer[] projectingRenderers = new ProjectingRenderer[0];
        [SerializeField] private string texturePropertyName = "";
        [SerializeField] private int textureResolution = 1024;
        [SerializeField] private float zDepth = 1;
        [SerializeField] private BrushDamagePair[] brushes;

        private RenderTexture[] renderTextures;

        private void Awake()
        {
            InitAndSetResources();
        }

        private void InitAndSetResources()
        {
            if (!Application.isPlaying)
                return;

            if(renderTextures != null)
            {
                foreach(var rt in renderTextures)
                {
                    DestroyImmediate(rt);
                }
            }

            renderTextures = new RenderTexture[projectingRenderers.Length];

            for (int i = 0; i < projectingRenderers.Length; i++)
            {
                renderTextures[i] = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.ARGB32);

                projectingRenderers[i].SetupTexture(renderTextures[i], texturePropertyName);
            }
        }

        public void CleanDamage()
        {
            var currentRT = RenderTexture.active;
            for (int i = 0; i < renderTextures.Length; i++)
            {
                RenderTexture.active = renderTextures[i];
                GL.Clear(true, true, Color.clear);
            }
            RenderTexture.active = currentRT;
        }

        public void Paint(Vector3 point, Vector3 normal, int brushID)
        {
            var brush = brushes[brushID];
            for (int i = 0; i < projectingRenderers.Length; i++)
            {
                DecalsProjector.Instance.Project(point, normal.normalized, projectingRenderers[i].TargetRenderer, brush.Brush, renderTextures[i], brush.Size, zDepth);
            }
        }

        [Serializable]
        private class BrushDamagePair
        {
            [SerializeField] private float size = 0.5f;
            [SerializeField] private Texture2D brush = null;

            public float Size { get => size; private set => size = value; }
            public Texture2D Brush { get => brush; private set => brush = value; }
        }

        [Serializable]
        public class ProjectingRenderer
        {
            [SerializeField] private Renderer targetRenderer;
            [SerializeField] private Renderer[] setuppingRenderers;

            public ProjectingRenderer(Renderer targetRenderer, Renderer[] setuppingRenderers)
            {
                this.targetRenderer = targetRenderer;
                this.setuppingRenderers = setuppingRenderers;
            }

            public Renderer TargetRenderer { get => targetRenderer; private set => targetRenderer = value; }
            public Renderer[] SetuppingRenderers { get => setuppingRenderers; private set => setuppingRenderers = value; }

            public void SetupTexture(RenderTexture texture, string texturePropertyName)
            {
                MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
                targetRenderer.GetPropertyBlock(materialPropertyBlock);
                materialPropertyBlock.SetTexture(texturePropertyName, texture);
                targetRenderer.SetPropertyBlock(materialPropertyBlock);

                for (int i = 0; i < SetuppingRenderers.Length; i++)
                {
                    SetuppingRenderers[i].GetPropertyBlock(materialPropertyBlock);
                    materialPropertyBlock.SetTexture(texturePropertyName, texture);
                    SetuppingRenderers[i].SetPropertyBlock(materialPropertyBlock);
                }
            }
        }
    }
}