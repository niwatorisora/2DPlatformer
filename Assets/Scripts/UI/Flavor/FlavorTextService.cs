using UnityEngine;

// フレーバーテキスト表示の唯一の窓口。ゲーム側はこのサービスだけを呼ぶ。
public class FlavorTextService : MonoBehaviour
{
    [SerializeField] ModalManager modalManager;
    [SerializeField] FlavorTextModal modal;
    [SerializeField] FlavorTextSet textSet;

    public bool Show(string key)
    {
        if (textSet != null && textSet.TryGet(key, out string title, out string text))
        {
            Show(title, text);
            return true;
        }

        // 呼び出し側の実装を止めないため false で返す。
        GameLog.Warning(this, $"Flavor text key '{key}' was not found.");
        return false;
    }

    // 将来の動的テキスト表示用。データセットを経由しない。
    public void Show(string title, string text)
    {
        if (modal == null || modalManager == null)
        {
            GameLog.Warning(this, "Flavor Text Service requires ModalManager and FlavorTextModal.");
            return;
        }

        modal.Show(title, text);
        modalManager.Push(modal);
    }
}
