using System;
using UnityEngine;

/// <summary>
/// Shared HP and damage intake component for combat actors.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TeamAffiliation))]
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] int maxHp = 100;
    [SerializeField] bool isInvulnerable;

    int currentHp;
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

    public bool CanReceiveDamage(DamageContext context)
    {
        if (!IsAlive) return false;
        if (isInvulnerable) return false;
        if (context.amount <= 0) return false;

        return !IsFriendlySource(context.sourceTeam);
    }

    public void TakeDamage(DamageContext context)
    {
        if (!CanReceiveDamage(context)) return;

        int finalDamage = CalculateDamage(context);
        if (finalDamage <= 0) return;

        currentHp = Mathf.Max(0, currentHp - finalDamage);
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
    }
}
