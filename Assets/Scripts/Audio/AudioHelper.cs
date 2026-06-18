using UnityEngine;

/// <summary>
/// ゲームプレイコードから AudioManager を直接参照させないための薄いスタティックヘルパー。
/// AudioManager が未配置でも null-safe に動作し、既存ゲームプレイを壊さない。
/// </summary>
public static class AudioHelper
{
    static IAudioService _service;

    /// <summary>
    /// シーン内の IAudioService を返す。キャッシュが無効なら再検索する。
    /// AudioManager が存在しない場合は null を返す（再生をスキップ）。
    /// </summary>
    public static IAudioService Service
    {
        get
        {
            // AudioManager が破棄されていれば再検索する。
            if (_service is Object obj && obj == null) _service = null;
            if (_service == null) _service = Object.FindFirstObjectByType<AudioManager>();
            return _service;
        }
    }

    /// <summary>
    /// SFX を再生する。sound が null または AudioManager 未配置の場合は無音で返る。
    /// </summary>
    public static bool TryPlay(SoundData sound, Vector3? worldPosition = null)
    {
        if (sound == null) return false;
        return Service?.TryPlay(sound, worldPosition) ?? false;
    }
}
