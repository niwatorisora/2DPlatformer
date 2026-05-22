using UnityEngine;

/// <summary>
/// Per-enemy-type authoring data. Drive HP, movement, detection, and weapon selection
/// from this asset so new enemy kinds need only a new ScriptableObject and Prefab.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Combat/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject prefab;

    [Header("Stats")]
    public int maxHp = 100;

    [Header("Movement")]
    public float moveSpeed = 3f;

    [Header("Detection")]
    public float detectionRange = 6f;
    // loseSightRange should be >= detectionRange so the enemy does not flicker between states.
    public float loseSightRange = 8f;

    [Header("Attack")]
    public float attackRange = 4f;

    [Header("Patrol")]
    public bool patrolEnabled = false;
    public float patrolDistance = 3f;

    void OnValidate()
    {
        maxHp = Mathf.Max(1, maxHp);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        detectionRange = Mathf.Max(0f, detectionRange);
        loseSightRange = Mathf.Max(detectionRange, loseSightRange);
        attackRange = Mathf.Max(0f, attackRange);
        patrolDistance = Mathf.Max(0f, patrolDistance);
    }
}
