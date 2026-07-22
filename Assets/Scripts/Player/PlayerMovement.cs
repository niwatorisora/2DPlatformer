using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour, IKnockbackReceiver
{
    [SerializeField] float moveSpeed = 5.0f;
    // SampleScene.unity に旧キーjumpForceが残存するため維持（SampleScene廃止時に削除可）
    [FormerlySerializedAs("jumpForce")]
    [SerializeField] float jumpVelocity = 5.0f;
    [SerializeField] float jumpBufferTime = 0.1f;
    [SerializeField] float coyoteTime = 0.1f;
    [SerializeField, Tooltip("ひるみ明けに慣性が入力へ移行するブレンド時間")]
    float knockbackRecoverySeconds = 0.2f;

    [SerializeField] Transform groundCheck;
    [SerializeField] float checkRadius = 0.2f;
    [SerializeField] LayerMask groundLayer;

    Rigidbody2D rb;
    float horizontalInput;
    bool isGrounded;
    bool hasJumped;
    float lastJumpPressedTime = float.NegativeInfinity;
    float lastGroundedTime = float.NegativeInfinity;
    float hitstunEndTime;
    float knockbackRecoveryStartTime = float.NegativeInfinity;
    bool shouldStartKnockbackRecovery;
    bool isRecoveringFromKnockback;
    bool wasHitstunned;
    SpriteRenderer spriteRenderer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Transform visualRoot = transform.Find("VisualRoot");
        if (visualRoot != null)
            spriteRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>(true);
    }

    void Update()
    {
        UpdateHitstunVisual();

        if (IsHitstunned) return;

        // GetAxis は反転時に 0 を経由して速度が一時停止するため、即時反転できる GetAxisRaw を使う。
        horizontalInput = Input.GetAxisRaw("Horizontal");
        // Ground check stays in Update so jump input and grounded state are sampled together.
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        if (isGrounded)
        {
            lastGroundedTime = Time.time;

            // 着地後に地面判定が残ったまま跳躍上昇できない場合でも hasJumped が固定されないよう、
            // 接地かつ下降・静止中は次のジャンプを許可する。
            if (rb.linearVelocity.y <= 0f) hasJumped = false;
        }

        if (!isGrounded) hasJumped = false;

        if (Input.GetButtonDown("Jump"))
        {
            // バッファ＝着地直前の先行入力を拾う。
            lastJumpPressedTime = Time.time;
        }
    }

    void FixedUpdate()
    {
        // 横速度の毎フレーム上書きがノックバックを打ち消すため、ひるみ中は上書きを停止して物理に主導権を渡す。
        if (IsHitstunned) return;

        if (shouldStartKnockbackRecovery)
        {
            shouldStartKnockbackRecovery = false;
            isRecoveringFromKnockback = knockbackRecoverySeconds > 0f;
            knockbackRecoveryStartTime = Time.time;
        }

        if (isRecoveringFromKnockback)
        {
            // ひるみ明けに慣性を即没収せず、入力へ滑らかに主導権を返す（真下落下の違和感対策）。
            float t = Mathf.Clamp01((Time.time - knockbackRecoveryStartTime) / knockbackRecoverySeconds);
            rb.linearVelocity = new Vector2(
                Mathf.Lerp(rb.linearVelocity.x, horizontalInput * moveSpeed, t),
                rb.linearVelocity.y);
            if (t >= 1f) isRecoveringFromKnockback = false;
        }
        else
        {
            // Physics velocity is applied in FixedUpdate while preserving vertical movement.
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }

        bool hasJumpBuffer = Time.time - lastJumpPressedTime <= jumpBufferTime;
        // コヨーテ＝崖離れ直後の猶予。
        bool isWithinCoyoteTime = Time.time - lastGroundedTime <= coyoteTime;

        if (hasJumpBuffer && isWithinCoyoteTime && !hasJumped)
        {
            // AddForce では落下速度に相殺されて不発になるのを防ぐ。
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);
            hasJumped = true;
            // 両方をリセットして二段ジャンプ防止。
            lastJumpPressedTime = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
        }
    }

    public void ReceiveKnockback(Vector2 velocity, float stunSeconds)
    {
        rb.linearVelocity = velocity;
        hitstunEndTime = Time.time + Mathf.Max(0f, stunSeconds);
        shouldStartKnockbackRecovery = true;
        isRecoveringFromKnockback = false;
        lastJumpPressedTime = float.NegativeInfinity;
        wasHitstunned = IsHitstunned;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
    }

    bool IsHitstunned => Time.time < hitstunEndTime;

    void UpdateHitstunVisual()
    {
        if (spriteRenderer == null) return;
        if (!IsHitstunned)
        {
            if (wasHitstunned) spriteRenderer.enabled = true;
            wasHitstunned = false;
            return;
        }

        wasHitstunned = true;
        spriteRenderer.enabled = Mathf.FloorToInt(Time.time / 0.08f) % 2 == 0;
    }

    void OnDisable()
    {
        if (spriteRenderer != null) spriteRenderer.enabled = true;
    }
}
