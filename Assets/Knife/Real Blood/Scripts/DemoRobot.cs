using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Showcase helper behaviour that can shoot to fixed Transform target
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class DemoRobot : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float rotateSpeed = 45f;
        [SerializeField] private float blendSpeed = 1;
        [SerializeField] private Transform rotatingRoot;
        [SerializeField] private AutoSpawner bulletSpawner;
        [SerializeField] private GameObject[] indicators;
        [SerializeField] private float angleForShooting = 15f;

        private bool isRobotEnabled;
        private float aimWeight = 0;
        private Animator robotAnimator;

        private float aimAngle = 0;

        private Quaternion targetRotation;
        private Quaternion startRotation;

        private Coroutine enableBulletSpawnerCoroutine;

        private void Awake()
        {
            robotAnimator = GetComponent<Animator>();
            startRotation = rotatingRoot.rotation;
            targetRotation = startRotation;

            DisableRobot();
        }

        /// <summary>
        /// Enable Robot
        /// </summary>
        [ContextMenu("Enable robot")]
        public void EnableRobot()
        {
            isRobotEnabled = true;

            robotAnimator.SetBool("IsEnabled", true);
            SwitchBulletSpawner(2f, true);
            foreach (var i in indicators)
            {
                i.SetActive(true);
            }
        }

        private void SwitchBulletSpawner(float delay, bool enabled)
        {
            if (enableBulletSpawnerCoroutine != null)
            {
                StopCoroutine(enableBulletSpawnerCoroutine);
            }

            enableBulletSpawnerCoroutine = StartCoroutine(EnableBulletSpawner(delay, enabled));
        }

        private IEnumerator EnableBulletSpawner(float delay, bool enabled)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            bulletSpawner.gameObject.SetActive(enabled);
        }

        /// <summary>
        /// Disable robot
        /// </summary>
        [ContextMenu("Disable robot")]
        public void DisableRobot()
        {
            isRobotEnabled = false;

            robotAnimator.SetBool("IsEnabled", false);
            SwitchBulletSpawner(0f, false);
            foreach (var i in indicators)
            {
                i.SetActive(false);
            }
        }

        void Update()
        {
            aimWeight = Mathf.MoveTowards(aimWeight, isRobotEnabled ? 1 : 0, Time.deltaTime * blendSpeed);

            Vector3 directionRoot = target.position - rotatingRoot.position;
            directionRoot.y = 0;
            directionRoot.Normalize();

            targetRotation = Quaternion.Lerp(targetRotation, Quaternion.LookRotation(directionRoot, Vector3.up), Time.deltaTime * rotateSpeed);

            rotatingRoot.rotation = Quaternion.Lerp(startRotation, targetRotation, aimWeight);

            float angle = Vector3.Angle(rotatingRoot.forward, directionRoot);

            bulletSpawner.IsPause = angle > angleForShooting;
        }

        /// <summary>
        /// Set robot shooting mode
        /// </summary>
        /// <param name="mode"></param>
        public void SetMode(RobotMode mode)
        {
            if (mode == RobotMode.Pistol)
            {
                bulletSpawner.Interval = 1f;
            }
            else if (mode == RobotMode.Auto)
            {
                bulletSpawner.Interval = 0.1f;
            }
        }

        /// <summary>
        /// Robot shooting mode
        /// </summary>
        public enum RobotMode
        {
            Pistol,
            Auto
        }
    }
}