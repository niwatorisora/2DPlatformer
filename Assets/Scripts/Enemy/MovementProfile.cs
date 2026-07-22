using UnityEngine;

/// <summary>
/// 敵の移動挙動1種＝アセット1枚。数値調整はここ1箇所で全使用行に効く。
/// </summary>
[CreateAssetMenu(fileName = "NewMovementProfile", menuName = "Combat/Movement Profile")]
public class MovementProfile : ScriptableObject
{
    public enum MovementKind
    {
        Ground,
        JumpingGround,
        Flying,
        // 既存アセットはintで保存しているため必ず末尾に追加
        Charge
    }

    [Header("Kind")]
    [SerializeField] MovementKind kind;

    [Header("Shared")]
    [SerializeField] float moveSpeed = 3f;

    [Header("Jumping Ground")]
    [Tooltip("JumpingGround のみで使用するジャンプ間隔（秒）。")]
    [SerializeField] float jumpInterval = 1.5f;
    [Tooltip("JumpingGround のみで使用するジャンプ初速。")]
    [SerializeField] float jumpVelocity = 6f;

    [Header("Flying")]
    [Tooltip("待機・哨戒時に湧いた地点から浮上する高さ。Flying専用")]
    [SerializeField] float hoverHeight = 2.5f;
    [Tooltip("Flying のみで使用する旋回の追従係数。1 は即時、低いほど滑らか。")]
    [SerializeField, Range(0.01f, 1f)] float steeringSmoothing = 0.15f;

    [Header("Charge")]
    [Tooltip("この距離で突進を構える")]
    [SerializeField] float chargeTriggerRange = 4f;
    [Tooltip("溜め。点滅で予告")]
    [SerializeField] float chargeWindupSeconds = 0.4f;
    [Tooltip("突進中の水平速度")]
    [SerializeField] float chargeSpeed = 12f;
    [Tooltip("突進を維持する時間（秒）")]
    [SerializeField] float chargeDurationSeconds = 0.5f;
    [Tooltip("次の突進までの待機時間（秒）")]
    [SerializeField] float chargeCooldownSeconds = 1.5f;

    public MovementKind Kind => kind;
    public float MoveSpeed => moveSpeed;
    public float JumpInterval => jumpInterval;
    public float JumpVelocity => jumpVelocity;
    public float HoverHeight => hoverHeight;
    public float SteeringSmoothing => steeringSmoothing;
    public float ChargeTriggerRange => chargeTriggerRange;
    public float ChargeWindupSeconds => chargeWindupSeconds;
    public float ChargeSpeed => chargeSpeed;
    public float ChargeDurationSeconds => chargeDurationSeconds;
    public float ChargeCooldownSeconds => chargeCooldownSeconds;

    void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        jumpInterval = Mathf.Max(0f, jumpInterval);
        jumpVelocity = Mathf.Max(0f, jumpVelocity);
        hoverHeight = Mathf.Max(0f, hoverHeight);
        steeringSmoothing = Mathf.Clamp(steeringSmoothing, 0.01f, 1f);
        chargeTriggerRange = Mathf.Max(0f, chargeTriggerRange);
        chargeWindupSeconds = Mathf.Max(0f, chargeWindupSeconds);
        chargeSpeed = Mathf.Max(0f, chargeSpeed);
        chargeDurationSeconds = Mathf.Max(0f, chargeDurationSeconds);
        chargeCooldownSeconds = Mathf.Max(0f, chargeCooldownSeconds);
    }
}
