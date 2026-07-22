using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "UI/Hud Theme")]
public class HudTheme : ScriptableObject
{
    [SerializeField, Tooltip("HUDの基本文字色です。")]
    Color boneCream = new Color32(0xE8, 0xD5, 0xA8, 0xFF);
    [SerializeField, Tooltip("フレーバー本文の文字色")]
    Color flavorBodyColor = new Color32(0xF2, 0xEF, 0xE6, 0xFF);
    [SerializeField, Tooltip("文字縁取りと濃色装飾に使う色です。")]
    Color darkBrown = new Color32(0x4A, 0x2E, 0x14, 0xFF);
    [SerializeField, Tooltip("強調表示とHP満タン区画に使う色です。")]
    Color gold = new Color32(0xF0, 0xC0, 0x30, 0xFF);
    [SerializeField, Tooltip("パネルとHP空区画に使う色です。")]
    Color panelDark = new Color32(0x1A, 0x14, 0x20, 0xFF);
    [SerializeField, Tooltip("HUD全体で使う任意のフォントです。未設定時は既存フォントを維持します。")]
    Font font;
    [SerializeField, Tooltip("フレーバーテキスト専用フォント。未設定ならHUDフォントにフォールバック")]
    Font flavorFont;
    [SerializeField, Tooltip("HP満タン区画に使う任意のスプライトです。")]
    Sprite hpSegmentFull;
    [SerializeField, Tooltip("HP空区画に使う任意のスプライトです。")]
    Sprite hpSegmentEmpty;
    [SerializeField, Tooltip("HUDパネル枠に使う任意のスプライトです。")]
    Sprite panelFrame;

    public Color BoneCream() => boneCream;
    public Color FlavorBodyColor() => flavorBodyColor;
    public Color DarkBrown() => darkBrown;
    public Color Gold() => gold;
    public Color PanelDark() => panelDark;
    public Sprite HpSegment(bool full) => full ? hpSegmentFull : hpSegmentEmpty;
    public Sprite PanelFrame() => panelFrame;

    public void ApplyText(Text label, Color color)
    {
        if (label == null) return;
        if (font != null) label.font = font;
        label.color = color;
    }

    public void ApplyFlavorText(Text label, Color color)
    {
        if (label == null) return;
        if (flavorFont != null) label.font = flavorFont;
        else if (font != null) label.font = font;
        label.color = color;
    }

    public void ApplyImage(Image image, Sprite sprite, Color fallback)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = sprite != null ? Color.white : fallback;
    }

    public void ApplyOutline(Graphic graphic)
    {
        if (graphic == null) return;
        Outline outline = graphic.GetComponent<Outline>();
        if (outline == null) outline = graphic.gameObject.AddComponent<Outline>();
        outline.effectColor = darkBrown;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
    }
}
