using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Knife.RealBlood
{
    /// <summary>
    /// Helper class to enable and disable DemoRobot on Unity Trigger Events
    /// </summary>
    public class DemoRobotTrigger : MonoBehaviour
    {
        [SerializeField] private DemoRobot robot;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                robot.EnableRobot();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                robot.DisableRobot();
            }
        }
    }
}