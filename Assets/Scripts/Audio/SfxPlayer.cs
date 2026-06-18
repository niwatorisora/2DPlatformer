using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// ObjectPool&lt;AudioSource&gt; を用いたワンショット SFX 再生器。
/// BulletPool と同じ設計で、再生終了後に AudioSource をプールへ自動返却する。
/// AudioManager が生成・保持する。
/// </summary>
public class SfxPlayer
{
    readonly Transform container;
    readonly ObjectPool<AudioSource> pool;
    readonly SoundCooldownTracker tracker;

    // 再生中 AudioSource → SoundData の対応（Release 時に tracker へ通知するため）
    readonly Dictionary<AudioSource, SoundData> activeSources = new();

    public SfxPlayer(Transform container, SoundCooldownTracker tracker,
                     int defaultCapacity = 16, int maxSize = 64)
    {
        this.container = container;
        this.tracker   = tracker;

        pool = new ObjectPool<AudioSource>(
            CreateSource,
            src => src.gameObject.SetActive(true),
            src => src.gameObject.SetActive(false),
            src => Object.Destroy(src.gameObject),
            true,
            defaultCapacity,
            maxSize);
    }

    /// <summary>
    /// SFX を再生する。スパムガードを通過した場合のみ AudioSource を確保する。
    /// effectiveVolume = master × sfx × sound.volume の乗算済み値を渡すこと。
    /// </summary>
    public bool Play(SoundData sound, float effectiveVolume, Vector3? worldPosition = null)
    {
        if (sound == null || sound.clip == null) return false;
        if (!tracker.TryAcquire(sound)) return false;

        AudioSource src = pool.Get();
        activeSources[src] = sound;

        src.clip         = sound.clip;
        src.volume       = effectiveVolume;
        src.pitch        = Random.Range(sound.pitchMin, sound.pitchMax);
        src.loop         = false;
        src.spatialBlend = sound.spatialBlend;
        src.transform.position = worldPosition ?? Vector3.zero;
        src.Play();

        // 再生終了後に自動返却するコルーチンをメインスレッドで動かす必要があるため
        // AudioManager（MonoBehaviour）側の StartCoroutine を経由して返却をトリガーする。
        // ここでは長さを返して AudioManager が判断できるようにする。
        // → Release はコルーチン不要。AudioManager.Update でポーリングして返却する。
        return true;
    }

    /// <summary>
    /// 毎フレーム呼び出して再生終了済みの AudioSource をプールへ返す。
    /// </summary>
    public void Tick()
    {
        // activeSources を走査して再生完了したものを返却する。
        // Dictionary をそのままイテレートしながら削除できないため一時リストを使う。
        List<AudioSource> toRelease = null;

        foreach (var (src, sound) in activeSources)
        {
            if (src.isPlaying) continue;
            toRelease ??= new List<AudioSource>();
            toRelease.Add(src);
        }

        if (toRelease == null) return;

        foreach (var src in toRelease)
        {
            if (activeSources.TryGetValue(src, out SoundData sound))
            {
                tracker.Release(sound);
                activeSources.Remove(src);
            }
            pool.Release(src);
        }
    }

    /// <summary>再生中の AudioSource を全て強制停止してプールへ返す。</summary>
    public void StopAll()
    {
        var toRelease = new List<AudioSource>(activeSources.Keys);
        foreach (var src in toRelease)
        {
            src.Stop();
            if (activeSources.TryGetValue(src, out SoundData sound))
            {
                tracker.Release(sound);
                activeSources.Remove(src);
            }
            pool.Release(src);
        }
    }

    public void PauseAll()
    {
        foreach (var src in activeSources.Keys) src.Pause();
    }

    public void ResumeAll()
    {
        foreach (var src in activeSources.Keys) src.UnPause();
    }

    AudioSource CreateSource()
    {
        var go = new GameObject("SfxSource");
        go.transform.SetParent(container);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        go.SetActive(false);
        return src;
    }
}
