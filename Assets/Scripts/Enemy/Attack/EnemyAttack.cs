using UnityEngine;

/// <summary>
/// Base contract for enemy attack behaviour. Swap implementations (ranged, melee, etc.)
/// by replacing the concrete component on the Prefab without touching AI states.
/// </summary>
public abstract class EnemyAttack : MonoBehaviour
{
    public virtual bool IsAutoFire => false;

    /// <summary>EnemyController がプロファイルと実行時依存を注入する。</summary>
    public abstract void Configure(AttackProfile profile, IBulletPool bulletPool, TeamId ownerTeam);

    public abstract bool CanAttack(Transform target);

    public abstract void TryAttack(Transform target);
}
