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
    [Tooltip("見た目の既定スキン。行側の指定が優先")]
    [SerializeField] public EnemySkin defaultSkin;

    [Header("Stats")]
    public TeamId teamId = TeamId.Enemy;
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

    [Header("Score")]
    public int scoreValue = 100;

    [Header("Audio")]
    // 撃破時に再生。未設定なら無音。
    public SoundData deathSound;

    void OnValidate()
    {
        maxHp = Mathf.Max(1, maxHp);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        detectionRange = Mathf.Max(0f, detectionRange);
        loseSightRange = Mathf.Max(detectionRange, loseSightRange);
        attackRange = Mathf.Max(0f, attackRange);
        patrolDistance = Mathf.Max(0f, patrolDistance);
        scoreValue = Mathf.Max(0, scoreValue);
    }
}
