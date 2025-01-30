using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Behaviour that set vector applier value by index
    /// </summary>
    [ExecuteAlways]
    public class LocalPositionToVectorApplier : MonoBehaviour
    {
        /// <summary>
        /// Value index in vector applier array
        /// </summary>
        public int Index = 0;
        /// <summary>
        /// When true values will be updated in Update
        /// </summary>
        public bool AutoUpdate;
        [SerializeField] private bool OnEnableOnly;

        [SerializeField]
        private InstancedRendererVectorApplier applier;

        public InstancedRendererVectorApplier Applier
        {
            get
            {
                if (applier == null)
                    applier = GetComponent<InstancedRendererVectorApplier>();

                return applier;
            }
        }

        private void OnValidate()
        {
            Applier.SetValue(Index, transform.localPosition);
        }

        private void OnEnable()
        {
            if (OnEnableOnly)
            {
                if (Application.isPlaying)
                {
                    Applier.SetValue(Index, transform.localPosition);
                }
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (AutoUpdate)
                {
                    Applier.SetValue(Index, transform.localPosition);
                }
            }
        }
    }
}