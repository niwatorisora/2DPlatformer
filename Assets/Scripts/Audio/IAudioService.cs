using System;
using UnityEngine;

/// <summary>
/// 音声システムの公開 API。AudioManager がこれを実装する。
/// 呼び出し元は AudioManager 型を直接参照せず、このインターフェースを使うことで
/// 将来のモック差し替えや DI に対応できる。
/// </summary>
public interface IAudioService
{
    // ---- 音量 ----

    /// <summary>0–1 の音量を返す。</summary>
    float GetVolume(SoundCategory category);

    /// <summary>0–1 の音量を設定し PlayerPrefs に保存する。</summary>
    void SetVolume(SoundCategory category, float volume);

    /// <summary>ミュートを切り替える。</summary>
    void SetMuted(SoundCategory category, bool muted);

    /// <summary>ミュート中か。</summary>
    bool IsMuted(SoundCategory category);

    /// <summary>ミュートとマスター乗算を含む実効音量（0–1）。</summary>
    float GetEffectiveVolume(SoundCategory category);

    // ---- 永続化 ----

    void LoadSettings();
    void SaveSettings();
    void ResetSettingsToDefault();

    // ---- 再生 ----

    /// <summary>
    /// SFX を再生する。クールダウン・clip 未設定・上限超過時は false を返す。
    /// worldPosition を渡すと AudioSource.spatialBlend に応じて 3D 配置される。
    /// </summary>
    bool TryPlay(SoundData sound, Vector3? worldPosition = null);

    /// <summary>BGM を再生する。crossfadeDuration 秒でクロスフェード。</summary>
    void PlayBgm(SoundData sound, float crossfadeDuration = 0.5f);

    /// <summary>環境音を再生する。crossfadeDuration 秒でクロスフェード。</summary>
    void PlayAmbient(SoundData sound, float crossfadeDuration = 0.5f);

    void StopBgm(float fadeOutDuration = 0.5f);
    void StopAmbient(float fadeOutDuration = 0.5f);

    /// <summary>再生中の SFX を全て停止してプールに返す。</summary>
    void StopAllSfx();

    // ---- 全体制御 ----

    void PauseAll();
    void ResumeAll();

    // ---- イベント（将来の設定 UI 向け）----

    event Action<SoundCategory, float> OnVolumeChanged;
    event Action<SoundCategory, bool>  OnMuteChanged;
}
