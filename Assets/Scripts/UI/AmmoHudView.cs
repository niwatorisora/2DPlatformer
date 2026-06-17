using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤーの残弾を "{マガジン} / {所持}" 形式で、装備中の武器名・武器アイコンと共に表示する
/// スクリーン空間HUDウィジェット。Magazine にバインドし、そのイベントで更新する。
/// 依存は一方向: このビューがゲームロジックを参照する（逆は無い）。
/// </summary>
public class AmmoHudView : MonoBehaviour
{
    [SerializeField] Magazine magazine;      // 未設定ならシーンから自動取得
    [SerializeField] Text ammoLabel;         // "30 / 120"
    [SerializeField] Text weaponNameLabel;   // 武器の displayName
    // WeaponData.weaponSprite を表示する Image。未アサインなら無視。
    [SerializeField] Image weaponIconImage;

    bool subscribed;

    void OnEnable() => TrySubscribe();
    void Start() => TrySubscribe();

    void LateUpdate()
    {
        if (!subscribed) TrySubscribe();
    }

    void OnDisable()
    {
        if (!subscribed || magazine == null) return;
        magazine.OnAmmoChanged -= Refresh;
        subscribed = false;
    }

    void TrySubscribe()
    {
        if (subscribed) return;
        if (magazine == null) magazine = FindFirstObjectByType<Magazine>();
        if (magazine == null) return;

        magazine.OnAmmoChanged += Refresh;
        subscribed = true;
        Refresh();
    }

    void Refresh()
    {
        if (magazine == null) return;

        if (ammoLabel != null)
        {
            // 所持弾無限時は「∞」を表示。"∞" は ∞ (無限大記号)。
            string reserve = magazine.InfiniteReserve ? "∞" : magazine.ReserveAmmo.ToString();
            ammoLabel.text = $"{magazine.CurrentAmmo} / {reserve}";
        }

        if (weaponNameLabel != null)
            weaponNameLabel.text = magazine.Weapon != null ? magazine.Weapon.displayName : "";

        if (weaponIconImage != null)
        {
            var sprite = magazine.Weapon != null ? magazine.Weapon.weaponSprite : null;
            weaponIconImage.sprite = sprite;
            // スプライト未設定時は Image コンポーネントを非表示にして透過を避ける。
            weaponIconImage.enabled = sprite != null;
        }
    }
}