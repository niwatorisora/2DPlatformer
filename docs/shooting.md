# 射撃システム

射撃は「弾の性能」と「銃の撃ち方」を分けて管理しています。

## 型の配置

| 型 | 種別 | ファイル |
|----|------|---------|
| `BulletData` | ScriptableObject | `BulletData.cs` |
| `BulletConfig` | readonly struct | `BulletData.cs` |
| `WeaponData` | ScriptableObject | `WeaponData.cs` |
| `Magazine` | MonoBehaviour | `Magazine.cs` |
| `IBulletPool` | interface | `BulletPool.cs` |
| `BulletPool` | MonoBehaviour | `BulletPool.cs` |
| `Bullet` | MonoBehaviour | `Bullet.cs` |
| `ShooterCore` | static class（internal） | `ShooterCore.cs` |

## データ定義

### BulletData（弾の性能 ScriptableObject）

弾が発射された後にどう振る舞うかを定義します。

| フィールド | 説明 |
|-----------|------|
| `speed` | 初速 |
| `lifeTime` | 寿命（秒）。超えると自動返却 |
| `useGravity` | 重力を受けるか |
| `gravityScale` | 重力スケール（useGravity が true のとき有効） |
| `damage` | ダメージ量 |
| `hitMask` | 衝突対象レイヤー（LayerMask） |

`Create → Combat → Bullet Data` で新規作成できます。

### BulletConfig（発射時スナップショット）

`BulletData.From(BulletData so)` で生成する `readonly struct`。発射後に SO が書き変わっても弾の動作が変わらないよう、値を固定します。`Bullet.Launch` に渡されます。

### WeaponData（銃の撃ち方 ScriptableObject）

どのように発射するかを定義します。弾の性能は `BulletData` に委譲します。

| フィールド | 説明 |
|-----------|------|
| `displayName` | HUD などに表示する武器名 |
| `bulletData` | 使用する `BulletData` |
| `autoFire` | `true` のとき長押し連続発射。`false`（デフォルト）はクリックごとに 1 バースト |
| `cooldown` | 発射間隔（秒） |
| `simultaneousShotCount` | 同時発射数（ショットガンは 8 など） |
| `spreadAngle` | 拡散角（度）。0 なら直線 |
| `sequenceShotCount` | 連続発射数（バースト） |
| `sequenceInterval` | 連続発射間隔（秒） |
| `magazineSize` | マガジン 1 個の容量 |
| `reloadTime` | リロード所要秒数 |
| `startingReserveAmmo` | 開始時の手持ち予備弾 |
| `infiniteReserve` | `true` なら予備弾無限（マガジン容量だけは有限） |
| `autoReloadWhenEmpty` | マガジンが空になったら自動でリロード開始 |

`Create → Combat → Weapon Data` で新規作成できます。

弾数の**設定**は `WeaponData`、**ランタイム状態**（残弾・リロード中か）は `Magazine` が保持します。`Magazine` は `Configure(WeaponData)` で初期化し、マガジンを満タンにします。

**設定例:**

| 武器種 | autoFire | simultaneous | spread | sequence | interval |
|--------|----------|-------------|--------|----------|----------|
| ハンドガン | false | 1 | 0 | 1 | 0 |
| ショットガン | false | 8 | 5 | 1 | 0 |
| バーストライフル | false | 1 | 0 | 3 | 0.08 |
| マシンガン | true | 1 | 1–3 | 1 | 0 |

## 弾数管理（Magazine）

`Magazine` は単一シューター（現状はプレイヤー）のランタイム弾数を管理します。`WeaponData` から容量・リロード時間などを受け取り、残弾と予備弾を保持します。

### 主要 API

```csharp
void Configure(WeaponData weaponData);  // 容量・予備弾を初期化しマガジン満タン
bool TryConsume(int count = 1);         // 弾消費。不足/リロード中は false
void StartReload();                     // 手動リロード開始
```

### リロードの挙動

- **プール式**: マガジンを容量まで補充し、必要な分だけ予備弾から引く。既にマガジンに入っている弾は失わない。
- `autoReloadWhenEmpty` が `true` のとき、マガジンが空になると自動で `StartReload()` する。
- リロード中は `CanFire` が `false` になり発射できない。
- `infiniteReserve` が `true` のとき予備弾は減らない（HUD では `∞` 表示）。

