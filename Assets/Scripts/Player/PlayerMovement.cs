using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5.0f;
    // SampleScene.unity に旧キーjumpForceが残存するため維持（SampleScene廃止時に削除可）
    [FormerlySerializedAs("jumpForce")]
    [SerializeField] float jumpVelocity = 5.0f;
    [SerializeField] float jumpBufferTime = 0.1f;
    [SerializeField] float coyoteTime = 0.1f;

    [SerializeField] Transform groundCheck;
    [SerializeField] float checkRadius = 0.2f;
    [SerializeField] LayerMask groundLayer;

    Rigidbody2D rb;
    float horizontalInput;
    bool isGrounded;
    bool hasJumped;
    float lastJumpPressedTime = float.NegativeInfinity;
    float lastGroundedTime = float.NegativeInfinity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
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
        // Physics velocity is applied in FixedUpdate while preserving vertical movement.
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

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
}
