using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Demo behaviour to reset components on damage taken
    /// </summary>
    public class ResetButtonByDamage : MonoBehaviour, IHittable
    {
        [SerializeField] private MonoBehaviour[] resettables;
        [SerializeField] private MonoBehaviour showButtonEventObserverable;
        [SerializeField] private MonoBehaviour[] showButtonEventObserverables;
        [SerializeField] private float showButtonDelay = 5f;

        private IResettable[] resettablesComponents;
        private ISimpleObserverable showButtonEvent;

        private Renderer[] renderers;
        private Collider[] colliders;

        private bool hasEvent;
        private bool isShowed = false;

        private void Start()
        {
            renderers = GetComponentsInChildren<Renderer>();
            colliders = GetComponentsInChildren<Collider>();

            if (showButtonEventObserverable != null)
                showButtonEvent = showButtonEventObserverable.GetComponent<ISimpleObserverable>();

            resettablesComponents = new IResettable[resettables.Length];
            for (int i = 0; i < resettablesComponents.Length; i++)
            {
                resettablesComponents[i] = resettables[i].GetComponent<IResettable>();
            }

            hasEvent = showButtonEvent != null || (showButtonEventObserverables != null && showButtonEventObserverables.Length > 0);

            if (showButtonEvent != null || (showButtonEventObserverables != null && showButtonEventObserverables.Length > 0))
            {
                if(showButtonEvent != null)
                    showButtonEvent.AddListener(ShowButton);

                if(showButtonEventObserverables != null && showButtonEventObserverables.Length > 0)
                {
                    for (int i = 0; i < showButtonEventObserverables.Length; i++)
                    {
                        var showEvent = showButtonEventObserverables[i].GetComponent<ISimpleObserverable>();

                        if(showEvent != null)
                            showEvent.AddListener(ShowButton);
                    }
                }

                HideButton();
            }
            else
            {
                // no hide
            }
        }

        private void ShowButton()
        {
            if (isShowed)
                return;

            isShowed = true;
            StartCoroutine(ShowButtonWithDelay(showButtonDelay));
        }

        private IEnumerator ShowButtonWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            foreach (var r in renderers)
            {
                r.enabled = true;
            }
            foreach (var c in colliders)
            {
                c.enabled = true;
            }
        }

        private void HideButton()
        {
            isShowed = false;
            StopAllCoroutines();
            foreach (var r in renderers)
            {
                r.enabled = false;
            }
            foreach (var c in colliders)
            {
                c.enabled = false;
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (this == null)
                return;

            if (showButtonEventObserverable != null)
                showButtonEvent = showButtonEventObserverable.GetComponent<ISimpleObserverable>();
            else
                showButtonEvent = null;

            if (showButtonEvent == null)
            {
                showButtonEventObserverable = null;
            }
            else
            {
                showButtonEventObserverable = showButtonEvent as MonoBehaviour;
            }

            for (int i = 0; i < resettables.Length; i++)
            {
                if (resettables[i] != null)
                {
                    var resettable = resettables[i].GetComponent<IResettable>();
                    if (resettable == null)
                    {
                        resettables[i] = null;
                    }
                    else
                    {
                        resettables[i] = resettable as MonoBehaviour;
                    }
                }
            }
        }

        public void TakeDamage(DamageData[] damage)
        {
            if (hasEvent)
                HideButton();

            foreach (var r in resettablesComponents)
            {
                r.ResetComponent();
            }
        }
    }
}