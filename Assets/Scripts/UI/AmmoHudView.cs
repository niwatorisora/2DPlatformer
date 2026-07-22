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
    // HudTheme.weaponIcon を表示する。スプライト未設定時は単色表示にする。
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

        if (weaponIconImage == null) return;
        theme.ApplyImage(weaponIconImage, theme.WeaponIcon(), theme.Gold());
        weaponIconImage.preserveAspect = true;
        weaponIconImage.enabled = true;
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
        Refresh();
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

        ApplyTheme();
        if (changed) punch?.Punch();
    }
}
