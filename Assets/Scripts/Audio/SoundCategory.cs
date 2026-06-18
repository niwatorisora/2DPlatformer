/// <summary>
/// 音量チャンネルの区分。
/// Master は他の全チャンネルに乗算される親音量。
/// </summary>
public enum SoundCategory
{
    Master  = 0,
    Sfx     = 1,
    Bgm     = 2,
    Ambient = 3,
}
