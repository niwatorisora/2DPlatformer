using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤーの残弾を "{マガジン} / {所持}" 形式で、装備中の武器名・武器アイコンと共に表示する
/// スクリーン空間HUDウィジェット。Magazine にバインドし、そのイベントで更新する。
/// 依存は一方向: このビューがゲームロジックを参照する（逆は無い）。
/// </summary>
public class AmmoHudView : EventBoundView<Magazine>
{
    [SerializeField] HudTheme theme;
    [SerializeField] Image panel;
    [SerializeField] Text ammoLabel;         // "30 / 120"
    [SerializeField] Text weaponNameLabel;   // 武器の displayName
    // WeaponData.hudIcon を表示する。未設定時はテーマ色の単色表示にする。
    [SerializeField] Image weaponIconImage;
    [SerializeField] HudPunch punch;

    bool hasValue;
    int previousAmmo;
    int previousReserve;

    void Awake() => ApplyTheme();

    public void ApplyTheme()
    {
        if (theme == null) return;
        theme.ApplyImage(panel, theme.PanelFrame(), theme.PanelDark());
        theme.ApplyText(ammoLabel, Target != null && Target.IsFull
            ? theme.Gold() : theme.BoneCream());
        theme.ApplyText(weaponNameLabel, theme.BoneCream());

    }

    protected override void Subscribe(Magazine magazine)
    {
        magazine.OnAmmoChanged += Refresh;
    }

    protected override void Unsubscribe(Magazine magazine)
    {
        magazine.OnAmmoChanged -= Refresh;
    }

    protected override void OnTargetBound(Magazine magazine)
    {
        ApplyTheme();
        ApplyWeaponIcon();
        Refresh();
    }

    void ApplyWeaponIcon()
    {
        if (weaponIconImage == null || theme == null) return;
        theme.ApplyImage(weaponIconImage, Target?.Weapon != null ? Target.Weapon.HudIcon : null,
            theme.Gold());
        weaponIconImage.preserveAspect = true;
        weaponIconImage.enabled = true;
    }

    void Refresh()
    {
        if (Target == null) return;

        bool changed = hasValue && (previousAmmo != Target.CurrentAmmo ||
            previousReserve != Target.ReserveAmmo);
        previousAmmo = Target.CurrentAmmo;
        previousReserve = Target.ReserveAmmo;
        hasValue = true;

        if (ammoLabel != null)
        {
            // 所持弾無限時は「∞」を表示。"∞" は ∞ (無限大記号)。
            string reserve = Target.InfiniteReserve ? "∞" : Target.ReserveAmmo.ToString();
            ammoLabel.text = $"{Target.CurrentAmmo} / {reserve}";
        }

        if (weaponNameLabel != null)
            weaponNameLabel.text = Target.Weapon != null ? Target.Weapon.displayName : "";

        // Configure may run after this view first binds. Re-apply the icon on each
        // ammo update so a late-equipped weapon replaces the fallback image.
        ApplyWeaponIcon();
        ApplyTheme();
        if (changed) punch?.Punch();
    }
}
