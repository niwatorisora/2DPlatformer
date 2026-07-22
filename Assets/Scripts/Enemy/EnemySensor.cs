using UnityEngine;

/// <summary>
/// Range checks for enemy AI. Keeps distance math in one place so states stay readable.
/// The initial implementation uses straight-line distance only; add Physics2D.Raycast
/// here when line-of-sight occlusion is needed without touching the state classes.
/// </summary>
public class EnemySensor : MonoBehaviour
{
    Transform trackedTarget;
    Collider2D trackedTargetCollider;
    float detectionRange;
    float loseSightRange;

    public void Configure(Transform target, float detection, float loseSight)
    {
        trackedTarget  = target;
        trackedTargetCollider = target != null ? target.GetComponent<Collider2D>() : null;
        detectionRange = Mathf.Max(0f, detection);
        loseSightRange = Mathf.Max(detectionRange, loseSight);
    }

    /// <summary>Returns true and sets target when a tracked target is within detection range.</summary>
    public bool TryDetectTarget(out Transform target)
    {
        target = null;
        if (trackedTarget == null) return false;

        if (DistanceTo(trackedTarget) <= detectionRange)
        {
            target = trackedTarget;
            return true;
        }

        return false;
    }

    public bool IsInAttackRange(Transform target, float attackRange)
    {
        if (target == null) return false;
        return DistanceToTargetSurface(target) <= attackRange;
    }

    public bool HasLostSight(Transform target)
    {
        if (target == null) return true;
        return DistanceTo(target) > loseSightRange;
    }

    float DistanceTo(Transform target)
        => Vector2.Distance(transform.position, target.position);

    float DistanceToTargetSurface(Transform target)
    {
        Collider2D targetCollider = GetTargetCollider(target);
        if (targetCollider == null) return DistanceTo(target);

        Vector2 enemyPosition = transform.position;
        Vector2 closestPoint = targetCollider.ClosestPoint(enemyPosition);
        return Vector2.Distance(enemyPosition, closestPoint);
    }

    Collider2D GetTargetCollider(Transform target)
    {
        // 追跡対象は通常固定だが、呼び出し先が別対象でも正しい Collider を使う。
        if (target != trackedTarget)
        {
            trackedTarget = target;
            trackedTargetCollider = target != null ? target.GetComponent<Collider2D>() : null;
        }

        // 実行中に Collider が追加・削除された場合も、中心距離へ安全にフォールバックする。
        if (trackedTargetCollider == null && target != null)
            trackedTargetCollider = target.GetComponent<Collider2D>();

        return trackedTargetCollider;
    }
}
