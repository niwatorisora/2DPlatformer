using UnityEngine;
using UnityEngine.Serialization;

/// <summary>敵の攻撃手段1種＝アセット1枚。</summary>
[CreateAssetMenu(fileName = "NewAttackProfile", menuName = "Combat/Attack Profile")]
public class AttackProfile : ScriptableObject
{
    public enum AttackKind { Shooter, Contact }

    [Header("Kind")]
    [SerializeField] AttackKind kind;
    [Header("Shooter")]
    [Tooltip("弾、連射間隔、連射方式は WeaponData 側で設定する。")]
    [SerializeField] WeaponData weaponData;
    [SerializeField] float spawnDistanceFromOrigin = 0.4f;

    [Header("Engagement")]
    [Tooltip("この距離まで近づいたら攻撃行動に入る。接触型は密着距離を指定")]
    [FormerlySerializedAs("attackRange")]
    [SerializeField] float engageRange = 4f;
    [SerializeField, HideInInspector] AttackKind engageRangeKind = AttackKind.Shooter;

    [Header("Contact")]
    [SerializeField] float damage = 1f;
    [SerializeField] float hitCooldownSeconds = 0.8f;
    [SerializeField] float knockbackForce = 5f;
    [Tooltip("ノックバックの上向き成分の比率")]
    [SerializeField] float knockbackUpwardRatio = 0.7f;
    [Tooltip("被弾時の操作不能時間。無敵時間より短くすること")]
    [SerializeField] float hitstunSeconds = 0.3f;
    [Tooltip("攻撃者の速度をどれだけ上乗せするか。突進の勢いが乗る")]
    [SerializeField] float attackerVelocityInheritance = 0.5f;

    public AttackKind Kind => kind;
    public WeaponData WeaponData => weaponData;
    public float EngageRange => engageRange;
    public float SpawnDistanceFromOrigin => spawnDistanceFromOrigin;
    public float Damage => damage;
    public float HitCooldownSeconds => hitCooldownSeconds;
    public float KnockbackForce => knockbackForce;
    public float KnockbackUpwardRatio => knockbackUpwardRatio;
    public float HitstunSeconds => hitstunSeconds;
    public float AttackerVelocityInheritance => attackerVelocityInheritance;

    void OnValidate()
    {
        // 種別を切り替えた直後だけ、未調整の既定値を種別ごとの既定値へ合わせる。
        if (kind != engageRangeKind && Mathf.Approximately(engageRange, GetDefaultEngageRange(engageRangeKind)))
            engageRange = GetDefaultEngageRange(kind);
        engageRangeKind = kind;
        engageRange = Mathf.Max(0f, engageRange);
        spawnDistanceFromOrigin = Mathf.Max(0f, spawnDistanceFromOrigin);
        damage = Mathf.Max(0f, damage);
        hitCooldownSeconds = Mathf.Max(0f, hitCooldownSeconds);
        knockbackForce = Mathf.Max(0f, knockbackForce);
        knockbackUpwardRatio = Mathf.Max(0f, knockbackUpwardRatio);
        hitstunSeconds = Mathf.Max(0f, hitstunSeconds);
        attackerVelocityInheritance = Mathf.Max(0f, attackerVelocityInheritance);
    }

    static float GetDefaultEngageRange(AttackKind attackKind) => attackKind == AttackKind.Contact ? 0.6f : 4f;
}
