using UnityEngine;

/// <summary>
/// Ranged attack that delegates burst/spread logic to ShooterCore so WeaponData
/// behaves the same way for enemies as it does for the player.
/// </summary>
public class EnemyShooterAttack : EnemyAttack
{
    [SerializeField] WeaponData weaponData;
    [SerializeField] Transform shootOrigin;

    [SerializeField] float spawnDistanceFromOrigin = 0.4f;

    WeaponData activeWeaponData;
    float activeSpawnDistanceFromOrigin;
    IBulletPool bulletPool;
    TeamId ownerTeam;
    float nextFireTime;
    Coroutine fireSequence;

    public override bool IsAutoFire => ActiveWeaponData != null && ActiveWeaponData.autoFire;

    void Awake()
    {
        if (shootOrigin == null) shootOrigin = transform;
    }

    public override void Configure(AttackProfile profile, IBulletPool pool, TeamId team)
    {
        bulletPool   = pool;
        ownerTeam    = team;
        nextFireTime = 0f;
        activeWeaponData = profile != null && profile.Kind == AttackProfile.AttackKind.Shooter
            ? profile.WeaponData : weaponData;
        activeSpawnDistanceFromOrigin = profile != null && profile.Kind == AttackProfile.AttackKind.Shooter
            ? profile.SpawnDistanceFromOrigin : spawnDistanceFromOrigin;
    }

    public override bool CanAttack(Transform target)
    {
        if (ActiveWeaponData == null || ActiveWeaponData.bulletData == null || bulletPool == null) return false;
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

        var config   = BulletConfig.From(ActiveWeaponData.bulletData);
        nextFireTime = Time.time + ActiveWeaponData.cooldown;

        // Enemy recalculates spawn position per shot direction so spread fans outward from origin.
        Vector2 SpawnPos(Vector2 d) => (Vector2)shootOrigin.position + d * activeSpawnDistanceFromOrigin;

        void Salvo() => ShooterCore.FireSpread(
            direction, config, ActiveWeaponData, bulletPool, gameObject, ownerTeam, SpawnPos);

        if (ActiveWeaponData.sequenceShotCount <= 1 || ActiveWeaponData.sequenceInterval <= 0f)
        {
            for (int i = 0; i < Mathf.Max(1, ActiveWeaponData.sequenceShotCount); i++) Salvo();
            return;
        }

        fireSequence = StartCoroutine(
            ShooterCore.FireSequenceRoutine(ActiveWeaponData, Salvo, () => fireSequence = null));
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

    WeaponData ActiveWeaponData => activeWeaponData != null ? activeWeaponData : weaponData;
}
