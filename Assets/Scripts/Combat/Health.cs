using System;
using UnityEngine;

/// <summary>
/// Runtime details about one damage attempt.
/// </summary>
public readonly struct DamageContext
{
    public readonly int amount;
    public readonly GameObject source;
    public readonly TeamId sourceTeam;
    public readonly Collider2D hitCollider;

    public DamageContext(int amount, GameObject source, TeamId sourceTeam, Collider2D hitCollider)
    {
        this.amount      = amount;
        this.source      = source;
        this.sourceTeam  = sourceTeam;
        this.hitCollider = hitCollider;
    }
}

/// <summary>
/// Contract for objects that can receive combat damage.
/// </summary>
public interface IDamageable
{
    event Action<int> OnDamaged;
    bool CanReceiveDamage(DamageContext context);
    void TakeDamage(DamageContext context);
}

/// <summary>
/// Shared HP and damage intake component for combat actors.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TeamAffiliation))]
public class Health : MonoBehaviour, IDamageable
{
    [Tooltip("Initial HP for manually placed actors. Enemies spawned by WaveSpawner use EnemyData.maxHp instead.")]
    [SerializeField] int maxHp = 100;
    [SerializeField] bool isInvulnerable;
    [SerializeField, Tooltip("被弾後の無敵時間。0で無効。プレイヤー用")]
    float invincibleSecondsAfterHit = 0f;

    int currentHp;
    float nextDamageTime;
    TeamAffiliation teamAffiliation;

    public event Action<int> OnDamaged;
    public event Action OnDied;

    public int MaxHp => maxHp;
    public int CurrentHp => currentHp;
    public bool IsAlive => currentHp > 0;
    public bool IsInvulnerable
    {
        get => isInvulnerable;
        set => isInvulnerable = value;
    }

    void Awake()
    {
        teamAffiliation = GetComponent<TeamAffiliation>();
        currentHp = maxHp;
    }

    /// <summary>
    /// Sets max HP and resets current HP to the new value.
    /// Call this from WaveSpawner or other spawners that drive HP from EnemyData.
    /// Manual scene-placed actors can rely on the Inspector value and skip this call.
    /// </summary>
    public void Initialize(int newMaxHp)
    {
        maxHp = Mathf.Max(1, newMaxHp);
        currentHp = maxHp;
        nextDamageTime = 0f;
    }

    public bool CanReceiveDamage(DamageContext context)
    {
        if (!IsAlive) return false;
        if (isInvulnerable) return false;
        if (Time.time < nextDamageTime) return false;
        if (context.amount <= 0) return false;

        return !IsFriendlySource(context.sourceTeam);
    }

    public void TakeDamage(DamageContext context)
    {
        if (!CanReceiveDamage(context)) return;

        int finalDamage = CalculateDamage(context);
        if (finalDamage <= 0) return;

        currentHp = Mathf.Max(0, currentHp - finalDamage);
        nextDamageTime = Time.time + invincibleSecondsAfterHit;
        OnDamaged?.Invoke(finalDamage);

        if (currentHp <= 0)
            OnDied?.Invoke();
    }

    /// <summary>
    /// Override point for future armor, resistance, or status-effect damage changes.
    /// </summary>
    protected virtual int CalculateDamage(DamageContext context)
        => Mathf.Max(0, context.amount);

    bool IsFriendlySource(TeamId sourceTeam)
    {
        TeamId ownTeam = teamAffiliation != null ? teamAffiliation.TeamId : TeamId.Neutral;
        return TeamAffiliation.AreFriendly(sourceTeam, ownTeam);
    }

    void OnValidate()
    {
        maxHp = Mathf.Max(1, maxHp);
        invincibleSecondsAfterHit = Mathf.Max(0f, invincibleSecondsAfterHit);
    }
}
