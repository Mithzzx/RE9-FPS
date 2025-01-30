using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Knife.RealBlood.Decals
{
    /// <summary>
    /// Decals projector helper behaviour
    /// </summary>
    public class DecalsProjector : MonoBehaviour
    {
        private Camera decalsCamera;

        private static DecalsProjector instance;

        public static DecalsProjector Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = GameObject.FindObjectOfType<DecalsProjector>();
                    instance.Init();
                }

                return instance;
            }
        }

        CommandBuffer renderingBuffer;

        Material projectionMaterial;

        Matrix4x4 projectionMatrix;
        Matrix4x4 worldToCameraMatrix;

        private void Start()
        {
            Init();
        }

        public void Init()
        {
            GameObject camera = new GameObject("DecalsCamera", typeof(Camera));
            camera.hideFlags = HideFlags.HideAndDontSave;
            decalsCamera = camera.GetComponent<Camera>();

            renderingBuffer = new CommandBuffer();
            renderingBuffer.name = "Decals rendering buffer";

            projectionMaterial = new Material(Resources.Load<Shader>("ProjectionShader"));

            decalsCamera.orthographic = true;
            decalsCamera.orthographicSize = 0.5f;
            decalsCamera.aspect = 1;
            decalsCamera.transform.position = -Vector3.forward * 0.2f;
            decalsCamera.transform.eulerAngles = Vector3.zero;
            projectionMatrix = decalsCamera.projectionMatrix;
            worldToCameraMatrix = decalsCamera.worldToCameraMatrix;

            decalsCamera.orthographic = true;
            decalsCamera.orthographicSize = 0.5f;
            decalsCamera.aspect = 1;
            decalsCamera.nearClipPlane = 0.01f;
            decalsCamera.transform.position = -Vector3.forward * 0.2f;
            decalsCamera.transform.eulerAngles = Vector3.zero;
            projectionMatrix = decalsCamera.projectionMatrix;
            worldToCameraMatrix = decalsCamera.worldToCameraMatrix;

            decalsCamera.gameObject.SetActive(true);
            decalsCamera.enabled = false;
        }

        private void OnDestroy()
        {
            Object.DestroyImmediate(projectionMaterial, true);
            if (decalsCamera != null)
                Object.DestroyImmediate(decalsCamera.gameObject, true);
        }

        public void Project(Vector3 position, Vector3 normal, Renderer renderer, Texture input, RenderTexture target, float size = 0.5f, float zDepth = 1)
        {
            renderingBuffer.Clear();
            RenderTargetIdentifier targetBuffer = new RenderTargetIdentifier(target);
            projectionMaterial.SetTexture("_Input1", input);
            decalsCamera.transform.position = position + normal * 0.1f;
            decalsCamera.transform.rotation = Quaternion.LookRotation(-normal.normalized);
            decalsCamera.farClipPlane = zDepth;
            decalsCamera.orthographicSize = size;
            projectionMaterial.SetMatrix("_ProjMat", decalsCamera.projectionMatrix * decalsCamera.worldToCameraMatrix);
            projectionMaterial.SetVector("_HitNormal", normal);

            renderingBuffer.SetRenderTarget(targetBuffer, targetBuffer);

            renderingBuffer.SetViewProjectionMatrices(worldToCameraMatrix, projectionMatrix);
            renderingBuffer.DrawRenderer(renderer, projectionMaterial);

            Graphics.ExecuteCommandBuffer(renderingBuffer);
        }
    }
}