using UnityEngine;

/// <summary>
/// 1 つのサウンド定義を保持する ScriptableObject。
/// 「何を・どう鳴らすか」のデータ側を担い、AudioManager がランタイム状態を管理する。
///
/// スパム対策:
///   cooldown        … 同一 SoundData の最短再生間隔（秒）
///   maxSimultaneous … 同時に重ねられる最大再生数（SFX のみ有効）
/// </summary>
[CreateAssetMenu(fileName = "NewSound", menuName = "Audio/Sound Data")]
public class SoundData : ScriptableObject
{
    [Header("Clip")]
    public AudioClip clip;

    [Header("Category")]
    public SoundCategory category = SoundCategory.Sfx;

    [Header("Volume & Pitch")]
    [Range(0f, 1f)]
    public float volume = 1f;
    // ランダムピッチで連打時の機械的な重なりを軽減する。同じ値なら固定ピッチ。
    [Range(0.5f, 2f)]
    public float pitchMin = 1f;
    [Range(0.5f, 2f)]
    public float pitchMax = 1f;

    [Header("Spatial")]
    // 0 = 2D（位置無視）、1 = 3D（ワールド位置に従う）。SFX ではほぼ 0 で十分。
    [Range(0f, 1f)]
    public float spatialBlend = 0f;

    [Header("Playback")]
    // BGM・環境音でループさせる場合は true。SFX では通常 false。
    public bool loop = false;

    [Header("Spam Guard")]
    [Tooltip("同一サウンドの最短再生間隔（秒）。0 なら無制限。")]
    [Min(0f)]
    public float cooldown = 0f;
    [Tooltip("同時再生できる上限数。0 は無制限（SFX にのみ有効）。")]
    [Min(0)]
    public int maxSimultaneous = 0;

    void OnValidate()
    {
        volume   = Mathf.Clamp01(volume);
        pitchMin = Mathf.Clamp(pitchMin, 0.5f, 2f);
        pitchMax = Mathf.Clamp(Mathf.Max(pitchMin, pitchMax), 0.5f, 2f);
        spatialBlend     = Mathf.Clamp01(spatialBlend);
        cooldown         = Mathf.Max(0f, cooldown);
        maxSimultaneous  = Mathf.Max(0, maxSimultaneous);
    }
}
