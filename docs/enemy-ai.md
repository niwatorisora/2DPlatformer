# 敵 AI システム

Factory パターンと State パターンを組み合わせた敵 AI の構成をまとめます。

## 全体構成

```
EnemyData (ScriptableObject)
  └─ EnemyFactory.Create(data, position)
       └─ Instantiate(data.prefab)
            ├─ EnemyController が無ければ AddComponent
            └─ EnemyController.Initialize(data, target, bulletPool)
                 ├─ Health / TeamAffiliation / EnemySensor が無ければ AddComponent
                 ├─ Health.Initialize(data.maxHp)
                 ├─ EnemyMovement.Configure(data.moveSpeed) ← Prefab の移動コンポーネントに委譲
                 ├─ EnemyAttack.Configure(bulletPool, teamId) ← Prefab の攻撃コンポーネントに委譲
                 ├─ EnemySensor.Configure(target, data.detectionRange, data.loseSightRange)
                 ├─ BuildStateMachine()（State インスタンスを生成）
                 └─ Health.OnDied を購読
```

## EnemyData（ScriptableObject）

敵種ごとのパラメータをまとめたアセット。`Create → Combat → Enemy Data` で作成します。

| フィールド | 説明 |
|-----------|------|
| `prefab` | インスタンス化する敵 Prefab |
| `teamId` | 弾・ダメージ判定に使う陣営（通常の敵は `Enemy`） |
| `maxHp` | 最大 HP |
| `moveSpeed` | 移動速度 |
| `detectionRange` | ターゲット検知距離 |
| `loseSightRange` | Chase 離脱距離（≥ detectionRange を推奨） |
| `attackRange` | 攻撃射程（≤ detectionRange を推奨） |
| `patrolEnabled` | 巡回を行うか |
| `patrolDistance` | 巡回の片道距離 |

## EnemyController（AI ハブ）

毎フレーム `EnemyStateMachine.Tick()` を呼び、各 State から `Movement`・`Attack`・`Sensor`・`Data`・`Target` にアクセスできるプロパティを公開します。

```csharp
// Factory から呼ばれる（BuildStateMachine 後に Health.OnDied を購読）
bool Initialize(EnemyData data, Transform target, IBulletPool bulletPool);

// State から呼ばれる
void ChangeState(EnemyState nextState);
```

`EnemyController` は `Health` / `TeamAffiliation` / `EnemySensor` を runtime wiring として不足時に追加します。Prefab で必須なのは、その敵の挙動を決める `EnemyMovement` 実装と `EnemyAttack` 実装です。

敵の共通値（陣営、HP、移動速度、検知距離、攻撃距離、巡回設定）は `EnemyData` が source of truth です。Prefab 側に `Health` や `TeamAffiliation` があっても、Factory 初期化された敵は `EnemyData` の値で上書きされます。

## EnemySensor

距離判定を 1 か所に集め、将来の Raycast 対応を容易にしています。

```csharp
void Configure(Transform target, float detection, float loseSight);
bool TryDetectTarget(out Transform target);  // detectionRange 以内か
bool IsInAttackRange(Transform target, float attackRange);  // attackRange 以内か
bool HasLostSight(Transform target);         // loseSightRange を超えたか
```

壁越し視線が必要になったら、このクラスの内部のみ `Physics2D.Raycast` に差し替えます。

## 移動コンポーネント（EnemyMovement）

`EnemyMovement` は abstract で、Prefab ごとに異なる実装をアタッチします。
`moveSpeed` は `EnemyData` から注入されます。ジャンプ力や地面判定、飛行ステアリングなど実装固有の値は Prefab Inspector で設定します。

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
武器や射撃位置など攻撃実装ごとの固有値は Prefab Inspector で設定します。

```csharp
virtual bool IsAutoFire { get; }            // true: 連続発射 / false: 1バースト後 Chase へ
abstract void Configure(IBulletPool bulletPool, TeamId ownerTeam);
abstract bool CanAttack(Transform target);  // クールダウン・ターゲット確認
abstract void TryAttack(Transform target);
```

### 実装一覧

| クラス | 特徴 |
|--------|------|
| `EnemyShooterAttack` | `ShooterCore` を使いターゲット方向に射撃。`PlayerShooter` と同じ spread/burst ロジック。`WeaponData` は Prefab Inspector で設定 |

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
| `EnemyAttackState` | 停止して射撃 | 攻撃射程外 → Chase、ロスト → Idle/Patrol、セミオート 1 バースト後 → Chase |
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
- **新しい移動手段**: `EnemyMovement` を継承した新コンポーネントを作り Prefab にアタッチ。共通速度は `EnemyData.moveSpeed` を使い、固有値だけを component に持たせる。
- **新しい攻撃手段（近接など）**: `EnemyAttack` を継承した新コンポーネントを作り Prefab にアタッチ。固有値だけを component に持たせる。
- **複数攻撃手段**: 将来的に `EnemyController` が複数の `EnemyAttack` を管理する設計に拡張可能。
- **敵のプール化**: `EnemyFactory.Create` の内部だけ差し替えれば対応できる。
- **視線遮蔽（壁越し検知）**: `EnemySensor` の内部のみ `Physics2D.Raycast` に変更する。
