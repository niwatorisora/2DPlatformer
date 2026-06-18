using System;
using UnityEngine;

/// <summary>
/// シーンに 1 つ配置する音声管理コンポーネント。IAudioService を実装し、
/// SfxPlayer / MusicPlayer / AmbientPlayer / SoundCooldownTracker を束ねる。
///
/// 音量モデル:
///   effectiveVolume(category) = GetVolume(Master) × GetVolume(category) × SoundData.volume
///   ミュート時は effectiveVolume が 0。
///
/// PlayerPrefs キー:
///   audio_vol_master / audio_vol_sfx / audio_vol_bgm / audio_vol_ambient
///   audio_mute_master / audio_mute_sfx / audio_mute_bgm / audio_mute_ambient
/// </summary>
public class AudioManager : MonoBehaviour, IAudioService
{
    // ---- PlayerPrefs キープレフィックス ----
    const string PrefKeyVolPrefix  = "audio_vol_";
    const string PrefKeyMutePrefix = "audio_mute_";

    // ---- デフォルト音量 ----
    const float DefaultVolume = 1f;

    // ---- 内部状態 ----
    readonly float[] volumes = new float[4];
    readonly bool[]  mutes   = new bool[4];

    SfxPlayer          sfxPlayer;
    MusicPlayer        musicPlayer;
    AmbientPlayer      ambientPlayer;
    SoundCooldownTracker tracker;

    // ---- IAudioService イベント ----
    public event Action<SoundCategory, float> OnVolumeChanged;
    public event Action<SoundCategory, bool>  OnMuteChanged;

    // ---- Inspector ----
    [Header("SFX Pool")]
    [SerializeField] int sfxPoolDefaultCapacity = 16;
    [SerializeField] int sfxPoolMaxSize         = 64;

    // ---- Unity lifecycle ----

    void Awake()
    {
        tracker       = new SoundCooldownTracker();
        sfxPlayer     = new SfxPlayer(transform, tracker, sfxPoolDefaultCapacity, sfxPoolMaxSize);
        musicPlayer   = new MusicPlayer(transform, this);
        ambientPlayer = new AmbientPlayer(transform, this);

        LoadSettings();
    }

