# 2D Platformer Prototype Project Overview

## 概要

このプロジェクトは Unity 6000.4.5f1 で作られている 2D プラットフォーマーのプロトタイプです。プレイヤー移動、マウス方向への射撃、弾のプール管理、ScriptableObject による弾・武器データ管理、陣営ベースの味方判定、および State パターンと Factory パターンによる敵 AI が実装されています。

主なシーンは `Assets/Scenes/SampleScene.unity` です。

## 技術構成

- Unity: 6000.4.5f1
- Render Pipeline: Universal Render Pipeline 2D
- 入力: Unity 標準の `Input.GetAxis`, `Input.GetMouseButtonDown`, `Input.GetButtonDown`
- 物理: Rigidbody2D / Collider2D / Trigger
- データ管理: ScriptableObject

## ディレクトリ構成

- `Assets/Scripts/Player`
  - プレイヤー移動と射撃入力を扱います。
- `Assets/Scripts/Combat`
  - HP、ダメージ対象、陣営、被ダメージログを扱います。
- `Assets/Scripts/Combat/Shooting`
  - 弾、弾プール、弾データ、武器データを扱います。
- `Assets/Scripts/Enemy`
  - 敵 AI の中心クラス（`EnemyController`, `EnemySensor`, `EnemyDeathHandler`, `EnemyFactory`, `EnemyData`）を置きます。
- `Assets/Scripts/Enemy/Movement`
  - 敵の移動コンポーネント（`EnemyMovement` abstract + `EnemyGroundMovement`）を置きます。
- `Assets/Scripts/Enemy/Attack`
  - 敵の攻撃コンポーネント（`EnemyAttack` abstract + `EnemyShooterAttack`）を置きます。
- `Assets/Scripts/Enemy/States`
  - 敵 AI のステートクラス群（`EnemyStateMachine`, `EnemyState`, `EnemyIdleState`, `EnemyPatrolState`, `EnemyChaseState`, `EnemyAttackState`, `EnemyDeadState`）を置きます。
- `Assets/Scripts/Diagnostics`
  - Unity Console 向けの共通ログ出力を扱います。
- `Assets/Scripts/Dev`
  - 動作確認用の簡易ターゲットを置いています。
- `Assets/Bullet`
  - 弾用 ScriptableObject などを置きます。
- `Assets/Weapons`
  - 武器用 ScriptableObject などを置きます。

## 射撃システム

射撃は「弾の性能」と「銃の撃ち方」を分けて管理しています。

`BulletData` は、発射された弾がどう振る舞うかを定義します。

- ダメージ
- 速度
- 寿命
- 重力を受けるか
- 衝突対象レイヤー `hitMask`

`WeaponData` は、銃がどう発射するかを定義します。

- 発射する `BulletData`
- クールダウン
- 同時発射数
- spread 角度
- 連続発射数
- 連続発射間隔

たとえば shotgun は、`simultaneousShotCount = 8`, `spreadAngle = 5`, `sequenceShotCount = 1` のように設定します。

プレイヤーは `PlayerShooter` がマウス方向に撃ちます。敵は `EnemyShooterAttack` がターゲット方向に撃ちます。両者は同じ `WeaponData` と `IBulletPool` を共有でき、spread・burst ロジックも同一です。

## HP とダメージ

HP は `Health` コンポーネントで管理します。`Health` は `IDamageable` を実装し、現在HP、最大HP、無敵状態、被ダメージ可否、被ダメージイベント（`OnDamaged`）、死亡イベント（`OnDied`）を扱います。

`Health.Initialize(int maxHp)` を呼ぶと、最大HPをリセットしてスポーン時に `EnemyData` のHPを反映できます。Inspector 値のまま使う場合は呼ばなくても動きます。

被ダメージ・死亡のコンソールログは `CombatDamageLog` コンポーネントを同じ GameObject に付けると `GameLog` 経由で出力されます（プレイヤー・敵プレハブで使用）。

`TeamAffiliation` は陣営だけを担当します。`Health` には `RequireComponent(typeof(TeamAffiliation))` が付いているため、HPを持つキャラクターに `Health` を追加すると陣営コンポーネントも必要になります。

死亡時の標準動作は `OnDied` イベントの発火だけです。Destroy、非表示、リスポーン、スコア加算などは、プレイヤー、敵、破壊物ごとの別コンポーネントが購読して実装します。

## 弾の命中判定

弾は `BulletData.hitMask` に含まれるレイヤーだけを衝突対象として扱います。そのうえで、発射者自身や同じ陣営の相手には当たらず、弾は貫通します。

命中時の基本ルールは次の通りです。

- 発射者自身、発射者の親子 Collider: 無視して貫通
- 同陣営の `Health`: ダメージを拒否して貫通
- `IDamageable` がダメージを受け入れる有効対象: ダメージを与えて弾を戻す
- `IDamageable` を持たない有効対象: ダメージなしで弾を戻す
- `hitMask` 外のレイヤー: 完全に無視

地形は `Health` や `IDamageable` を付けず、`hitMask` に含めることで「弾を止めるだけの対象」として扱えます。

敵の弾がプレイヤーに当たらない場合は、敵の `WeaponData` → `BulletData` の `hitMask` にプレイヤーのレイヤーが含まれているか確認してください。

## 敵システム

### 概要

敵は小さなコンポーネントの組み合わせで表現します。`Health` や `TeamAffiliation` など戦闘共通部品を再利用し、AI 固有の部品を追加する構成です。

