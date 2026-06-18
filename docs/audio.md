# 音声システム

## 概要

ScriptableObject 駆動の音声 API。4 チャンネル音量制御・PlayerPrefs 永続化・スパム対策を持ち、  
既存の `ScoreManager` / `Magazine` と同じ思想で設計されています。Unity AudioMixer は使わず、  
`master × channel × sound.volume` の乗算で音量を計算します。

## ファイル構成

`Assets/Scripts/Audio/` に以下が含まれます。

| ファイル | 役割 |
|---------|------|
| `SoundCategory.cs` | `Master`, `Sfx`, `Bgm`, `Ambient` enum |
| `SoundData.cs` | 音声定義 SO（`Create → Audio → Sound Data`） |
| `IAudioService.cs` | 再生・停止・音量 API の interface |
| `AudioManager.cs` | シーン唯一の実装体。音量永続化・イベント発火 |
| `SfxPlayer.cs` | `ObjectPool<AudioSource>` によるワンショット再生 |
| `MusicPlayer.cs` | BGM 専用 2 トラック + クロスフェード |
| `AmbientPlayer.cs` | 環境音専用 2 トラック + クロスフェード |
| `SoundCooldownTracker.cs` | スパム対策（クールダウン・同時再生上限） |
| `AudioHelper.cs` | スタティック薄ラッパー（ゲームプレイ側が直接呼ぶ） |

`Assets/Audio/Sounds/` にサンプル用 SoundData アセットを配置します。

## SoundData（SO）フィールド

`Create → Audio → Sound Data` から作成します。

| フィールド | 説明 |
|-----------|------|
| `clip` | `AudioClip` |
| `category` | `Sfx` / `Bgm` / `Ambient` |
| `volume` | 個別音量倍率（0–1） |
| `pitchMin` / `pitchMax` | ランダムピッチ範囲。同値なら固定ピッチ |
| `spatialBlend` | 0=2D、1=3D（ワールド位置再生時） |
| `loop` | BGM・環境音用ループフラグ |
| `cooldown` | 同一サウンドの最短再生間隔（秒）。0 = 無制限 |
| `maxSimultaneous` | 同時再生上限。0 = 無制限 |

## AudioManager API

```csharp
// 音量（0–1）
float GetVolume(SoundCategory category);
void  SetVolume(SoundCategory category, float volume);
void  SetMuted(SoundCategory category, bool muted);
bool  IsMuted(SoundCategory category);
float GetEffectiveVolume(SoundCategory category); // master × channel、ミュート時 0

// 永続化
void LoadSettings();
void SaveSettings();
void ResetSettingsToDefault();

// 再生
bool TryPlay(SoundData sound, Vector3? worldPosition = null); // SFX 専用
void PlayBgm(SoundData sound, float crossfadeDuration = 0.5f);
void PlayAmbient(SoundData sound, float crossfadeDuration = 0.5f);
void StopBgm(float fadeOutDuration = 0.5f);
void StopAmbient(float fadeOutDuration = 0.5f);
void StopAllSfx();

// 全体制御
void PauseAll();
void ResumeAll();

// イベント（将来の設定 UI 向け）
event Action<SoundCategory, float> OnVolumeChanged;
event Action<SoundCategory, bool>  OnMuteChanged;
```

## ゲームプレイ側から鳴らす方法

`AudioHelper.TryPlay(soundData)` を使います。AudioManager がシーンにいなければ無音で返ります。

```csharp
// SFX を 2D で鳴らす
AudioHelper.TryPlay(weaponData.fireSound);

// ワールド位置指定（3D SFX）
AudioHelper.TryPlay(deathSound, transform.position);
```

BGM・環境音は `AudioHelper.Service` で `IAudioService` を取得して呼びます。

```csharp
IAudioService audio = AudioHelper.Service;
audio?.PlayBgm(bgmData);
```

## PlayerPrefs 永続化

Awake で `LoadSettings()`、`OnApplicationPause` / `OnDestroy` で `SaveSettings()` を自動呼び出します。

| PlayerPrefs キー | 内容 |
|----------------|------|
| `audio_vol_Master` | Master 音量（float 0–1） |
| `audio_vol_Sfx` | SFX 音量 |
| `audio_vol_Bgm` | BGM 音量 |
| `audio_vol_Ambient` | 環境音量 |
| `audio_mute_*` | 各チャンネルのミュート（int 0/1） |

## スパム対策

1. **per-sound cooldown**: `SoundData.cooldown` 秒以内の再再生を拒否（`SoundCooldownTracker`）
2. **polyphony limit**: `SoundData.maxSimultaneous` を超える同時再生を拒否
3. **SFX プール**: `ObjectPool<AudioSource>` で GC/Instantiate スパイクを抑制（`SfxPlayer`）
4. **ピッチランダム化**: `pitchMin` – `pitchMax` のランダムピッチで連打音の機械的な重なりを軽減

## ゲームプレイ配線（最小設定）

既存 SO に任意の `SoundData` 参照を追加します。未設定なら無音のまま後方互換を保ちます。

| SO | フィールド | 再生タイミング |
|----|-----------|--------------|
| `WeaponData` | `fireSound` | 射撃サルボ成功時（`PlayerShooter.Salvo()`） |
| `WeaponData` | `reloadSound` | リロード開始時（`Magazine.ReloadRoutine()`） |
| `EnemyData` | `deathSound` | 敵撃破時（`EnemyController.OnDied()`） |

## シーン配置

`MainScene1.unity` の `AudioManager` GameObject に `AudioManager` コンポーネントが配置済みです。  
クリップ未設定のまま実行しても動作します（無音で再生をスキップするだけです）。

## 今回やらないこと（意図的）

- 音量スライダー UI
- Unity AudioMixer アセット
- `DontDestroyOnLoad`（シーン遷移が来たら拡張）
- 弾着弾音・被ダメ音（次フェーズ：`BulletData` / `Health` への配線）
- FMOD / Wwise 等の外部ミドルウェア

## 検証観点

- 4 チャンネル音量が独立に効く（`effectiveVolume = master × channel × sound.volume`）
- 同一射撃音の連打でクールダウンが効き、音が潰れない
- `WeaponData` / `EnemyData` に Sound 未設定でも既存挙動が変わらない
- Play 停止後に PlayerPrefs から音量が復元される
- BGM・環境音の切替でクロスフェードが動く
