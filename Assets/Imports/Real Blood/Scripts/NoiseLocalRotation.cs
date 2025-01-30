using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Behaviour that can add noisy transform rotation
    /// </summary>
    public class NoiseLocalRotation : MonoBehaviour
    {
        [SerializeField] private float maxAngle = 15f;
        [SerializeField] private float smoothSpeed = 5f;
        [SerializeField] private Vector3 localAxis1 = new Vector3(1, 0, 0);
        [SerializeField] private Vector3 localAxis2 = new Vector3(0, 1, 0);
        [SerializeField] private float noiseSpeed = 1f;
        [SerializeField] private float noiseFrequency = 0.2f;

        private int randomSeed1;
        private int randomSeed2;

        private Quaternion startRotation;

        private float noise1;
        private float noise2;

        private float lastNoiseChange = 0;

        private void Awake()
        {
            startRotation = transform.localRotation;
        }

        private void OnEnable()
        {
            transform.localRotation = startRotation;
            randomSeed1 = Random.Range(0, 10000);
            randomSeed2 = Random.Range(0, 10000);
        }

        private void Update()
        {
            float offset1 = randomSeed1;
            float offset2 = randomSeed2;

            float time = Time.time * noiseSpeed;

            float time1 = (time + randomSeed1);
            float time2 = (time + randomSeed2);

            if (time > lastNoiseChange + noiseFrequency)
            {
                noise1 = Mathf.PerlinNoise(time1, time1 * time1) * 2 - 1;
                noise2 = Mathf.PerlinNoise(time2, time2 * time2) * 2 - 1;
                lastNoiseChange = time;
            }

            Quaternion rotation1 = Quaternion.AngleAxis(noise1 * maxAngle, localAxis1);
            Quaternion rotation2 = Quaternion.AngleAxis(noise2 * maxAngle, localAxis2);

            Quaternion rotation = startRotation * rotation1 * rotation2;

            transform.localRotation = Quaternion.Lerp(transform.localRotation, rotation, Time.deltaTime * smoothSpeed);
        }
    }
}