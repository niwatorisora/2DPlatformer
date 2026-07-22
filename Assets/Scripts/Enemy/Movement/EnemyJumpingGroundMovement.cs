using UnityEngine;

/// <summary>
/// Ground movement that hops at a fixed interval while actively chasing.
/// Inherits horizontal locomotion from EnemyGroundMovement so only jump logic
/// needs to be added here.
/// Attach a child GameObject as the groundCheck transform, matching the
/// same pattern used by PlayerMovement.
/// </summary>
public class EnemyJumpingGroundMovement : EnemyGroundMovement
{
    static bool warnedAboutEmptyGroundLayer;

    [Header("Jump")]
    [SerializeField] float jumpVelocity = 6f;
    [Tooltip("接地中に追跡ジャンプを行う間隔（秒）。")]
    [SerializeField] float jumpInterval = 1.5f;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float checkRadius = 0.2f;
    [SerializeField] LayerMask groundLayer;

    Rigidbody2D rb;
    Collider2D mainCollider;
    float nextJumpTime;
    bool isMoveActive;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        mainCollider = GetComponent<Collider2D>();
    }

    public override void Configure(float speed)
    {
        base.Configure(speed);
        // 同フレーム出現の個体が同期して跳ぶのを防ぐ。
        nextJumpTime = Time.time + Random.Range(0f, jumpInterval);
    }

    /// <summary>共有速度とジャンプ用の値を適用し、次回ジャンプ時刻を再同期する。</summary>
    public override void Configure(MovementProfile profile)
    {
        if (profile == null) return;
        jumpInterval = Mathf.Max(0f, profile.JumpInterval);
        jumpVelocity = Mathf.Max(0f, profile.JumpVelocity);
        Configure(profile.MoveSpeed);
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
        nextJumpTime = Time.time + jumpInterval * Random.Range(0.85f, 1.15f);
    }

    bool IsGrounded()
    {
        LayerMask mask = ResolveGroundLayer();
        if (mask.value == 0) return false;

        Vector2 checkPosition = groundCheck != null
            ? groundCheck.position
            : GetColliderBottomWorld();
        return Physics2D.OverlapCircle(checkPosition, checkRadius, mask);
    }

    LayerMask ResolveGroundLayer()
    {
        if (groundLayer.value != 0) return groundLayer;

        int groundLayerIndex = LayerMask.NameToLayer("Ground");
        if (!warnedAboutEmptyGroundLayer)
        {
            warnedAboutEmptyGroundLayer = true;
            string fallback = groundLayerIndex >= 0
                ? "暫定的に Ground レイヤーを使用します。"
                : "Ground レイヤーが存在しないため接地判定できません。";
            GameLog.Warning(this,
                $"EnemyJumpingGroundMovement のPrefabフィールド「Ground Layer」が Nothing です。Ground を設定してください。{fallback}");
        }

        return groundLayerIndex >= 0 ? LayerMask.GetMask("Ground") : (LayerMask)0;
    }

    Vector2 GetColliderBottomWorld()
    {
        if (mainCollider == null) return transform.position;

        // Collider のローカル下端を使い、VisualRoot の位置合わせと同じ基準で判定する。
        Vector2 localBottom = mainCollider switch
        {
            BoxCollider2D box => box.offset + Vector2.down * (box.size.y * 0.5f),
            CapsuleCollider2D capsule => capsule.offset + Vector2.down * (capsule.size.y * 0.5f),
            CircleCollider2D circle => circle.offset + Vector2.down * circle.radius,
            _ => new Vector2(mainCollider.bounds.center.x, mainCollider.bounds.min.y)
        };
        return mainCollider is BoxCollider2D or CapsuleCollider2D or CircleCollider2D
            ? mainCollider.transform.TransformPoint(localBottom)
            : localBottom;
    }

    void OnValidate()
    {
        jumpVelocity = Mathf.Max(0f, jumpVelocity);
        jumpInterval = Mathf.Max(0f, jumpInterval);
        checkRadius = Mathf.Max(0f, checkRadius);
    }
}
