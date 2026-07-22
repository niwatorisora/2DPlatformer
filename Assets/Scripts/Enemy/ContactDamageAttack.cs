using System.Collections.Generic;
using UnityEngine;

/// <summary>物理的に接触した敵対者へダメージを与える攻撃実行器。</summary>
public class ContactDamageAttack : EnemyAttack
{
    readonly Dictionary<Health, float> lastHitTime = new();
    AttackProfile profile;
    TeamId ownerTeam;

    public override bool IsAutoFire => true;
    public override void Configure(AttackProfile attackProfile, IBulletPool bulletPool, TeamId team)
    {
        profile = attackProfile;
        ownerTeam = team;
        lastHitTime.Clear();
    }

    public override bool CanAttack(Transform target) => true;
    public override void TryAttack(Transform target) { }
    void OnCollisionStay2D(Collision2D collision)
    {
        if (profile != null && profile.Kind == AttackProfile.AttackKind.Contact) TryHit(collision.collider);
    }

    void TryHit(Collider2D victimCollider)
    {
        Health victim = victimCollider.GetComponentInParent<Health>();
        TeamAffiliation victimTeam = victimCollider.GetComponentInParent<TeamAffiliation>();
        if (victim == null || victimTeam == null || TeamAffiliation.AreFriendly(ownerTeam, victimTeam.TeamId)) return;
        PruneHitTimes();
        if (lastHitTime.TryGetValue(victim, out float time) && Time.time < time + profile.HitCooldownSeconds) return;
        var context = new DamageContext(Mathf.RoundToInt(profile.Damage), gameObject, ownerTeam, victimCollider);
        if (!victim.CanReceiveDamage(context)) return;
        victim.TakeDamage(context);
        lastHitTime[victim] = Time.time;
        ApplyKnockback(victim);
    }

    void ApplyKnockback(Health victim)
    {
        Rigidbody2D body = victim.GetComponent<Rigidbody2D>();
        if (body == null || profile.KnockbackForce <= 0f) return;
        float horizontalSign = Mathf.Sign(victim.transform.position.x - transform.position.x);
        if (horizontalSign == 0f)
        {
            horizontalSign = -Mathf.Sign(body.linearVelocity.x);
            if (horizontalSign == 0f) horizontalSign = 1f;
        }
        // どこから触れても「後ろ斜め上」に飛ばし、ノックバックを体感で分からせる。
        Vector2 direction = new Vector2(horizontalSign, profile.KnockbackUpwardRatio).normalized;
        Rigidbody2D attackerBody = GetComponent<Rigidbody2D>();
        Vector2 attackerVelocity = attackerBody != null ? attackerBody.linearVelocity : Vector2.zero;
        Vector2 finalVelocity = direction * profile.KnockbackForce
            + attackerVelocity * profile.AttackerVelocityInheritance;
        IKnockbackReceiver receiver = victim.GetComponent(typeof(IKnockbackReceiver)) as IKnockbackReceiver;
        if (receiver != null)
            receiver.ReceiveKnockback(finalVelocity, profile.HitstunSeconds);
        else
            body.linearVelocity = finalVelocity;
    }

    void PruneHitTimes()
    {
        List<Health> stale = null;
        foreach (var entry in lastHitTime)
            if (entry.Key == null || !entry.Key.IsAlive) (stale ??= new()).Add(entry.Key);
        if (stale != null) foreach (Health victim in stale) lastHitTime.Remove(victim);
    }

    void OnDisable() => lastHitTime.Clear();
}
