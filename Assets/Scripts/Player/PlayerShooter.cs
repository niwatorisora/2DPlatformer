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

        var config    = BulletConfig.From(weaponData.bulletData);
        direction     = direction.normalized;
        nextFireTime  = Time.time + weaponData.cooldown;

        // All spread shots fire from the same pre-calculated spawn point.
        void Salvo() => ShooterCore.FireSpread(
            direction, config, weaponData, bulletPool, gameObject, GetTeamId(),
            _ => shootPosition);

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
