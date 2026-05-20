using System.Collections;
using UnityEngine;

/// <summary>
/// Ranged attack that reuses WeaponData and IBulletPool.
/// Mirrors PlayerShooter's spread and burst logic so shotgun/burst WeaponData
/// work the same way for enemies as they do for the player.
/// </summary>
public class EnemyShooterAttack : EnemyAttack
{
    [SerializeField] Transform shootOrigin;
    [SerializeField] float spawnDistanceFromOrigin = 0.4f;

    WeaponData weaponData;
    IBulletPool bulletPool;
    TeamId ownerTeam;
    float nextFireTime;
    Coroutine fireSequence;

    void Awake()
    {
        if (shootOrigin == null) shootOrigin = transform;
    }

    public override void Configure(EnemyData data, IBulletPool pool, TeamId team)
    {
        weaponData   = data.weaponData;
        bulletPool   = pool;
        ownerTeam    = team;
        nextFireTime = 0f;
    }

    public override bool CanAttack(Transform target)
    {
        if (weaponData == null || weaponData.bulletData == null || bulletPool == null) return false;
        if (target == null) return false;
        // Block overlapping bursts the same way PlayerShooter does.
        if (fireSequence != null) return false;
        return Time.time >= nextFireTime;
    }

    public override void TryAttack(Transform target)
    {
        if (!CanAttack(target)) return;

        Vector2 direction = ((Vector2)target.position - (Vector2)shootOrigin.position).normalized;
        if (direction.sqrMagnitude <= Mathf.Epsilon) return;

        var config = BulletConfig.From(weaponData.bulletData);
        nextFireTime = Time.time + weaponData.cooldown;

        if (weaponData.sequenceShotCount <= 1 || weaponData.sequenceInterval <= 0f)
        {
            for (int i = 0; i < Mathf.Max(1, weaponData.sequenceShotCount); i++)
                FireSimultaneousShots(direction, config);
            return;
        }

        fireSequence = StartCoroutine(FireSequence(direction, config));
    }

    IEnumerator FireSequence(Vector2 direction, BulletConfig config)
    {
        int shotCount = Mathf.Max(1, weaponData.sequenceShotCount);
        float interval = Mathf.Max(0f, weaponData.sequenceInterval);

        for (int i = 0; i < shotCount; i++)
        {
            FireSimultaneousShots(direction, config);
            if (i < shotCount - 1)
                yield return new WaitForSeconds(interval);
        }

        fireSequence = null;
    }

    void FireSimultaneousShots(Vector2 direction, BulletConfig config)
    {
        int shotCount    = Mathf.Max(1, weaponData.simultaneousShotCount);
        float spreadAngle = Mathf.Max(0f, weaponData.spreadAngle);

        if (shotCount == 1 || spreadAngle <= 0f)
        {
            Vector2 spawnPos = (Vector2)shootOrigin.position + direction * spawnDistanceFromOrigin;
            bulletPool.Shoot(spawnPos, direction, config, gameObject, ownerTeam);
            return;
        }

        float angleStep  = spreadAngle / (shotCount - 1);
        float startAngle = -spreadAngle * 0.5f;

        // Spread is centered around the direction to the target, matching PlayerShooter behaviour.
        for (int i = 0; i < shotCount; i++)
        {
            float angle      = startAngle + angleStep * i;
            Vector2 shotDir  = Quaternion.Euler(0f, 0f, angle) * direction;
            Vector2 spawnPos = (Vector2)shootOrigin.position + shotDir * spawnDistanceFromOrigin;
            bulletPool.Shoot(spawnPos, shotDir, config, gameObject, ownerTeam);
        }
    }

    void OnDisable()
    {
        if (fireSequence == null) return;
        StopCoroutine(fireSequence);
        fireSequence = null;
    }

    void OnValidate()
    {
        spawnDistanceFromOrigin = Mathf.Max(0f, spawnDistanceFromOrigin);
    }
}
