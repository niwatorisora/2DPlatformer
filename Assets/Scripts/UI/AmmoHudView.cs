using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤーの残弾を "{マガジン} / {所持}" 形式で、装備中の武器名・武器アイコンと共に表示する
/// スクリーン空間HUDウィジェット。Magazine にバインドし、そのイベントで更新する。
/// 依存は一方向: このビューがゲームロジックを参照する（逆は無い）。
/// </summary>
public class AmmoHudView : EventBoundView<Magazine>
{
    [SerializeField] Text ammoLabel;         // "30 / 120"
    [SerializeField] Text weaponNameLabel;   // 武器の displayName
    // WeaponData.weaponSprite を表示する Image。未アサインなら無視。
    [SerializeField] Image weaponIconImage;

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
        Refresh();
    }

    void Refresh()
    {
        if (Target == null) return;

        if (ammoLabel != null)
        {
            // 所持弾無限時は「∞」を表示。"∞" は ∞ (無限大記号)。
            string reserve = Target.InfiniteReserve ? "∞" : Target.ReserveAmmo.ToString();
            ammoLabel.text = $"{Target.CurrentAmmo} / {reserve}";
        }

        if (weaponNameLabel != null)
            weaponNameLabel.text = Target.Weapon != null ? Target.Weapon.displayName : "";

        if (weaponIconImage != null)
        {
            var sprite = Target.Weapon != null ? Target.Weapon.weaponSprite : null;
            weaponIconImage.sprite = sprite;
            // スプライト未設定時は Image コンポーネントを非表示にして透過を避ける。
            weaponIconImage.enabled = sprite != null;
        }
    }
}
