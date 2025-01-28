using System;
using UnityEngine;
using UnityEngine.AI;

public class DebugNavMeshAgent : MonoBehaviour
{
    private NavMeshAgent agent;
    
    [Header("Settings")]
    [SerializeField] private bool showPath;
    [SerializeField] private bool showVelocity;
    [SerializeField] private bool showDesiredVelocity;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnDrawGizmos()
    {
        if (showPath)
        {
            Gizmos.color = Color.black;
            for (int i = 0; i < agent.path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(agent.path.corners[i], agent.path.corners[i + 1]);
                Gizmos.DrawSphere(agent.path.corners[i], 0.1f);
            }
        }

        if (showVelocity)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + agent.velocity);
        }

        if (showDesiredVelocity)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + agent.desiredVelocity);
        }
    }
}
