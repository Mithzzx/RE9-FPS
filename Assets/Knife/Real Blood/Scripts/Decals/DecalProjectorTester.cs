using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood.Decals
{
    public class DecalProjectorTester : MonoBehaviour
    {
        [SerializeField] private Texture2D brush;
        [SerializeField] private float size = 0.5f;
        [SerializeField] private float zDepth = 1f;
        [SerializeField] private Renderer target;
        [SerializeField] private RenderTexture rt;

        private void Update()
        {
            DecalsProjector.Instance.Project(transform.position, transform.forward, target, brush, rt, size, zDepth);
        }
    }
}