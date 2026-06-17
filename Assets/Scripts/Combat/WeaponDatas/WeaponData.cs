using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Weapon";
    // 武器切替 HUD や将来のアイテム UI で使うスプライト。ゲームプレイ値ではないため任意。
    public Sprite weaponSprite;

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
