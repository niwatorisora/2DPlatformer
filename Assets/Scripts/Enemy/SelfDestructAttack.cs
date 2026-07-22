using System.Collections;
using UnityEngine;

/// <summary>接敵後に導火線を点滅させ、自身を爆発させる攻撃実行器。</summary>
public class SelfDestructAttack : EnemyAttack
{
    const float BlinkInterval = 0.08f;
    AttackProfile profile;
    Coroutine fuseRoutine;
    SpriteRenderer spriteRenderer;
    bool originalVisibility;

    public override bool IsAutoFire => true;

    public override void Configure(AttackProfile attackProfile, IBulletPool bulletPool, TeamId ownerTeam)
    {
        CancelFuse();
        profile = attackProfile;
    }

    public override bool CanAttack(Transform target) => profile != null
        && profile.Kind == AttackProfile.AttackKind.SelfDestruct;

    public override void TryAttack(Transform target)
    {
        if (fuseRoutine != null || !CanAttack(target)) return;
        GameLog.Debug(this, $"自爆: 導火線着火 ({profile.FuseSeconds}秒)");
        fuseRoutine = StartCoroutine(Fuse());
    }

    void OnDisable() => CancelFuse();

    IEnumerator Fuse()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer != null) originalVisibility = spriteRenderer.enabled;
        float elapsed = 0f;
        while (elapsed < profile.FuseSeconds)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(BlinkInterval);
            elapsed += BlinkInterval;
        }
        RestoreSprite();
        fuseRoutine = null;
        GameLog.Debug(this, $"自爆: 起爆 半径{profile.Explosion.radius}");
        Explosion.Spawn(transform.position, profile.Explosion, gameObject);

        Health health = GetComponent<Health>();
        Collider2D ownCollider = GetComponent<Collider2D>();
        if (health != null)
        {
            // Neutral 扱いにして同チーム拒否を避け、通常の死亡・スコア・despawn 経路を使う。
            health.TakeDamage(new DamageContext(health.MaxHp, gameObject, TeamId.Neutral, ownCollider));
        }
    }

    void CancelFuse()
    {
        if (fuseRoutine != null) StopCoroutine(fuseRoutine);
        fuseRoutine = null;
        RestoreSprite();
    }

    void RestoreSprite()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = originalVisibility;
        spriteRenderer = null;
    }
}
