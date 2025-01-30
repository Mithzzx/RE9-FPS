using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Provides spawn on particle collision with normal angle checking
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class DecalsOnParticleCollisionAngledSpawn : DecalsOnParticleCollision
    {
        [SerializeField] private float minAngle = 25f;
        [SerializeField] private float maxAngle = 155f;
        [SerializeField] private Vector3 planeNormal = Vector3.up;

        protected override bool ShouldSpawn(ParticleCollisionEvent particleCollisionEvent)
        {
            var normal = particleCollisionEvent.normal;

            float angle = Vector3.Angle(normal, planeNormal);

            bool shouldSpawn = angle >= minAngle && angle <= maxAngle;

            return shouldSpawn;
        }
    }
}