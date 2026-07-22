using UnityEngine;

/// <summary>外部攻撃からノックバックとひるみを受け取る契約。</summary>
public interface IKnockbackReceiver
{
    void ReceiveKnockback(Vector2 velocity, float stunSeconds);
}
