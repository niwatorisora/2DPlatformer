using UnityEngine;
using UnityEngine.Serialization;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] BulletPool bulletPool;
    [FormerlySerializedAs("firePoint")]
    [SerializeField] Transform shootOrigin;
    [SerializeField] float spawnDistanceFromOrigin = 0.6f;
    [SerializeField] Camera targetCamera;
    [SerializeField] WeaponData weaponData;

    Coroutine fireSequence;
    TeamAffiliation teamAffiliation;
    float nextFireTime;
    Magazine magazine;

    void Awake()
    {
        if (shootOrigin == null) shootOrigin = transform;
        if (targetCamera == null) targetCamera = Camera.main;
        // TeamAffiliation is optional, but Neutral shooters do not get friendly-fire protection.
        teamAffiliation = GetComponentInParent<TeamAffiliation>();

        magazine = GetComponent<Magazine>();
        if (magazine != null && weaponData != null) magazine.Configure(weaponData);
    }

    void Update()
    {
        bool triggered = weaponData != null && weaponData.autoFire
            ? Input.GetMouseButton(0)
            : Input.GetMouseButtonDown(0);
        if (triggered) ShootAtMouse();

        if (Input.GetKeyDown(KeyCode.R)) magazine?.StartReload();
    }

    void ShootAtMouse()
    {
        if (Time.time < nextFireTime) return;

        if (bulletPool == null)
        {
            GameLog.Error(this, "requires a bullet pool.");
            return;
        }

        if (targetCamera == null)
        {
            GameLog.Error(this, "requires a camera.");
            return;
        }

        if (weaponData == null)
        {
            GameLog.Error(this, "requires weapon data.");
            return;
        }

        if (weaponData.bulletData == null)
        {
            GameLog.Error(weaponData, "requires bullet data.");
            return;
        }

        if (magazine != null && !magazine.CanFire) return;

        Vector2 shootDirection = GetDirectionToMouse();
        if (shootDirection.sqrMagnitude <= Mathf.Epsilon) return;

        Vector2 normalizedDirection = shootDirection.normalized;
        Vector2 spawnPosition = (Vector2)shootOrigin.position + normalizedDirection * spawnDistanceFromOrigin;
        Fire(spawnPosition, normalizedDirection);
    }

    Vector2 GetDirectionToMouse()
    {
        Vector3 mouseScreen = Input.mousePosition;
        // Match the mouse projection plane to the shooter so 2D aiming stays stable.
        mouseScreen.z = shootOrigin.position.z - targetCamera.transform.position.z;
        Vector3 mouseWorld = targetCamera.ScreenToWorldPoint(mouseScreen);
        return (Vector2)mouseWorld - (Vector2)shootOrigin.position;
    }

    public void Fire(Vector2 shootPosition, Vector2 direction)
    {
        if (weaponData == null || weaponData.bulletData == null || bulletPool == null) return;
        if (direction.sqrMagnitude <= Mathf.Epsilon) return;
        if (fireSequence != null) return;

        var config = BulletConfig.From(weaponData.bulletData);
        direction = direction.normalized;
        nextFireTime = Time.time + weaponData.cooldown;

        // All spread shots fire from the same pre-calculated spawn point.
        // 1サルボにつき1発消費。弾が無ければそのサルボはスキップする。
        void Salvo()
        {
            if (magazine != null && !magazine.TryConsume(1)) return;
            ShooterCore.FireSpread(
                direction, config, weaponData, bulletPool, gameObject, GetTeamId(),
                _ => shootPosition);
            AudioHelper.TryPlay(weaponData.fireSound);
        }

        if (weaponData.sequenceShotCount <= 1 || weaponData.sequenceInterval <= 0f)
        {
            for (int i = 0; i < Mathf.Max(1, weaponData.sequenceShotCount); i++) Salvo();
            return;
        }

        fireSequence = StartCoroutine(
            ShooterCore.FireSequenceRoutine(weaponData, Salvo, () => fireSequence = null));
    }

    TeamId GetTeamId()
        => teamAffiliation != null ? teamAffiliation.TeamId : TeamId.Neutral;

    void OnValidate()
    {
        spawnDistanceFromOrigin = Mathf.Max(0f, spawnDistanceFromOrigin);
    }

    void OnDisable()
    {
        if (fireSequence == null) return;
        StopCoroutine(fireSequence);
        fireSequence = null;
    }
}
