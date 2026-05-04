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
