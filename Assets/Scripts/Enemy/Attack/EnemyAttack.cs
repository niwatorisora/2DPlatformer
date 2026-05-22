using UnityEngine;

/// <summary>
/// Base contract for enemy attack behaviour. Swap implementations (ranged, melee, etc.)
/// by replacing the concrete component on the Prefab without touching AI states.
/// </summary>
public abstract class EnemyAttack : MonoBehaviour
{
    public virtual bool IsAutoFire => false;

    /// <summary>Called once by EnemyController.Initialize to inject runtime dependencies.</summary>
    public abstract void Configure(IBulletPool bulletPool, TeamId ownerTeam);

    public abstract bool CanAttack(Transform target);

    public abstract void TryAttack(Transform target);
}
