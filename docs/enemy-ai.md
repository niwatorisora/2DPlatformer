# 敵 AI システム

Factory パターンと State パターンを組み合わせた敵 AI の構成をまとめます。

## 全体構成

```
EnemyData (ScriptableObject)
  └─ EnemyFactory.Create(data, position)
       └─ Instantiate(data.prefab)
            └─ EnemyController.Initialize(data, target, bulletPool)
                 ├─ Health.Initialize(data.maxHp)
                 ├─ EnemySensor.Configure(data, target)
                 ├─ EnemyMovement.Configure(data.moveSpeed)   ← Prefab の移動コンポーネントに委譲
                 ├─ EnemyAttack.Configure(data, bulletPool, teamId)  ← Prefab の攻撃コンポーネントに委譲
                 └─ EnemyStateMachine（State インスタンスを生成）
```

## EnemyData（ScriptableObject）

敵種ごとのパラメータをまとめたアセット。`Create → Combat → Enemy Data` で作成します。

| フィールド | 説明 |
|-----------|------|
| `prefab` | インスタンス化する敵 Prefab |
| `maxHp` | 最大 HP |
| `moveSpeed` | 移動速度 |
| `detectionRange` | ターゲット検知距離 |
| `loseSightRange` | Chase 離脱距離（≥ detectionRange を推奨） |
| `attackRange` | 攻撃射程（≤ detectionRange を推奨） |
| `weaponData` | 使用する WeaponData（EnemyShooterAttack が参照） |
| `patrolEnabled` | 巡回を行うか |
| `patrolDistance` | 巡回の片道距離 |

## EnemyController（AI ハブ）

毎フレーム `EnemyStateMachine.Tick()` を呼び、各 State から `Movement`・`Attack`・`Sensor`・`Data`・`Target` にアクセスできるプロパティを公開します。

```csharp
// Factory から呼ばれる
void Initialize(EnemyData data, Transform target, IBulletPool bulletPool);

// EnemyDeathHandler から呼ばれる
void OnDied();

// State から呼ばれる
void ChangeState(EnemyState nextState);
```

`RequireComponent(typeof(Health), typeof(TeamAffiliation))` が付いており、これらは Prefab に必須です。

## EnemySensor

距離判定を 1 か所に集め、将来の Raycast 対応を容易にしています。

```csharp
void Configure(EnemyData data, Transform target);
bool TryDetectTarget();     // detectionRange 以内か
bool IsInAttackRange();     // attackRange 以内か
bool HasLostSight();        // loseSightRange を超えたか
```

壁越し視線が必要になったら、このクラスの内部のみ `Physics2D.Raycast` に差し替えます。

## EnemyDeathHandler

`Health.OnDied` を購読し、`EnemyController.OnDied()` を呼ぶだけの橋渡しコンポーネントです。死亡時の実際の処理（Dead State への遷移 → Destroy）は `EnemyController` と `EnemyDeadState` が行います。

## 移動コンポーネント（EnemyMovement）

`EnemyMovement` は abstract で、Prefab ごとに異なる実装をアタッチします。

```csharp
abstract void Configure(float moveSpeed);
abstract void MoveToward(Vector2 worldPosition);
abstract void Stop();
```

### 実装一覧

| クラス | 特徴 | 主な使用 Prefab |
|--------|------|----------------|
| `EnemyGroundMovement` | `Rigidbody2D.linearVelocity.x` を FixedUpdate で更新。縦速度は保持 | `Enemy.prefab` |
| `EnemyJumpingGroundMovement` | `EnemyGroundMovement` を継承。ターゲットが上方にいるとき自動ジャンプ | `JumpingEnemy.prefab` |
| `EnemyFlyingMovement` | `gravityScale=0`、全方向 2D 移動。Lerp でスムーズにステアリング | — |

## 攻撃コンポーネント（EnemyAttack）

`EnemyAttack` は abstract で、Prefab ごとに実装をアタッチします。

```csharp
abstract void Configure(EnemyData data, IBulletPool bulletPool, TeamId team);
abstract bool CanAttack();   // クールダウン確認
abstract void TryAttack(Transform target);
```

### 実装一覧

| クラス | 特徴 |
|--------|------|
| `EnemyShooterAttack` | `ShooterCore` を使いターゲット方向に射撃。`PlayerShooter` と同じ spread/burst ロジック |

近接攻撃などは `EnemyAttack` を継承した別コンポーネントとして追加します。

## ステートマシン

`EnemyStateMachine` と `EnemyState`（abstract）は `EnemyStateMachine.cs` に同居しています。State は MonoBehaviour ではなく、`EnemyController.Initialize` のタイミングで `new` されます。

```csharp
// EnemyState の基本構造
class EnemyState
{
    protected EnemyController ctrl;
    virtual void Enter() {}
    virtual void Tick()  {}
    virtual void Exit()  {}
}
```

### 状態遷移表

| 状態 | 説明 | 遷移先 |
|------|------|--------|
| `EnemyIdleState` | 停止して待機 | 検知 → Chase、`patrolEnabled` → Patrol |
| `EnemyPatrolState` | スポーン位置を軸に左右往復 | 検知 → Chase |
| `EnemyChaseState` | ターゲットに接近 | 攻撃射程内 → Attack、ロスト → Idle/Patrol |
| `EnemyAttackState` | 停止して射撃 | 攻撃射程外 → Chase、ロスト → Idle/Patrol |
| `EnemyDeadState` | 停止 → 0.5 秒後に `Destroy` | — |

## EnemyFactory

開発・ゲームプレイ中に敵をスポーンするエントリーポイントです。

```csharp
// 外部から呼ぶ
EnemyController Create(EnemyData data, Vector2 position);
```

Inspector 設定項目：

| フィールド | 説明 |
|-----------|------|
| `bulletPool` | シーン内の `BulletPool` への参照 |
| `playerTarget` | 敵が追跡するターゲット（通常はプレイヤー） |
| `debugEnemyData` | デバッグスポーン用の `EnemyData` |
| `spawnOnStart` | Start 時に自動スポーンするか |
| `spawnOffset` | スポーン位置のオフセット |

`[ContextMenu] Debug Spawn Here` で Editor 上からスポーンを実行できます。

## 拡張方針

- **新しい敵種**: `EnemyData` アセットと Prefab を追加するだけ（コードの変更不要）。
- **新しい移動手段**: `EnemyMovement` を継承した新コンポーネントを作り Prefab にアタッチ。
- **新しい攻撃手段（近接など）**: `EnemyAttack` を継承した新コンポーネントを作り Prefab にアタッチ。
- **複数攻撃手段**: 将来的に `EnemyController` が複数の `EnemyAttack` を管理する設計に拡張可能。
- **敵のプール化**: `EnemyFactory.Create` の内部だけ差し替えれば対応できる。
- **視線遮蔽（壁越し検知）**: `EnemySensor` の内部のみ `Physics2D.Raycast` に変更する。
