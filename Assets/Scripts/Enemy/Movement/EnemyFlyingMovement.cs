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
    float originalGravityScale;
    float moveSpeed;
    float hoverTargetY;
    Vector2 desiredVelocity;
    bool hasMoveCommand;
    bool hasHoverTarget;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;
    }

    public override void Configure(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
        hasHoverTarget = false;
    }

    /// <summary>共有速度と飛行時の旋回係数を適用する。</summary>
    public override void Configure(MovementProfile profile)
    {
        if (profile == null) return;
        steeringSmoothing = Mathf.Clamp(profile.SteeringSmoothing, 0.01f, 1f);
        Configure(profile.MoveSpeed);
        // プール再利用時も現在地を新しい湧き地点として記録する。
        hoverTargetY = transform.position.y + profile.HoverHeight;
        hasHoverTarget = true;
    }

    public override void MoveToward(Vector2 worldPosition)
    {
        // 射撃型の飛行敵を想定し、地上の相手にも高度を保って絡む。
        worldPosition.y = Mathf.Max(worldPosition.y, hoverTargetY);
        Vector2 direction = ((Vector2)worldPosition - (Vector2)transform.position).normalized;
        desiredVelocity = direction * moveSpeed;
        hasMoveCommand = true;
    }

    public override void Stop()
    {
        desiredVelocity = Vector2.zero;
        hasMoveCommand = false;
    }

    void OnEnable()
    {
        if (rb != null) rb.gravityScale = 0f;
    }

    void OnDisable()
    {
        if (rb != null) rb.gravityScale = originalGravityScale;
    }

    void FixedUpdate()
    {
        if (!hasMoveCommand && hasHoverTarget)
        {
            // 待機中は水平方向を止め、湧き地点からの所定高度へ滑らかに戻る。
            float verticalDirection = Mathf.Sign(hoverTargetY - transform.position.y);
            desiredVelocity = Vector2.up * verticalDirection * moveSpeed;
        }

        // Lerp toward desired velocity each physics step for a smooth direction change.
        // steeringSmoothing = 1 means instant snap; lower values ease the turn.
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVelocity, steeringSmoothing);

        // MoveToward はこの物理ステップ限りの指示として扱う。
        hasMoveCommand = false;
    }

    void OnValidate()
    {
        steeringSmoothing = Mathf.Clamp(steeringSmoothing, 0.01f, 1f);
    }
}