```
EnemyData (ScriptableObject)
  └─ EnemyFactory (MonoBehaviour)
       └─ EnemyController.Initialize()
            ├─ EnemyStateMachine
            │    ├─ EnemyIdleState
            │    ├─ EnemyPatrolState
            │    ├─ EnemyChaseState
            │    ├─ EnemyAttackState
            │    └─ EnemyDeadState
            ├─ EnemySensor
            ├─ EnemyMovement (abstract)
            │    └─ EnemyGroundMovement
            ├─ EnemyAttack (abstract)
            │    └─ EnemyShooterAttack
            ├─ Health
            └─ TeamAffiliation (Enemy)
```

### EnemyData (ScriptableObject)

`Create → Combat → Enemy Data` で作成する、敵種ごとのパラメータアセット。

- `prefab` — 敵 Prefab
- `maxHp` — 最大HP
- `moveSpeed` — 移動速度
- `detectionRange` — 検知射程
- `loseSightRange` — Chase を離脱する距離（≥ detectionRange）
- `attackRange` — 攻撃射程
- `weaponData` — 発射設定
- `patrolEnabled` / `patrolDistance` — 巡回の有無と幅

### EnemyController (MonoBehaviour)

敵の中心ハブ。`Initialize(EnemyData, Transform, IBulletPool)` を Factory から呼ばれた後、毎フレーム `EnemyStateMachine.Tick` を実行します。各 State は Controller 経由で `Movement`, `Attack`, `Sensor`, `Data`, `Target` にアクセスします。

`EnemyDeathHandler` が `Health.OnDied` を購読して `EnemyController.OnDied()` を呼ぶことで Dead 状態へ遷移します。

### 状態遷移

| 状態 | 説明 | 次の状態 |
|------|------|---------|
| Idle | 停止待機 | target 検知 → Chase、patrolEnabled → Patrol |
| Patrol | 左右往復 | target 検知 → Chase |
| Chase | target に接近 | 攻撃射程内 → Attack、視界外 → Idle/Patrol |
| Attack | 停止して射撃 | 攻撃射程外 → Chase、視界外 → Idle/Patrol |
| Dead | 停止 → 0.5 秒後に Destroy | — |

State クラスは MonoBehaviour ではなく普通の C# クラスです。`EnemyController.Initialize` のタイミングで生成されます。

### EnemySensor (MonoBehaviour)

距離判定を一箇所に集めたコンポーネント。`TryDetectTarget`, `IsInAttackRange`, `HasLostSight` を提供します。現在は直線距離のみ。壁越し視線判定が必要になったら、このクラスの内部だけ `Physics2D.Raycast` に差し替えます。

### EnemyMovement / EnemyGroundMovement

`EnemyMovement` は `Configure(moveSpeed)` / `MoveToward(worldPos)` / `Stop()` の abstract。`EnemyGroundMovement` は `Rigidbody2D.linearVelocity.x` を FixedUpdate で更新する地上移動実装で、縦方向の速度は保持します。

### EnemyAttack / EnemyShooterAttack

`EnemyAttack` は `Configure` / `CanAttack` / `TryAttack` の abstract。`EnemyShooterAttack` は `WeaponData` と `IBulletPool` を使いターゲット方向に射撃します。`PlayerShooter` と同じ spread・burst ロジックを持つため、同じ `WeaponData` アセットがそのまま使えます。近接攻撃などを追加するときは `EnemyAttack` を継承して別コンポーネントを作ります。

### EnemyFactory (MonoBehaviour)

`Create(EnemyData, Vector2)` で Prefab を Instantiate → `Initialize` → `EnemyController` を返します。`bulletPool` と `playerTarget` を Inspector で設定します。

開発用として `Debug Enemy Data` + `Spawn On Start` チェックボックスと `[ContextMenu] Debug Spawn Here` も持ちます。

## ログ出力

ゲームプレイやデバッグ用のログは `GameLog` 経由で出力します。Unity Console では `[Debug:ClassName] message` のように、ログ種別と出力元クラス名の接頭辞が付きます。

`GameLog.Debug(this, "message")` のように Unity の `Object` を渡すと、Console から該当コンポーネントやアセットを辿れます。

## 陣営

陣営は `TeamAffiliation` コンポーネントで管理します。Layer 名だけでは味方判定になりません。

現在の陣営は次の通りです。

- `Neutral`
- `Player`
- `Ally`
- `Enemy`

`Player` と `Ally` は味方同士として扱われます。`Enemy` 同士も味方同士として扱われます。`Neutral` は味方判定に含めません。

## 今後の拡張方針

- 新しい弾種は `BulletData` を追加して作る。
- 新しい銃種は `WeaponData` を追加し、発射数・spread・連続発射数で表現する。
- 新しい敵種は `EnemyData` アセットと Prefab を追加するだけで作れる。
- 新しい攻撃手法（近接など）は `EnemyAttack` を継承した新コンポーネントで追加する。
- 弾を止めるだけのオブジェクトは `IDamageable` を付けず、対象レイヤーを `BulletData.hitMask` に含める。
- ダメージ軽減や耐性は、まず `Health.CalculateDamage` の差し替えや周辺コンポーネントで拡張する。
- 視線遮蔽（壁越し検知）が必要になったら `EnemySensor` 内部を `Physics2D.Raycast` に差し替える。
- 敵のプール化が必要になったら `EnemyFactory.Create` の内部を差し替えるだけで対応できる。
