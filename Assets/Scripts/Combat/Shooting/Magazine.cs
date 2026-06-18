using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 単一シューター（プレイヤーまたは敵）のランタイム弾数状態。
/// マガジン内の残弾と予備弾を保持する。容量やリロード時間などの設定は
/// WeaponData から Configure() 経由で受け取る（状態はSOに持たせない）。
///
/// リロードはプール式: マガジンを容量まで補充し、補充に必要な分だけ
/// 予備弾から引く。既にマガジンに入っていた弾は決して無駄にしない。
/// </summary>
public class Magazine : MonoBehaviour
{
    // HUD など外部はこれらのイベントを購読して表示を更新する（今回は未使用）。
    public event Action OnAmmoChanged;
    public event Action OnReloadStarted;
    public event Action OnReloadCompleted;

    // --- 設定（Configure で WeaponData から取り込む） ---
    int magazineSize;
    float reloadTime;
    bool infiniteReserve;
    bool autoReloadWhenEmpty;
    WeaponData sourceWeapon;

    // --- ランタイム状態 ---
    int currentAmmo;
    int reserveAmmo;
    bool isReloading;
    Coroutine reloadRoutine;

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public int MagazineSize => magazineSize;
    public bool InfiniteReserve => infiniteReserve;
    public bool IsReloading => isReloading;
    public bool IsFull => currentAmmo >= magazineSize;
    public bool HasReserve => infiniteReserve || reserveAmmo > 0;
    public bool CanFire => !isReloading && currentAmmo > 0;
    public WeaponData Weapon => sourceWeapon;

    /// <summary>容量・予備弾を WeaponData から初期化し、マガジンを満タンにする。</summary>
    public void Configure(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            GameLog.Error(this, "Configure requires weapon data.");
            return;
        }

        magazineSize = Mathf.Max(1, weaponData.magazineSize);
        reloadTime = Mathf.Max(0f, weaponData.reloadTime);
        infiniteReserve = weaponData.infiniteReserve;
        autoReloadWhenEmpty = weaponData.autoReloadWhenEmpty;
        sourceWeapon = weaponData;

        CancelReload();
        currentAmmo = magazineSize;
        reserveAmmo = Mathf.Max(0, weaponData.startingReserveAmmo);
        OnAmmoChanged?.Invoke();
    }

    /// <summary>count発を消費しようとする。リロード中/弾不足なら false。</summary>
    public bool TryConsume(int count = 1)
    {
        if (isReloading || count <= 0 || currentAmmo < count) return false;

        currentAmmo -= count;
        OnAmmoChanged?.Invoke();

        if (currentAmmo == 0 && autoReloadWhenEmpty) StartReload();
        return true;
    }

    /// <summary>有用かつ可能ならリロードを開始する。多重呼び出しは無視。</summary>
    public void StartReload()
    {
        if (isReloading || IsFull || !HasReserve) return;
        if (!isActiveAndEnabled) return;
        reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        isReloading = true;
        OnReloadStarted?.Invoke();
        AudioHelper.TryPlay(sourceWeapon != null ? sourceWeapon.reloadSound : null);

        if (reloadTime > 0f) yield return new WaitForSeconds(reloadTime);

        int needed = magazineSize - currentAmmo;
        int moved = infiniteReserve ? needed : Mathf.Min(needed, reserveAmmo);
        currentAmmo += moved;
        if (!infiniteReserve) reserveAmmo -= moved;

        isReloading = false;
        reloadRoutine = null;
        OnReloadCompleted?.Invoke();
        OnAmmoChanged?.Invoke();
    }

    void CancelReload()
    {
        if (reloadRoutine != null) StopCoroutine(reloadRoutine);
        reloadRoutine = null;
        isReloading = false;
    }

    void OnDisable() => CancelReload();
}