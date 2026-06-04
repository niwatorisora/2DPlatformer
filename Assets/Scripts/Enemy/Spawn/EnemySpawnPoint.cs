using UnityEngine;

/// <summary>
/// Scene spawn location. Multiple points can share a groupId.
/// </summary>
public class EnemySpawnPoint : MonoBehaviour
{
    [Tooltip("Group identifier. Spawn entries reference this to pick a point.")]
    [SerializeField] string groupId = "default";
    public string GroupId => groupId;

    [Tooltip("Radius for Gizmo visualization and blockage check.")]
    [SerializeField] float radius = 0.5f;
    public float Radius => radius;

    [Tooltip("LayerMask used to detect whether this point is blocked.")]
    [SerializeField] LayerMask blockageMask;
    public LayerMask BlockageMask => blockageMask;

    /// <summary>
    /// Whether this spawn point is blocked by a collider within radius.
    /// </summary>
    public bool IsBlocked()
    {
        if (radius <= 0f) return false;
        return Physics2D.OverlapCircle(transform.position, radius, blockageMask) != null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);

        if (!string.IsNullOrEmpty(groupId))
            UnityEditor.Handles.Label(transform.position, $"[{groupId}]");
    }
#endif

    void OnValidate()
    {
        radius = Mathf.Max(0f, radius);
    }
}
