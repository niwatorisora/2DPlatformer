using UnityEngine;

[CreateAssetMenu(fileName = "NewBullet", menuName = "Combat/Bullet Data")]
public class BulletData : ScriptableObject
{
    [Header("Motion")]
    public float speed = 12f;
    public float lifeTime = 2f;
    public bool useGravity;
    public float gravityScale = 1f;

    [Header("Hit")]
    public int damage = 10;
    public LayerMask hitMask = ~0;

    void OnValidate()
    {
        speed = Mathf.Max(0f, speed);
        lifeTime = Mathf.Max(0.01f, lifeTime);
        damage = Mathf.Max(0, damage);
        gravityScale = Mathf.Max(0f, gravityScale);
    }
}

/// <summary>
/// Immutable runtime copy of BulletData, captured at the moment a shot is fired.
/// </summary>
public readonly struct BulletConfig
{
    public readonly float speed;
    public readonly float lifeTime;
    public readonly int   damage;
    public readonly bool  useGravity;
    public readonly float gravityScale;
    public readonly int   hitMask;

    public BulletConfig(float speed, float lifeTime, int damage, bool useGravity, float gravityScale, int hitMask)
    {
        this.speed        = speed;
        this.lifeTime     = lifeTime;
        this.damage       = damage;
        this.useGravity   = useGravity;
        this.gravityScale = gravityScale;
        this.hitMask      = hitMask;
    }

    public static BulletConfig From(BulletData data)
        => new BulletConfig(data.speed, data.lifeTime, data.damage, data.useGravity, data.gravityScale, data.hitMask.value);
}
