using UnityEngine;

/// <summary>
/// Ranged attack that delegates burst/spread logic to ShooterCore so WeaponData
/// behaves the same way for enemies as it does for the player.
/// </summary>
public class EnemyShooterAttack : EnemyAttack
{
    [SerializeField] WeaponData weaponData;
    [SerializeField] Transform shootOrigin;

    public override bool IsAutoFire => weaponData != null && weaponData.autoFire;
    [SerializeField] float spawnDistanceFromOrigin = 0.4f;

    IBulletPool bulletPool;
    TeamId ownerTeam;
    float nextFireTime;
    Coroutine fireSequence;

    void Awake()
    {
        if (shootOrigin == null) shootOrigin = transform;
    }

    public override void Configure(IBulletPool pool, TeamId team)
    {
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

        var config   = BulletConfig.From(weaponData.bulletData);
        nextFireTime = Time.time + weaponData.cooldown;

        // Enemy recalculates spawn position per shot direction so spread fans outward from origin.
        Vector2 SpawnPos(Vector2 d) => (Vector2)shootOrigin.position + d * spawnDistanceFromOrigin;

        void Salvo() => ShooterCore.FireSpread(
            direction, config, weaponData, bulletPool, gameObject, ownerTeam, SpawnPos);

        if (weaponData.sequenceShotCount <= 1 || weaponData.sequenceInterval <= 0f)
        {
            for (int i = 0; i < Mathf.Max(1, weaponData.sequenceShotCount); i++) Salvo();
            return;
        }

        fireSequence = StartCoroutine(
            ShooterCore.FireSequenceRoutine(weaponData, Salvo, () => fireSequence = null));
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
