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
        Flying
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
    [Tooltip("Flying のみで使用する旋回の追従係数。1 は即時、低いほど滑らか。")]
    [SerializeField, Range(0.01f, 1f)] float steeringSmoothing = 0.15f;

    public MovementKind Kind => kind;
    public float MoveSpeed => moveSpeed;
    public float JumpInterval => jumpInterval;
    public float JumpVelocity => jumpVelocity;
    public float SteeringSmoothing => steeringSmoothing;

    void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        jumpInterval = Mathf.Max(0f, jumpInterval);
        jumpVelocity = Mathf.Max(0f, jumpVelocity);
        steeringSmoothing = Mathf.Clamp(steeringSmoothing, 0.01f, 1f);
    }
}
