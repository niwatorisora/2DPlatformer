using UnityEngine;

/// <summary>
/// Base contract for enemy locomotion. Swap implementations (ground, flying, etc.)
/// by replacing the concrete component on the Prefab without touching AI states.
/// </summary>
public abstract class EnemyMovement : MonoBehaviour
{
    public abstract void Configure(float moveSpeed);

    /// <summary>移動プロファイルの共通値を適用する。</summary>
    public virtual void Configure(MovementProfile profile)
    {
        if (profile == null) return;
        Configure(profile.MoveSpeed);
    }

    /// <summary>Move horizontally toward the given world-space position.</summary>
    public abstract void MoveToward(Vector2 worldPosition);

    public abstract void Stop();
}
