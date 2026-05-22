using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Burst-and-spread shooting logic shared by PlayerShooter and EnemyShooterAttack.
/// Not a MonoBehaviour; callers pass themselves in so coroutines run on the correct object.
/// </summary>
internal static class ShooterCore
{
    /// <summary>
    /// Returns a coroutine that fires <paramref name="doOneSalvo"/> on each step of the sequence.
    /// Start it with the caller's StartCoroutine; pass <c>() => fireSequence = null</c> as onComplete.
    /// </summary>
    internal static IEnumerator FireSequenceRoutine(
        WeaponData weaponData,
        Action doOneSalvo,
        Action onComplete)
    {
        int shotCount  = Mathf.Max(1, weaponData.sequenceShotCount);
        float interval = Mathf.Max(0f, weaponData.sequenceInterval);

        for (int i = 0; i < shotCount; i++)
        {
            doOneSalvo();
            if (i < shotCount - 1)
                yield return new WaitForSeconds(interval);
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// Fires simultaneousShotCount bullets spread around <paramref name="direction"/>.
    /// <paramref name="getSpawnPos"/> converts a shot direction into a world-space spawn point,
    /// allowing callers to use a fixed position (player) or a per-shot offset (enemy).
    /// </summary>
    internal static void FireSpread(
        Vector2 direction,
        BulletConfig config,
        WeaponData weaponData,
        IBulletPool bulletPool,
        GameObject owner,
        TeamId ownerTeam,
        Func<Vector2, Vector2> getSpawnPos)
    {
        int shotCount     = Mathf.Max(1, weaponData.simultaneousShotCount);
        float spreadAngle = Mathf.Max(0f, weaponData.spreadAngle);

        if (shotCount == 1 || spreadAngle <= 0f)
        {
            bulletPool.Shoot(getSpawnPos(direction), direction, config, owner, ownerTeam);
            return;
        }

        float angleStep  = spreadAngle / (shotCount - 1);
        float startAngle = -spreadAngle * 0.5f;

        for (int i = 0; i < shotCount; i++)
        {
            float   angle   = startAngle + angleStep * i;
            Vector2 shotDir = Quaternion.Euler(0f, 0f, angle) * direction;
            bulletPool.Shoot(getSpawnPos(shotDir), shotDir, config, owner, ownerTeam);
        }
    }
}
