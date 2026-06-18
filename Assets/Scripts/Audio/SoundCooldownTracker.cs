using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SFX のスパム再生を抑制するトラッカー。AudioManager が保持する。
///
/// 対策の二本柱:
///   1. per-sound cooldown  … 同一 SoundData の再生間隔を SoundData.cooldown 秒で制限
///   2. polyphony limit     … 同時再生数を SoundData.maxSimultaneous で制限
///
/// SoundData インスタンスをキーとして管理するため、同じクリップでも
/// 別 SoundData アセットを使えば独立して扱われる。
/// </summary>
public class SoundCooldownTracker
{
    // 各 SoundData の「次に再生できる時刻」
    readonly Dictionary<SoundData, float> nextAllowedTime = new();

    // 各 SoundData の「現在の同時再生数」
    readonly Dictionary<SoundData, int> activeCount = new();

    /// <summary>
    /// 再生が許可されるか判定する。許可なら内部カウンタを更新して true を返す。
    /// </summary>
    public bool TryAcquire(SoundData sound)
    {
        if (sound == null) return false;

        float now = Time.realtimeSinceStartup;

        // クールダウン判定
        if (sound.cooldown > 0f)
        {
            if (nextAllowedTime.TryGetValue(sound, out float allowed) && now < allowed)
                return false;
            nextAllowedTime[sound] = now + sound.cooldown;
        }

        // 同時再生数判定
        if (sound.maxSimultaneous > 0)
        {
            activeCount.TryGetValue(sound, out int current);
            if (current >= sound.maxSimultaneous) return false;
            activeCount[sound] = current + 1;
        }

        return true;
    }

    /// <summary>
    /// 再生が終了したことを通知する（polyphony カウンタを減らす）。
    /// </summary>
    public void Release(SoundData sound)
    {
        if (sound == null || sound.maxSimultaneous <= 0) return;

        if (activeCount.TryGetValue(sound, out int current))
            activeCount[sound] = Mathf.Max(0, current - 1);
    }

    /// <summary>全カウンタをリセットする（シーン遷移などで使用）。</summary>
    public void Clear()
    {
        nextAllowedTime.Clear();
        activeCount.Clear();
    }
}
