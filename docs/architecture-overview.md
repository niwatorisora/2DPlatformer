# アーキテクチャ概要

プロジェクト全体の構成と、主要システム間のデータフローをまとめます。

## ディレクトリ構成

```
Assets/
├── Bullet/                    # BulletData ScriptableObject アセット
├── Weapons/                   # WeaponData ScriptableObject アセット
├── Prefab/                    # 敵・弾 Prefab とデータアセット
├── Scenes/
│   └── SampleScene.unity      # 開発用メインシーン
└── Scripts/
    ├── Player/
    │   ├── PlayerMovement.cs  # 左右移動・ジャンプ
    │   └── PlayerShooter.cs   # マウス照準射撃
    ├── Combat/
    │   ├── Health.cs          # IDamageable, DamageContext, Health を同居
    │   ├── TeamAffiliation.cs # TeamId enum + 味方判定
    │   ├── BulletDatas/
    │   │   └── BulletData.cs  # BulletData SO + BulletConfig struct
    │   ├── WeaponDatas/
    │   │   └── WeaponData.cs  # WeaponData SO（発射パラメータ）
    │   └── Shooting/
    │       ├── BulletPool.cs  # IBulletPool interface + BulletPool
    │       ├── Bullet.cs      # 弾の移動・衝突・ダメージ・プール返却
    │       └── ShooterCore.cs # spread/sequence の共有発射ロジック（internal static）
    ├── Enemy/
    │   ├── EnemyDatas/
    │   │   └── EnemyData.cs       # 敵種パラメータ ScriptableObject
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
    │   ├── EnemyStateMachine.cs   # EnemyState abstract + EnemyStateMachine を同居
    │   ├── EnemyIdleState.cs
    │   ├── EnemyPatrolState.cs
    │   ├── EnemyChaseState.cs
    │   ├── EnemyAttackState.cs
    │   └── EnemyDeadState.cs
    ├── Enemy/Spawn/
    │   ├── EnemySpawnPoint.cs         # 出現地点（groupId, Gizmo）
    │   ├── SpawnContext.cs            # 実行時入力（waveIndex, difficulty, useSeed, seed）
    │   ├── EnemySpawnEntry.cs         # 時間差付き出現エントリ
    │   ├── EnemySpawnPattern.cs       # ScriptableObject（エントリ列）
    │   ├── EnemySpawnPatternSet.cs    # ScriptableObject（パターン候補）
    │   └── EnemySpawnPatternRunner.cs # パターン実行器
    └── Diagnostics/
        ├── GameLog.cs             # [Level:ClassName] 形式のコンソールログ
        └── CombatDamageLog.cs     # 被ダメ・死亡をコンソール出力
```

## コアデータフロー（射撃）

```
WeaponData (SO)
  └─ BulletData (SO)
       └─ BulletConfig (readonly struct、発射時にスナップショット)

PlayerShooter / EnemyShooterAttack
  └─ ShooterCore (static)
       ├─ FireSpread()         # 同時複数方向に発射
       └─ FireSequenceRoutine() # burst / 連続発射
            └─ IBulletPool.Shoot(BulletConfig, position, direction, ownerTeam)
                 └─ Bullet.Launch()
                      └─ 衝突 → Health.CanReceiveDamage() → IDamageable.TakeDamage()
```

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
PlayerShooter ──┐
                ├─► ShooterCore ──► IBulletPool ──► Bullet ──► IDamageable (= Health)
EnemyShooterAttack ─┘                                              └─► TeamAffiliation

Health ──► TeamAffiliation
EnemyController ──► EnemyStateMachine ──► EnemyState 各実装
                 ──► EnemySensor
                 ──► EnemyMovement（EnemyGroundMovement / EnemyJumpingGroundMovement / EnemyFlyingMovement）
                 ──► EnemyAttack（EnemyShooterAttack）
EnemyFactory ──► EnemyData ──► EnemyController
```

## 各ファイルに含まれる型

複数の型が同一ファイルに同居している箇所をまとめます。

| ファイル | 含まれる型 |
|---------|-----------|
| `Health.cs` | `DamageContext`（struct）、`IDamageable`（interface）、`Health`（MonoBehaviour） |
| `BulletData.cs` | `BulletData`（ScriptableObject）、`BulletConfig`（readonly struct） |
| `BulletPool.cs` | `IBulletPool`（interface）、`BulletPool`（MonoBehaviour） |
| `EnemyStateMachine.cs` | `EnemyState`（abstract class）、`EnemyStateMachine`（class） |
| `TeamAffiliation.cs` | `TeamId`（enum）、`TeamAffiliation`（MonoBehaviour） |
