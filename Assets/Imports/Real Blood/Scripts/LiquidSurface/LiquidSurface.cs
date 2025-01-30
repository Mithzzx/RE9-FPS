using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood.LiquidSurface
{
    /// <summary>
    /// Liquid surface behaviour
    /// </summary>
    public class LiquidSurface : MonoBehaviour
    {
        [Header("Rendering")]
        [SerializeField] private RenderTextureCreator renderTextureCreator = new RenderTextureCreator();
        [SerializeField] private RenderingCameraCreator renderingCameraCreator = new RenderingCameraCreator();
        [SerializeField] private float height = 3f;

        [Header("Camera placing")]
        [SerializeField] private TransformPlacer cameraPlacer = new TransformPlacer();

        [Header("Particles")]
        [SerializeField] private SimpleGameObjectPool particlesPool = new SimpleGameObjectPool();

        [Header("Shading")]
        [SerializeField] private TextureApplier textureApplier = new TextureApplier();

        private bool isInitialized = false;

        /// <summary>
        /// Spawn particle on surface
        /// </summary>
        /// <param name="position">spawn position</param>
        public void SpawnParticle(Vector3 position)
        {
            if (!isInitialized)
            {
                Debug.LogError("Liquid surface is not initialized");
                return;
            }

            var particle = particlesPool.GetObject();
            particle.SetActive(true);
            particle.transform.position = position;
            particle.transform.SetParent(null);
            particlesPool.Destroy(this, particle, 5f);
        }

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            DestroyResources();
        }

        private void Initialize()
        {
            renderTextureCreator.Create();
            renderingCameraCreator.Create();

            cameraPlacer.Place(renderingCameraCreator.Camera.transform);
            renderingCameraCreator.Camera.targetTexture = renderTextureCreator.RenderTexture;

            textureApplier.Apply(renderTextureCreator.RenderTexture);

            particlesPool.Create();

            isInitialized = true;
        }

        private void OnDrawGizmos()
        {
            cameraPlacer.DrawGizmos(renderingCameraCreator.Size);
        }

        private void DestroyResources()
        {
            isInitialized = false;
        }
    }
}