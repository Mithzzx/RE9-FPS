using System;
using UnityEngine;

namespace Knife.RealBlood.LiquidSurface
{
    /// <summary>
    /// Rendering camera creator can help to setup camera options in inspector that will be spawned in runtime
    /// </summary>
    [Serializable]
    public class RenderingCameraCreator
    {
        [SerializeField] private Vector2 size = new Vector2(1, 1);
        [SerializeField] private LayerMask renderingLayerMask;
        [SerializeField] private CameraClearFlags clearFlags = CameraClearFlags.SolidColor;
        [SerializeField] private Color backgroundColor = Color.clear;
        [SerializeField] private float nearPlane = 0.3f;
        [SerializeField] private float farPlane = 100f;

        public Camera Camera { get; private set; }
        public Vector2 Size { get => size; private set => size = value; }

        public void Create()
        {
            Camera = new GameObject("Liquid Surface Camera", typeof(Camera)).GetComponent<Camera>();

            Camera.nearClipPlane = nearPlane;
            Camera.farClipPlane = farPlane;

            if (Size.x > Size.y)
            {
                Camera.aspect = Size.x / Size.y;
            }
            else
            {
                Camera.aspect = Size.y / Size.x;
            }

            Camera.orthographic = true;
            Camera.orthographicSize = Mathf.Min(Size.x, Size.y) / 2f;

            Camera.cullingMask = renderingLayerMask;

            Camera.clearFlags = clearFlags;
            Camera.backgroundColor = backgroundColor;
        }
    }
}