    void Update()
    {
        // 再生終了した SFX をプールへ返す（GC フリーでポーリング）。
        sfxPlayer.Tick();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused) SaveSettings();
    }

    void OnDestroy()
    {
        SaveSettings();
    }

    // ---- IAudioService: 音量 ----

    public float GetVolume(SoundCategory category)
        => volumes[(int)category];

    public void SetVolume(SoundCategory category, float volume)
    {
        volumes[(int)category] = Mathf.Clamp01(volume);
        OnVolumeChanged?.Invoke(category, volumes[(int)category]);
        ApplyVolumeToLiveTracks(category);
        SaveSettings();
    }

    public void SetMuted(SoundCategory category, bool muted)
    {
        mutes[(int)category] = muted;
        OnMuteChanged?.Invoke(category, muted);
        ApplyVolumeToLiveTracks(category);
        SaveSettings();
    }

    public bool IsMuted(SoundCategory category)
        => mutes[(int)category];

    public float GetEffectiveVolume(SoundCategory category)
    {
        if (mutes[(int)SoundCategory.Master] || mutes[(int)category]) return 0f;
        return volumes[(int)SoundCategory.Master] * volumes[(int)category];
    }

    // ---- IAudioService: 永続化 ----

    public void LoadSettings()
    {
        foreach (SoundCategory cat in Enum.GetValues(typeof(SoundCategory)))
        {
            int i = (int)cat;
            volumes[i] = PlayerPrefs.GetFloat(PrefKeyVolPrefix  + cat.ToString(), DefaultVolume);
            mutes[i]   = PlayerPrefs.GetInt  (PrefKeyMutePrefix + cat.ToString(), 0) != 0;
        }
    }

    public void SaveSettings()
    {
        foreach (SoundCategory cat in Enum.GetValues(typeof(SoundCategory)))
        {
            int i = (int)cat;
            PlayerPrefs.SetFloat(PrefKeyVolPrefix  + cat.ToString(), volumes[i]);
            PlayerPrefs.SetInt  (PrefKeyMutePrefix + cat.ToString(), mutes[i] ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    public void ResetSettingsToDefault()
    {
        foreach (SoundCategory cat in Enum.GetValues(typeof(SoundCategory)))
        {
            int i = (int)cat;
            volumes[i] = DefaultVolume;
            mutes[i]   = false;
            OnVolumeChanged?.Invoke(cat, volumes[i]);
            OnMuteChanged?.Invoke(cat, mutes[i]);
        }
        ApplyVolumeToLiveTracks(SoundCategory.Master);
        ApplyVolumeToLiveTracks(SoundCategory.Bgm);
        ApplyVolumeToLiveTracks(SoundCategory.Ambient);
        SaveSettings();
    }

    // ---- IAudioService: 再生 ----

    public bool TryPlay(SoundData sound, Vector3? worldPosition = null)
    {
        if (sound == null)
        {
            GameLog.Warning(this, "TryPlay called with null SoundData.");
            return false;
        }

        if (sound.clip == null)
        {
            GameLog.Warning(this, $"SoundData '{sound.name}' has no AudioClip assigned.");
            return false;
        }

        if (sound.category != SoundCategory.Sfx)
        {
            GameLog.Warning(this, $"TryPlay is for Sfx; use PlayBgm/PlayAmbient for '{sound.name}'.");
            return false;
        }

        float effectiveVol = GetEffectiveVolume(SoundCategory.Sfx) * sound.volume;
        return sfxPlayer.Play(sound, effectiveVol, worldPosition);
    }

    public void PlayBgm(SoundData sound, float crossfadeDuration = 0.5f)
    {
        float vol = GetEffectiveVolume(SoundCategory.Bgm) * (sound != null ? sound.volume : 1f);
        musicPlayer.Play(sound, vol, crossfadeDuration);
    }

    public void PlayAmbient(SoundData sound, float crossfadeDuration = 0.5f)
    {
        float vol = GetEffectiveVolume(SoundCategory.Ambient) * (sound != null ? sound.volume : 1f);
        ambientPlayer.Play(sound, vol, crossfadeDuration);
    }

    public void StopBgm(float fadeOutDuration = 0.5f)
        => musicPlayer.Stop(fadeOutDuration);

    public void StopAmbient(float fadeOutDuration = 0.5f)
        => ambientPlayer.Stop(fadeOutDuration);

    public void StopAllSfx()
        => sfxPlayer.StopAll();

    // ---- IAudioService: 全体制御 ----

    public void PauseAll()
    {
        sfxPlayer.PauseAll();
        musicPlayer.Pause();
        ambientPlayer.Pause();
    }

    public void ResumeAll()
    {
        sfxPlayer.ResumeAll();
        musicPlayer.Resume();
        ambientPlayer.Resume();
    }

    // ---- 内部ヘルパー ----

    /// <summary>
    /// カテゴリの音量またはミュートが変わったとき、
    /// 再生中の BGM・環境音トラックに即時反映する。
    /// </summary>
    void ApplyVolumeToLiveTracks(SoundCategory changed)
    {
        // Master が変わったら BGM と環境音の両方を更新する。
        if (changed == SoundCategory.Master || changed == SoundCategory.Bgm)
            musicPlayer.UpdateVolume(GetEffectiveVolume(SoundCategory.Bgm)
                                     * (musicPlayer.CurrentSound != null ? musicPlayer.CurrentSound.volume : 1f));

        if (changed == SoundCategory.Master || changed == SoundCategory.Ambient)
            ambientPlayer.UpdateVolume(GetEffectiveVolume(SoundCategory.Ambient)
                                       * (ambientPlayer.CurrentSound != null ? ambientPlayer.CurrentSound.volume : 1f));
    }

    void OnValidate()
    {
        sfxPoolDefaultCapacity = Mathf.Max(1, sfxPoolDefaultCapacity);
        sfxPoolMaxSize = Mathf.Max(sfxPoolDefaultCapacity, sfxPoolMaxSize);
    }
}
