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
