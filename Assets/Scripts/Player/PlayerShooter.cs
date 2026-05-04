using System.Collections;
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

    void Awake()
    {
        if (shootOrigin == null) shootOrigin = transform;
        if (targetCamera == null) targetCamera = Camera.main;
        // TeamAffiliation is optional, but Neutral shooters do not get friendly-fire protection.
        teamAffiliation = GetComponentInParent<TeamAffiliation>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) ShootAtMouse();
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
        Vector2 normalizedDirection = direction.normalized;

        // Cooldown gates trigger pulls; an active sequence also blocks overlapping bursts.
        nextFireTime = Time.time + weaponData.cooldown;
        if (weaponData.sequenceShotCount <= 1 || weaponData.sequenceInterval <= 0f)
        {
            for (int i = 0; i < weaponData.sequenceShotCount; i++)
                FireSimultaneousShots(shootPosition, normalizedDirection, config);
            return;
        }

        fireSequence = StartCoroutine(FireSequence(shootPosition, normalizedDirection, config));
    }

    IEnumerator FireSequence(Vector2 shootPosition, Vector2 direction, BulletConfig config)
    {
        int shotCount = Mathf.Max(1, weaponData.sequenceShotCount);
        float interval = Mathf.Max(0f, weaponData.sequenceInterval);

        for (int i = 0; i < shotCount; i++)
        {
            FireSimultaneousShots(shootPosition, direction, config);
            if (i < shotCount - 1)
                yield return new WaitForSeconds(interval);
        }

        fireSequence = null;
    }

    void FireSimultaneousShots(Vector2 shootPosition, Vector2 direction, BulletConfig config)
    {
        int shotCount = Mathf.Max(1, weaponData.simultaneousShotCount);
        float spreadAngle = Mathf.Max(0f, weaponData.spreadAngle);

        if (shotCount == 1 || spreadAngle <= 0f)
        {
            bulletPool.Shoot(shootPosition, direction, config, gameObject, GetTeamId());
            return;
        }

        float angleStep = spreadAngle / (shotCount - 1);
        float startAngle = -spreadAngle * 0.5f;

        // Spread is centered around the requested direction.
        for (int i = 0; i < shotCount; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector2 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction;
            bulletPool.Shoot(shootPosition, shotDirection, config, gameObject, GetTeamId());
        }
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
