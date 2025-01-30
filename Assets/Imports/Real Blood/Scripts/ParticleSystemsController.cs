using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Behaviour that can control some parameters on particle system by multipliers
    /// </summary>
    public class ParticleSystemsController : MonoBehaviour
    {
        [SerializeField] private float lifetimeMultiplier = 1f;
        [SerializeField] private float sizeMultiplier = 1f;
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private float shapeAngleMultiplier = 1f;

        [SerializeField] private ParticleSystem[] particleSystems;
        [SerializeField] private ParticleSystem.MinMaxCurve[] lifetimes;
        [SerializeField] private ParticleSystem.MinMaxCurve[] sizes;
        [SerializeField] private ParticleSystem.MinMaxCurve[] speeds;
        [SerializeField] private float[] shapeAngles;

        private bool liveUpdate = false;

        [ContextMenu("Enable live update")]
        public void EnableLiveUpdate()
        {
            EnableLiveUpdate(true);
        }

        [ContextMenu("Disable live update")]
        public void DisableLiveUpdate()
        {
            EnableLiveUpdate(false);
        }

        [ContextMenu("Save current curves")]
        private void SaveCurrentCurves()
        {
            lifetimes = new ParticleSystem.MinMaxCurve[particleSystems.Length];
            sizes = new ParticleSystem.MinMaxCurve[particleSystems.Length];
            speeds = new ParticleSystem.MinMaxCurve[particleSystems.Length];
            shapeAngles = new float[particleSystems.Length];
            for (int i = 0; i < particleSystems.Length; i++)
            {
                lifetimes[i] = Multiply(particleSystems[i].main.startLifetime, 1f / lifetimeMultiplier);
                sizes[i] = Multiply(particleSystems[i].main.startSize, 1f / sizeMultiplier);
                speeds[i] = Multiply(particleSystems[i].main.startSpeed, 1f / speedMultiplier);
                shapeAngles[i] = particleSystems[i].shape.angle / shapeAngleMultiplier;
            }
        }

        private void EnableLiveUpdate(bool enabled)
        {
            liveUpdate = enabled;
        }

        private void OnValidate()
        {
            if (liveUpdate)
            {
                UpdateLifetime();
            }
        }

        private void UpdateLifetime()
        {
            for (int i = 0; i < particleSystems.Length; i++)
            {
                var main = particleSystems[i].main;

                ParticleSystem.MinMaxCurve lifetime = Multiply(lifetimes[i], lifetimeMultiplier);
                ParticleSystem.MinMaxCurve size = Multiply(sizes[i], sizeMultiplier);
                ParticleSystem.MinMaxCurve speed = Multiply(speeds[i], speedMultiplier);
                float shapeAngle = shapeAngles[i] * shapeAngleMultiplier;

                main.startLifetime = lifetime;
                main.startSize = size;
                main.startSpeed = speed;

                var shape = particleSystems[i].shape;

                shape.angle = shapeAngle;
            }
        }

        private ParticleSystem.MinMaxCurve Multiply(ParticleSystem.MinMaxCurve minMaxCurve, float mul)
        {
            minMaxCurve.curveMultiplier *= mul;
            minMaxCurve.constant *= mul;
            minMaxCurve.constantMin *= mul;
            minMaxCurve.constantMax *= mul;

            return minMaxCurve;
        }

        private void Start()
        {
            UpdateLifetime();
        }
    }
}