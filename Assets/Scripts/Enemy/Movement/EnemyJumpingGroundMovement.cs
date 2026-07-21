using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Ground movement that hops at a fixed interval while actively chasing.
/// Inherits horizontal locomotion from EnemyGroundMovement so only jump logic
/// needs to be added here.
/// Attach a child GameObject as the groundCheck transform, matching the
/// same pattern used by PlayerMovement.
/// </summary>
public class EnemyJumpingGroundMovement : EnemyGroundMovement
{
    [Header("Jump")]
    [FormerlySerializedAs("jumpForce")]
    [SerializeField] float jumpVelocity = 6f;
    [Tooltip("接地中に追跡ジャンプを行う間隔（秒）。")]
    [SerializeField] float jumpInterval = 1.5f;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float checkRadius = 0.2f;
    [SerializeField] LayerMask groundLayer;

    Rigidbody2D rb;
    float nextJumpTime;
    bool isMoveActive;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        if (groundCheck == null)
            GameLog.Warning(this, "groundCheck is not assigned; jump will be disabled.");
    }

    public override void Configure(float speed)
    {
        base.Configure(speed);
        // プール再利用時に前回のジャンプクールダウンを持ち越さない。
        nextJumpTime = 0f;
    }

    public override void MoveToward(Vector2 worldPosition)
    {
        base.MoveToward(worldPosition);
        isMoveActive = true;
    }

    public override void Stop()
    {
        base.Stop();
        isMoveActive = false;
    }

    void Update()
    {
        if (!isMoveActive) return;
        TryJump();
    }

    void TryJump()
    {
        if (Time.time < nextJumpTime) return;
        if (!IsGrounded()) return;

        // 高さ条件ではプレイヤーのジャンプに同期してしまうため使用しない。
        // 追跡中は接地ごとに一定間隔でホップする。
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
        nextJumpTime = Time.time + jumpInterval;
    }

    bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
    }

    void OnValidate()
    {
        jumpVelocity = Mathf.Max(0f, jumpVelocity);
        jumpInterval = Mathf.Max(0f, jumpInterval);
        checkRadius = Mathf.Max(0f, checkRadius);
    }
}
