using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Weapon";
    [FormerlySerializedAs("weaponSprite")]
    [SerializeField, Tooltip("HUD表示用アイコン")]
    Sprite hudIcon;
    [SerializeField, Tooltip("プレイヤーに持たせる表示用・未使用なら空でよい")]
    Sprite heldSprite;

    public Sprite HudIcon => hudIcon;
    public Sprite HeldSprite => heldSprite;

    [Header("Bullet")]
    public BulletData bulletData;

    [Header("Fire Mode")]
    public bool autoFire = false;

    [Header("Fire")]
    public float cooldown = 0.2f;
    // Simultaneous shots and sequence shots cover single, shotgun, and burst patterns.
    public int simultaneousShotCount = 1;
    public float spreadAngle = 30f;
    public int sequenceShotCount = 1;
    public float sequenceInterval = 0.08f;

    [Header("Ammo / Magazine")]
    public int magazineSize = 30;          // マガジン1個の容量
    public float reloadTime = 1.5f;        // リロード所要秒数
    public int startingReserveAmmo = 120;  // 開始時の手持ち予備弾
    public bool infiniteReserve = false;   // trueなら予備弾無限（マガジン容量だけは有限）
    public bool autoReloadWhenEmpty = true;// マガジンが空になったら自動でリロード開始

    [Header("Audio")]
    // 射撃サルボ成功時に再生。未設定なら無音（後方互換）。
    public SoundData fireSound;
    // リロード開始時に再生。未設定なら無音。
    public SoundData reloadSound;

    void OnValidate()
    {
        cooldown = Mathf.Max(0f, cooldown);
        simultaneousShotCount = Mathf.Max(1, simultaneousShotCount);
        spreadAngle = Mathf.Max(0f, spreadAngle);
        sequenceShotCount = Mathf.Max(1, sequenceShotCount);
        sequenceInterval = Mathf.Max(0f, sequenceInterval);

        magazineSize = Mathf.Max(1, magazineSize);
        reloadTime = Mathf.Max(0f, reloadTime);
        startingReserveAmmo = Mathf.Max(0, startingReserveAmmo);
    }
}
