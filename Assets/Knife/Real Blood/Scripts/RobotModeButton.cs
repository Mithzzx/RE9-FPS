using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Demo robot mode button behaviour
    /// </summary>
    public class RobotModeButton : MonoBehaviour, IHittable
    {
        [SerializeField] private DemoRobot.RobotMode robotMode;
        [SerializeField] private DemoRobot targetRobot;
        [SerializeField] private RobotModeButton otherButton;

        [SerializeField] private Material enabledMaterial;
        [SerializeField] private Material disabledMaterial;
        [SerializeField] private bool defaultEnabled = false;

        private Renderer attachedRenderer;

        private void Awake()
        {
            attachedRenderer = GetComponent<Renderer>();
        }

        private void Start()
        {
            if (defaultEnabled)
            {
                EnableMode();
            }
        }

        public void TakeDamage(DamageData[] damage)
        {
            EnableMode();
        }

        public void EnableMode()
        {
            targetRobot.SetMode(robotMode);
            otherButton.DisableMode();

            attachedRenderer.sharedMaterial = enabledMaterial;
        }

        public void DisableMode()
        {
            attachedRenderer.sharedMaterial = disabledMaterial;
        }
    }
}