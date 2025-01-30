using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// This class is used to call C# event on AnimationEvent call
    /// </summary>
    public class AnimationEventSimpleObserverable : MonoBehaviour, ISimpleObserverable
    {
        private Action onEventCalledEvent;

        /// <summary>
        /// Subscribe on event
        /// </summary>
        /// <param name="callback"></param>
        public void AddListener(Action callback)
        {
            onEventCalledEvent += callback;
        }

        /// <summary>
        /// Unsubscribe from event
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveListener(Action callback)
        {
            onEventCalledEvent -= callback;
        }

        private void CallAnimationEvent()
        {
            if (onEventCalledEvent != null)
                onEventCalledEvent();
        }
    }
}