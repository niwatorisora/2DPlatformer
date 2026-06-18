# アーキテクチャ概要

プロジェクト全体の構成と、主要システム間のデータフローをまとめます。

## ディレクトリ構成

```
Assets/
├── Prefab/                    # 敵・弾・UI Prefab
├── Scenes/
│   ├── SampleScene.unity      # 最小プロトタイプ
│   └── MainScene1.unity       # ゲームプレイ検証（マガジン HUD 等）
└── Scripts/
    ├── Character/
    │   └── CharacterVisualController.cs # 見た目回転固定 + 水平フリップ（VisualRoot を制御）
    ├── Player/
    │   ├── PlayerMovement.cs  # 左右移動・ジャンプ
    │   └── PlayerShooter.cs   # マウス照準射撃
    ├── Combat/
    │   ├── Health.cs          # IDamageable, DamageContext, Health を同居
    │   ├── TeamAffiliation.cs # TeamId enum + 味方判定
    │   ├── BulletDatas/
    │   │   └── BulletData.cs  # BulletData SO + BulletConfig struct（+ ShotgunBullet.asset 等）
    │   ├── WeaponDatas/
    │   │   └── WeaponData.cs  # WeaponData SO（発射パラメータ + マガジン設定 + displayName + weaponSprite）
    │   └── Shooting/
    │       ├── BulletPool.cs  # IBulletPool interface + BulletPool
    │       ├── Bullet.cs      # 弾の移動・衝突・ダメージ・プール返却
    │       ├── ShooterCore.cs # spread/sequence の共有発射ロジック（internal static）
    │       └── Magazine.cs    # ランタイム弾数状態（マガジン残弾/予備弾・リロード）
    ├── Enemy/
    │   ├── EnemyDatas/
    │   │   └── EnemyData.cs       # 敵種パラメータ ScriptableObject（HP/速度/距離/巡回/スコア）
    │   ├── EnemyController.cs     # 敵 AI ハブ、State Machine を駆動、Health.OnDied を購読
    │   ├── EnemySensor.cs         # 距離判定（検知・攻撃射程・ロスト）
    │   └── EnemyFactory.cs        # Prefab Instantiate + Initialize
    ├── Enemy/Movement/
    │   ├── EnemyMovement.cs         # abstract（Configure / MoveToward / Stop）
    │   ├── EnemyGroundMovement.cs   # 地上横移動（Rigidbody2D.linearVelocity.x）
    │   ├── EnemyJumpingGroundMovement.cs  # 地上移動 + 自動ジャンプ
    │   └── EnemyFlyingMovement.cs   # 全方向 2D 空中移動（gravityScale=0）
    ├── Enemy/Attack/
    │   ├── EnemyAttack.cs         # abstract（Configure / CanAttack / TryAttack）
    │   └── EnemyShooterAttack.cs  # ShooterCore でターゲット方向射撃
    ├── Enemy/States/
    │   └── EnemyStateMachine.cs   # EnemyStateMachine + 各 EnemyState 実装
    ├── Enemy/Spawn/
    │   ├── EnemySpawnPoint.cs         # 出現地点（groupId, Gizmo）
    │   ├── EnemySpawnPattern.cs       # ScriptableObject（エントリ列）+ Entry/Candidate
    │   ├── EnemySpawnPatternSet.cs    # ScriptableObject（パターン候補）
    │   └── EnemySpawnPatternRunner.cs # パターン実行器
    ├── Audio/
    │   ├── SoundCategory.cs       # Master / Sfx / Bgm / Ambient enum
    │   ├── SoundData.cs           # 音声定義 SO（clip / category / volume / pitch / cooldown 等）
    │   ├── IAudioService.cs       # 再生・停止・音量 API の interface
    │   ├── AudioManager.cs        # IAudioService 実装体（ObjectPool / PlayerPrefs / イベント）
    │   ├── SfxPlayer.cs           # ObjectPool<AudioSource> によるワンショット再生
    │   ├── MusicPlayer.cs         # BGM 2 トラッククロスフェード
    │   ├── AmbientPlayer.cs       # 環境音 2 トラッククロスフェード
    │   ├── SoundCooldownTracker.cs# cooldown / polyphony でスパム抑制
    │   └── AudioHelper.cs         # static 薄ラッパー（FindFirstObjectByType キャッシュ）
    ├── Diagnostics/
    │   ├── GameLog.cs             # [Level:ClassName] 形式のコンソールログ
    │   └── CombatDamageLog.cs     # 被ダメ・死亡をコンソール出力
    ├── Score/
    │   └── ScoreManager.cs        # スコア集計（EnemyController.OnEnemyKilled 購読 → OnScoreChanged）
    └── UI/
        ├── HpBarUI.cs             # ワールド空間 HP バー（Health.OnDamaged 購読）
        ├── AmmoHudView.cs         # 残弾 {マガジン}/{予備} + 武器名 + 武器アイコン（Magazine 購読）
        └── ScoreHudView.cs        # スコア表示（ScoreManager 購読）
```

## コアデータフロー（射撃）

