using UnityEngine;

/// <summary>
/// Full 2D movement for flying enemies. Gravity is disabled on Awake and the
/// enemy moves directly toward the target in both axes.
/// Replace EnemyGroundMovement with this component on the Prefab to make any
/// enemy type fly without changing the AI state classes.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyFlyingMovement : EnemyMovement
{
    [Tooltip("How tightly the enemy tracks the target direction. 1 = instant, lower = smoother.")]
    [SerializeField, Range(0.01f, 1f)] float steeringSmoothing = 0.15f;

    Rigidbody2D rb;
    float moveSpeed;
    Vector2 desiredVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Flying enemies are not affected by gravity.
        rb.gravityScale = 0f;
    }

    public override void Configure(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
    }

    public override void MoveToward(Vector2 worldPosition)
    {
        Vector2 direction = ((Vector2)worldPosition - (Vector2)transform.position).normalized;
        desiredVelocity = direction * moveSpeed;
    }

    public override void Stop()
    {
        desiredVelocity = Vector2.zero;
    }

    void FixedUpdate()
    {
        // Lerp toward desired velocity each physics step for a smooth direction change.
        // steeringSmoothing = 1 means instant snap; lower values ease the turn.
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, steeringSmoothing);
    }

    void OnValidate()
    {
        steeringSmoothing = Mathf.Clamp(steeringSmoothing, 0.01f, 1f);
    }
}
