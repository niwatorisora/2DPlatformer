using UnityEngine;

/// <summary>通常接近から予告付きの水平突進へ移行する地上敵用の移動実行器。</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyChargeMovement : EnemyMovement
{
    const float BlinkInterval = 0.1f;
    const float RecoverySeconds = 0.4f;

    enum ChargePhase { Approach, Windup, Dash, Recovery }

    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    ChargePhase phase;
    float moveSpeed;
    float triggerRange;
    float windupSeconds;
    float chargeSpeed;
    float dashDurationSeconds;
    float cooldownSeconds;
    float phaseEndTime;
    float cooldownEndTime;
    float nextBlinkTime;
    float lockedDirection;
    float targetVelocityX;
    bool spriteVisibilityCached;
    bool originalVisibility;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void Configure(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
        ResetCharge();
    }

    public override void Configure(MovementProfile profile)
    {
        if (profile == null) return;

        moveSpeed = Mathf.Max(0f, profile.MoveSpeed);
        triggerRange = Mathf.Max(0f, profile.ChargeTriggerRange);
        windupSeconds = Mathf.Max(0f, profile.ChargeWindupSeconds);
        chargeSpeed = Mathf.Max(0f, profile.ChargeSpeed);
        dashDurationSeconds = Mathf.Max(0f, profile.ChargeDurationSeconds);
        cooldownSeconds = Mathf.Max(0f, profile.ChargeCooldownSeconds);
        ResetCharge();
    }

    public override void MoveToward(Vector2 worldPosition)
    {
        if (phase != ChargePhase.Approach) return;

        float horizontalDelta = worldPosition.x - transform.position.x;
        targetVelocityX = Mathf.Sign(horizontalDelta) * moveSpeed;
        if (Mathf.Abs(horizontalDelta) <= triggerRange && Time.time >= cooldownEndTime)
            BeginWindup(horizontalDelta);
    }

    public override void Stop()
    {
        ResetCharge();
    }

    void OnDisable()
    {
        ResetCharge();
    }

    void Update()
    {
        if (phase == ChargePhase.Windup)
        {
            if (Time.time >= nextBlinkTime)
            {
                if (spriteRenderer != null) spriteRenderer.enabled = !spriteRenderer.enabled;
                nextBlinkTime += BlinkInterval;
            }
            if (Time.time >= phaseEndTime) BeginDash();
        }
        else if (phase == ChargePhase.Dash && Time.time >= phaseEndTime)
        {
            BeginRecovery();
        }
        else if (phase == ChargePhase.Recovery && Time.time >= phaseEndTime)
        {
            phase = ChargePhase.Approach;
            targetVelocityX = 0f;
        }
    }

    void FixedUpdate()
    {
        float velocityX = phase == ChargePhase.Dash ? lockedDirection * chargeSpeed : targetVelocityX;
        rb.linearVelocity = new Vector2(velocityX, rb.linearVelocity.y);
    }

    void BeginWindup(float horizontalDelta)
    {
        phase = ChargePhase.Windup;
        targetVelocityX = 0f;
        lockedDirection = Mathf.Sign(horizontalDelta);
        phaseEndTime = Time.time + windupSeconds;
        nextBlinkTime = Time.time;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer != null)
        {
            originalVisibility = spriteRenderer.enabled;
            spriteVisibilityCached = true;
        }
    }

    void BeginDash()
    {
        RestoreSprite();
        phase = ChargePhase.Dash;
        phaseEndTime = Time.time + dashDurationSeconds;
    }

    void BeginRecovery()
    {
        phase = ChargePhase.Recovery;
        targetVelocityX = 0f;
        cooldownEndTime = Time.time + cooldownSeconds;
        phaseEndTime = Time.time + RecoverySeconds;
    }

    void ResetCharge()
    {
        RestoreSprite();
        phase = ChargePhase.Approach;
        targetVelocityX = 0f;
        lockedDirection = 0f;
        phaseEndTime = 0f;
        cooldownEndTime = 0f;
    }

    void RestoreSprite()
    {
        if (spriteRenderer != null && spriteVisibilityCached) spriteRenderer.enabled = originalVisibility;
        spriteRenderer = null;
        spriteVisibilityCached = false;
    }
}
