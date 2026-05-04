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
