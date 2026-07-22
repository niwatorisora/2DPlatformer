using UnityEngine;

/// <summary>攻撃種別を問わず共通で使うノックバック計算。</summary>
public static class KnockbackUtility
{
    public static void Apply(Health victim, Vector2 origin, float force, float upwardRatio,
        float stunSeconds, Vector2 inheritedVelocity = default)
    {
        if (victim == null || force <= 0f) return;

        Rigidbody2D body = victim.GetComponent<Rigidbody2D>();
        float horizontalSign = Mathf.Sign(victim.transform.position.x - origin.x);
        if (horizontalSign == 0f)
        {
            horizontalSign = body != null ? -Mathf.Sign(body.linearVelocity.x) : 0f;
            if (horizontalSign == 0f) horizontalSign = 1f;
        }

        Vector2 direction = new Vector2(horizontalSign, Mathf.Max(0f, upwardRatio)).normalized;
        Vector2 velocity = direction * force + inheritedVelocity;
        IKnockbackReceiver receiver = victim.GetComponent(typeof(IKnockbackReceiver)) as IKnockbackReceiver;
        if (receiver != null) receiver.ReceiveKnockback(velocity, stunSeconds);
        else if (body != null) body.linearVelocity = velocity;
    }
}
