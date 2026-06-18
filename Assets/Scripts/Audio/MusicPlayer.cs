using System.Collections;
using UnityEngine;

/// <summary>
/// BGM 専用の 2 トラッククロスフェードプレイヤー。
/// AudioManager が生成・保持し、コルーチンの実行には AudioManager の MonoBehaviour を借りる。
/// </summary>
public class MusicPlayer
{
    readonly AudioSource[] tracks;  // [0] と [1] を交互に使う
    int currentTrack;

    MonoBehaviour host;

    public SoundData CurrentSound { get; private set; }

    public MusicPlayer(Transform container, MonoBehaviour coroutineHost)
    {
        host = coroutineHost;
        tracks = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject($"BgmTrack{i}");
            go.transform.SetParent(container);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop        = true;
            tracks[i] = src;
        }
    }

    /// <summary>
    /// BGM を再生する。既に同じ SoundData が流れていれば何もしない。
    /// </summary>
    public void Play(SoundData sound, float effectiveVolume, float crossfadeDuration = 0.5f)
    {
        if (sound == null || sound.clip == null)
        {
            Stop(crossfadeDuration);
            return;
        }

        if (CurrentSound == sound) return;

        CurrentSound = sound;
        int next = 1 - currentTrack;
        tracks[next].clip   = sound.clip;
        tracks[next].volume = 0f;
        tracks[next].pitch  = Random.Range(sound.pitchMin, sound.pitchMax);
        tracks[next].loop   = sound.loop;
        tracks[next].Play();

        float targetVolume = effectiveVolume;
        host.StartCoroutine(Crossfade(currentTrack, next, targetVolume, crossfadeDuration));
        currentTrack = next;
    }

    public void Stop(float fadeOutDuration = 0.5f)
    {
        CurrentSound = null;
        if (fadeOutDuration <= 0f)
        {
            tracks[currentTrack].Stop();
            return;
        }
        host.StartCoroutine(FadeOut(currentTrack, fadeOutDuration));
    }

    /// <summary>音量が変わったとき現在のトラックに即時反映する。</summary>
    public void UpdateVolume(float effectiveVolume)
    {
        tracks[currentTrack].volume = effectiveVolume;
    }

    public void Pause()  => tracks[currentTrack].Pause();
    public void Resume() => tracks[currentTrack].UnPause();

    IEnumerator Crossfade(int outTrack, int inTrack, float targetVolume, float duration)
    {
        float startOut = tracks[outTrack].volume;
        float elapsed  = 0f;

        if (duration <= 0f)
        {
            tracks[outTrack].Stop();
            tracks[inTrack].volume = targetVolume;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            tracks[outTrack].volume = Mathf.Lerp(startOut, 0f, t);
            tracks[inTrack].volume  = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        tracks[outTrack].Stop();
        tracks[outTrack].volume = 0f;
        tracks[inTrack].volume  = targetVolume;
    }

    IEnumerator FadeOut(int trackIndex, float duration)
    {
        float startVol = tracks[trackIndex].volume;
        float elapsed  = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            tracks[trackIndex].volume = Mathf.Lerp(startVol, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        tracks[trackIndex].Stop();
        tracks[trackIndex].volume = 0f;
    }
}