```
WeaponData (SO) ── Configure() ──► Magazine（ランタイム弾数）
  └─ BulletData (SO)
       └─ BulletConfig (readonly struct、発射時にスナップショット)

PlayerShooter / EnemyShooterAttack
  ├─ Magazine.TryConsume(1)   # 1サルボにつき1発消費（spread 内の同時弾は含まない）
  └─ ShooterCore (static)
       ├─ FireSpread()         # 同時複数方向に発射
       └─ FireSequenceRoutine() # burst / 連続発射
            └─ IBulletPool.Shoot(BulletConfig, position, direction, ownerTeam)
                 └─ Bullet.Launch()
                      └─ 衝突 → Health.CanReceiveDamage() → IDamageable.TakeDamage()

Magazine.OnAmmoChanged ──► AmmoHudView（残弾 / 予備弾 + displayName）
```

弾数の**設定**（容量・リロード時間・予備弾数など）は `WeaponData` が持ち、**状態**（現在の残弾・リロード中か）は `Magazine` が保持します。現状 `Magazine` を使うのはプレイヤー側（`PlayerShooter` が同一 GameObject から取得）のみです。

## スコアデータフロー

```
EnemyData.scoreValue
  └─ EnemyController.OnDied()
       └─ EnemyController.OnEnemyKilled（static event、引数 = scoreValue）
            └─ ScoreManager.AddScore()
                 └─ OnScoreChanged ──► ScoreHudView
```

`ScoreManager` はシーンに 1 つだけ配置します（複数あると加算が重複します）。

## 敵 AI 初期化フロー

```
EnemyData (SO)
  └─ EnemyFactory.Create(EnemyData, Vector2)
       └─ Instantiate(EnemyData.prefab)
            ├─ EnemyController が無ければ AddComponent
            └─ EnemyController.Initialize(data, target, bulletPool)
                 ├─ Health / TeamAffiliation / EnemySensor が無ければ AddComponent
                 ├─ Health.Initialize(data.maxHp)
                 ├─ EnemyMovement.Configure(data.moveSpeed)    ← 実装は Prefab に応じて異なる
                 ├─ EnemyAttack.Configure(pool, teamId)         ← 実装は Prefab に応じて異なる
                 ├─ EnemySensor.Configure(target, data.detectionRange, data.loseSightRange)
                 └─ BuildStateMachine()  ← State インスタンスを生成
```

敵の共通 gameplay 値（陣営、HP、速度、検知/攻撃距離、巡回）は `EnemyData` が source of truth です。Prefab は移動/攻撃 component の種類と、その component 固有の Inspector 値を持ちます。

## システム間の依存関係

```
PlayerShooter ──► Magazine ◄── WeaponData（弾数設定）
       │              └─► AmmoHudView
       ├─► ShooterCore ──► IBulletPool ──► Bullet ──► IDamageable (= Health)
EnemyShooterAttack ─┘                                              └─► TeamAffiliation

Health ──► TeamAffiliation
         └─► HpBarUI（ワールド空間 HP バー）

EnemyController ──► EnemyStateMachine ──► EnemyState 各実装
                 ──► EnemySensor
                 ──► EnemyMovement（EnemyGroundMovement / EnemyJumpingGroundMovement / EnemyFlyingMovement）
                 ──► EnemyAttack（EnemyShooterAttack）
                 ──► OnEnemyKilled ──► ScoreManager ──► ScoreHudView
EnemyFactory ──► EnemyData ──► EnemyController

AudioHelper ──► AudioManager（IAudioService）
                 ├─► SfxPlayer（ObjectPool<AudioSource>）
                 ├─► MusicPlayer（2 トラッククロスフェード）
                 ├─► AmbientPlayer（2 トラッククロスフェード）
                 └─► SoundCooldownTracker（cooldown / polyphony）
PlayerShooter ──► AudioHelper.TryPlay(WeaponData.fireSound)
Magazine      ──► AudioHelper.TryPlay(WeaponData.reloadSound)
EnemyController ► AudioHelper.TryPlay(EnemyData.deathSound)
```

## 各ファイルに含まれる型

複数の型が同一ファイルに同居している箇所をまとめます。

| ファイル | 含まれる型 |
|---------|-----------|
| `Health.cs` | `DamageContext`（struct）、`IDamageable`（interface）、`Health`（MonoBehaviour） |
| `BulletData.cs` | `BulletData`（ScriptableObject）、`BulletConfig`（readonly struct） |
| `BulletPool.cs` | `IBulletPool`（interface）、`BulletPool`（MonoBehaviour） |
| `EnemyStateMachine.cs` | `EnemyState`（abstract class）、`EnemyStateMachine`、各 `EnemyState` 実装 |
| `TeamAffiliation.cs` | `TeamId`（enum）、`TeamAffiliation`（MonoBehaviour） |
| `EnemySpawnPattern.cs` | `EnemyCandidate`（struct）、`EnemySpawnEntry`（Serializable class）、`EnemySpawnPattern`（ScriptableObject） |
| `EnemySpawnPatternRunner.cs` | `SpawnContext`（struct）、`EnemySpawnPatternRunner`（MonoBehaviour） |
| `Magazine.cs` | `Magazine`（MonoBehaviour） |
| `ScoreManager.cs` | `ScoreManager`（MonoBehaviour） |
