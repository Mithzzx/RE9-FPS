using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Shoot off hand event handler
    /// </summary>
    public class DemoHandExplosionEventHandler : MonoBehaviour, ISimpleObserverable
    {
        [SerializeField] private DemoDollHand[] hands;

        private Action onExplodedEvent;

        public void AddListener(Action callback)
        {
            onExplodedEvent += callback;
        }

        public void RemoveListener(Action callback)
        {
            onExplodedEvent -= callback;
        }

        private void Start()
        {
            foreach (var h in hands)
            {
                h.AddListener(OnExploded);
            }
        }

        private void OnExploded()
        {
            if (onExplodedEvent != null)
                onExplodedEvent();
        }
    }
}