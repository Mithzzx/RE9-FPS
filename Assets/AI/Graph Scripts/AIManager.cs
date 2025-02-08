using UnityEngine;
using System.Collections.Generic;

public class AIManager : MonoBehaviour
{
    private static AIManager _instance;
    public static AIManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AIManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AIManager");
                    _instance = go.AddComponent<AIManager>();
                }
            }
            return _instance;
        }
    }

    public Vector3 LastKnownPlayerPosition { get; private set; }
    public float LastSeenTime { get; private set; }
    public bool HasLastKnownPosition { get; private set; }

    // Attack coordination
    private readonly List<AttackAction> activeAttackers = new List<AttackAction>();
    private readonly Dictionary<GameObject, AttackAction> attackerMap = new Dictionary<GameObject, AttackAction>();
    private readonly Dictionary<AttackAction, float> attackCooldowns = new Dictionary<AttackAction, float>();
    private const float MinAttackSpacing = 0.5f; // Minimum time between different zombie attacks
    private const int MaxSimultaneousAttackers = 3; // Maximum zombies that can attack at once
    private float lastAttackTime;

    public void RegisterAttacker(AttackAction attacker)
    {
        if (!activeAttackers.Contains(attacker))
        {
            activeAttackers.Add(attacker);
            attackerMap[attacker.ai.Value] = attacker;
            attackCooldowns[attacker] = 0f;
        }
    }

    public void UnregisterAttacker(AttackAction attacker)
    {
        activeAttackers.Remove(attacker);
        attackerMap.Remove(attacker.ai.Value);
        attackCooldowns.Remove(attacker);
    }

    public bool CanAttack(AttackAction attacker, Transform playerTransform)
    {
        if (Time.time - lastAttackTime < MinAttackSpacing) return false;
        if (activeAttackers.Count >= MaxSimultaneousAttackers) return false;
        if (attackCooldowns[attacker] > Time.time) return false;

        // Check if this zombie is in a good position relative to others
        Vector3 dirToPlayer = (playerTransform.position - attacker.ai.Value.transform.position).normalized;
        foreach (var otherAttacker in activeAttackers)
        {
            if (otherAttacker == attacker) continue;

            Vector3 otherDirToPlayer = (playerTransform.position - otherAttacker.ai.Value.transform.position).normalized;
            float angle = Vector3.Angle(dirToPlayer, otherDirToPlayer);

            // Too close to another attacker
            if (angle < 60f) return false;
        }

        return true;
    }

    public void RecordAttack(AttackAction attacker)
    {
        lastAttackTime = Time.time;
        attackCooldowns[attacker] = Time.time + Random.Range(3f, 5f); // Individual cooldown
    }

    public Vector3 GetCirclingPosition(GameObject zombie, GameObject player, float circleRadius)
    {
        int attackerCount = activeAttackers.Count;
        int attackerIndex = activeAttackers.IndexOf(attackerMap[zombie]);

        // Calculate ideal spacing angle
        float angleSpacing = 360f / Mathf.Max(attackerCount, 3);
        float currentAngle = angleSpacing * attackerIndex;

        // Get position on circle around player
        Vector3 offset = Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * circleRadius;
        return player.transform.position + offset;
    }

    public void UpdateLastKnownPosition(Vector3 position)
    {
        LastKnownPlayerPosition = position;
        LastSeenTime = Time.time;
        HasLastKnownPosition = true;
    }

    public void ClearLastKnownPosition()
    {
        HasLastKnownPosition = false;
    }
}