### イベント（HUD 購読用）

```csharp
event Action OnAmmoChanged;
event Action OnReloadStarted;
event Action OnReloadCompleted;
```

### 弾消費の単位

`PlayerShooter` は **1 サルボ（`FireSpread` 1 回）につき 1 発** を `TryConsume` します。ショットガンの同時 8 発でも消費は 1 です。バースト（`sequenceShotCount > 1`）ではサルボごとに消費します。

## 発射ロジック（ShooterCore）

`PlayerShooter` と `EnemyShooterAttack` の共通発射ロジックを `internal static` クラスとして切り出しています。

```csharp
// spread + simultaneous 発射
static void FireSpread(Vector2 direction, BulletConfig config,
    WeaponData weaponData, IBulletPool bulletPool,
    GameObject owner, TeamId ownerTeam, Func<Vector2, Vector2> getSpawnPos);

// burst / sequence コルーチン
// doOneSalvo: 1サルボ分の FireSpread 呼び出し
// onComplete: コルーチン終了後のコールバック（fireSequence = null など）
static IEnumerator FireSequenceRoutine(WeaponData weaponData,
    Action doOneSalvo, Action onComplete);
```

### PlayerShooter と EnemyShooterAttack の違い

| 項目 | PlayerShooter | EnemyShooterAttack |
|------|-------------|-------------------|
| 照準 | マウスカーソル方向 | Target Transform 方向 |
| 発射起点 | `shootOrigin` Transform 固定 | 発射ごとに Target 方向を再計算 |
| 入力 | `Input.GetMouseButton(0)` / `R` でリロード | `EnemyAttackState.Tick()` から呼ばれる |
| 弾数 | 同一 GameObject の `Magazine` を使用 | 未使用（無制限射撃） |

## BulletPool / IBulletPool

`ObjectPool<Bullet>` を使ったオブジェクトプールです。シーン内に 1 つ置き、`EnemyFactory` と `PlayerShooter` がシリアライズフィールドで参照します。

```csharp
public interface IBulletPool
{
    void Shoot(Vector2 position, Vector2 direction, BulletConfig config,
               GameObject owner, TeamId ownerTeam);
}
```

### Inspector 設定項目

| フィールド | 説明 |
|-----------|------|
| `bulletPrefab` | `Bullet` コンポーネントを持つ Prefab |
| `defaultCapacity` | 初期プールサイズ |
| `maxSize` | 最大プールサイズ |
| `container` | 非アクティブ弾の親 Transform（整理用） |

## Bullet の動作

発射から返却までの流れ：

1. `BulletPool` がプールから取り出し `Launch(onRelease, direction, config, owner, ownerTeam)` を呼ぶ
2. `Rigidbody2D` に初速を与え、`useGravity` に応じて `gravityScale` を設定
3. `col.Overlap()` で発射直後に重複しているコライダーを即時 `ProcessHit` で処理
4. `OnTriggerEnter2D` で衝突検知 → `ProcessHit()` で命中判定
5. ダメージ適用またはスルー後、必要に応じてプールに返却

### 命中判定の順序

```
1. hitMask にレイヤーが含まれる?  → false → スルー（return）
2. IsOwnerCollider?     → true → スルー（return）
3. IDamageable を GetComponentInParent で取得
4. IDamageable が存在する場合:
   a. CanReceiveDamage? → false → スルー（return）
   b. TakeDamage()      → プール返却
5. IDamageable が存在しない場合:
   a. IsFriendlyCollider? → true → スルー（return）
   b. hitMask 内の非 Damageable オブジェクト → プール返却（地形等）
```

## 拡張方針

- **新しい弾種**: `BulletData` アセットを新規作成するだけ。
- **新しい武器**: `WeaponData` アセットを新規作成し、`bulletData` を参照させるだけ。プレイヤーに弾数制限を付ける場合は同一 GameObject に `Magazine` を追加し、`PlayerShooter` の `weaponData` と整合させる。
- **既存の simultaneousShotCount + spreadAngle + sequenceShotCount + sequenceInterval` モデルで表現できない撃ち方が必要になった場合のみ** `ShooterCore` を拡張する。それまで fire-mode ストラテジークラスは追加しない。
- **敵の新しい攻撃手段（近接など）**: `EnemyAttack` を継承した別コンポーネントを作る（`ShooterCore` は不使用）。
