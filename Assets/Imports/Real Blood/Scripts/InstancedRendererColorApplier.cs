using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Behaviour taht can apply colors to renderer (with MaterialPropertyBlock)
    /// </summary>
    public class InstancedRendererColorApplier : MonoBehaviour
    {
        /// <summary>
        /// Properties
        /// </summary>
        public string[] Names;
        /// <summary>
        /// Color values that will be applied
        /// </summary>
        public Color[] Values;

        [SerializeField]
        private Renderer myRenderer;

        public Renderer MyRenderer
        {
            get
            {
                if (myRenderer == null)
                    myRenderer = GetComponent<Renderer>();

                return myRenderer;
            }
        }

        private void OnEnable()
        {
            UpdateBlock();
        }

        private void OnValidate()
        {
            UpdateBlock();
        }

        /// <summary>
        /// Set value of color by index
        /// </summary>
        /// <param name="index">Index in array</param>
        /// <param name="value">Vector value</param>
        public void SetValue(int index, Vector4 value)
        {
            Values[index] = value;
            UpdateBlock();
        }

        private void UpdateBlock()
        {
            if (Names.Length != Values.Length)
                return;

            Renderer r = MyRenderer;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);

            for (int i = 0; i < Names.Length; i++)
            {
                block.SetColor(Names[i], Values[i]);
            }
            r.SetPropertyBlock(block);
        }
    }
}