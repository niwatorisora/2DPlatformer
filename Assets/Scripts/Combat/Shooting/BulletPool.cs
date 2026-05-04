using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletPool : MonoBehaviour, IBulletPool
{
    [SerializeField] Bullet bulletPrefab;
    [SerializeField] int defaultCapacity = 20;
    [SerializeField] int maxSize = 100;
    [SerializeField] Transform container;

    ObjectPool<Bullet> pool;

    void Awake()
    {
        ClampSettings();

        if (container == null) container = transform;

        // ObjectPool centralizes bullet reuse so rapid fire does not constantly allocate GameObjects.
        pool = new ObjectPool<Bullet>(
            CreateBullet,
            OnTakeFromPool,
            OnReturnedToPool,
            OnDestroyPoolObject,
            true,
            defaultCapacity,
            maxSize);

        Prewarm();
    }

    public void Shoot(Vector2 position, Vector2 direction, BulletConfig config, GameObject owner, TeamId ownerTeam)
    {
        if (bulletPrefab == null)
        {
            GameLog.Error(this, "requires a bullet prefab.");
            return;
        }

        if (direction.sqrMagnitude <= Mathf.Epsilon) return;

        Bullet bullet = pool.Get();
        bullet.transform.position = position;
        Vector2 normalizedDirection = direction.normalized;
        bullet.transform.up = normalizedDirection;
        bullet.Launch(() => Release(bullet), normalizedDirection, config, owner, ownerTeam);
    }

    void Release(Bullet bullet) => pool.Release(bullet);

    Bullet CreateBullet()
    {
        Bullet bullet = Instantiate(bulletPrefab, container);
        bullet.gameObject.SetActive(false);
        return bullet;
    }

    void OnTakeFromPool(Bullet bullet) => bullet.gameObject.SetActive(true);

    void OnReturnedToPool(Bullet bullet)
    {
        bullet.ResetForPool();
        bullet.gameObject.SetActive(false);
    }

    void OnDestroyPoolObject(Bullet bullet) => Destroy(bullet.gameObject);

    void Prewarm()
    {
        if (bulletPrefab == null || defaultCapacity <= 0) return;

        // Fill the pool during Awake so the first shots do not pay instantiation cost.
        var prewarmed = new List<Bullet>(defaultCapacity);
        for (int i = 0; i < defaultCapacity; i++)
            prewarmed.Add(pool.Get());
        for (int i = 0; i < prewarmed.Count; i++)
            pool.Release(prewarmed[i]);
    }

    void OnValidate() => ClampSettings();

    void ClampSettings()
    {
        defaultCapacity = Mathf.Max(0, defaultCapacity);
        maxSize = Mathf.Max(1, maxSize);
        if (maxSize < defaultCapacity) maxSize = defaultCapacity;
    }
}
