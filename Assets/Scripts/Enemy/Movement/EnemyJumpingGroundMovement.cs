using UnityEngine;

/// <summary>
/// Ground movement that automatically jumps when the target is above the enemy
/// or at a fixed interval. Inherits horizontal locomotion from EnemyGroundMovement
/// so only jump logic needs to be added here.
/// Attach a child GameObject as the groundCheck transform, matching the
/// same pattern used by PlayerMovement.
/// </summary>
public class EnemyJumpingGroundMovement : EnemyGroundMovement
{
    [Header("Jump")]
    [SerializeField] float jumpForce = 6f;
    [Tooltip("Minimum seconds between jumps.")]
    [SerializeField] float jumpCooldown = 1.5f;
    [Tooltip("Jump when target is this many units above the enemy.")]
    [SerializeField] float jumpHeightThreshold = 1f;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float checkRadius = 0.2f;
    [SerializeField] LayerMask groundLayer;

    Rigidbody2D rb;
    float nextJumpTime;
    Vector2 lastTargetPosition;
    bool isMoveActive;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        if (groundCheck == null)
            GameLog.Warning(this, "groundCheck is not assigned; jump will be disabled.");
    }

    public override void MoveToward(Vector2 worldPosition)
    {
        base.MoveToward(worldPosition);
        lastTargetPosition = worldPosition;
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
        TryJump(lastTargetPosition);
    }

    void TryJump(Vector2 targetPosition)
    {
        if (Time.time < nextJumpTime) return;
        if (!IsGrounded()) return;

        bool targetIsAbove = targetPosition.y > transform.position.y + jumpHeightThreshold;
        if (!targetIsAbove) return;

        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        nextJumpTime = Time.time + jumpCooldown;
    }

    bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
    }

    void OnValidate()
    {
        jumpForce        = Mathf.Max(0f, jumpForce);
        jumpCooldown     = Mathf.Max(0f, jumpCooldown);
        checkRadius      = Mathf.Max(0f, checkRadius);
        jumpHeightThreshold = Mathf.Max(0f, jumpHeightThreshold);
    }
}
