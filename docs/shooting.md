# 射撃システム

射撃は「弾の性能」と「銃の撃ち方」を分けて管理しています。

## 型の配置

| 型 | 種別 | ファイル |
|----|------|---------|
| `BulletData` | ScriptableObject | `BulletData.cs` |
| `BulletConfig` | readonly struct | `BulletData.cs` |
| `WeaponData` | ScriptableObject | `WeaponData.cs` |
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
| `bulletData` | 使用する `BulletData` |
| `cooldown` | 発射間隔（秒） |
| `simultaneousShotCount` | 同時発射数（ショットガンは 8 など） |
| `spreadAngle` | 拡散角（度）。0 なら直線 |
| `sequenceShotCount` | 連続発射数（バースト） |
| `sequenceInterval` | 連続発射間隔（秒） |

`Create → Combat → Weapon Data` で新規作成できます。

**設定例:**

| 武器種 | simultaneous | spread | sequence | interval |
|--------|-------------|--------|----------|----------|
| ハンドガン | 1 | 0 | 1 | 0 |
| ショットガン | 8 | 5 | 1 | 0 |
| バーストライフル | 1 | 0 | 3 | 0.08 |

## 発射ロジック（ShooterCore）

`PlayerShooter` と `EnemyShooterAttack` の共通発射ロジックを `internal static` クラスとして切り出しています。

```csharp
// spread + simultaneous 発射
static void FireSpread(WeaponData weapon, BulletConfig config,
    IBulletPool pool, Vector2 origin, Vector2 baseDir, TeamId team);

// burst / sequence コルーチン
static IEnumerator FireSequenceRoutine(MonoBehaviour owner,
    WeaponData weapon, BulletConfig config,
    IBulletPool pool, Func<Vector2> getOrigin, Func<Vector2> getDir, TeamId team);
```

### PlayerShooter と EnemyShooterAttack の違い

| 項目 | PlayerShooter | EnemyShooterAttack |
|------|-------------|-------------------|
| 照準 | マウスカーソル方向 | Target Transform 方向 |
| 発射起点 | `shootOrigin` Transform 固定 | 発射ごとに Target 方向を再計算 |
| 入力 | `Input.GetMouseButton(0)` | `EnemyAttackState.Tick()` から呼ばれる |

## BulletPool / IBulletPool

`ObjectPool<Bullet>` を使ったオブジェクトプールです。シーン内に 1 つ置き、`EnemyFactory` と `PlayerShooter` がシリアライズフィールドで参照します。

```csharp
public interface IBulletPool
{
    void Shoot(BulletConfig config, Vector2 position, Vector2 direction, TeamId ownerTeam,
               IEnumerable<Collider2D> ownerColliders);
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

1. `BulletPool` がプールから取り出し `Launch(config, position, direction, team, ownerColliders)` を呼ぶ
2. `Rigidbody2D` に初速を与え、`useGravity` に応じて `gravityScale` を設定
3. `OverlapCircle` で発射直後の重複コライダーを無視リストに追加
4. `OnTriggerEnter2D` で衝突検知 → `ProcessHit()` で命中判定
5. ダメージ適用またはスルー後、必要に応じてプールに返却

### 命中判定の順序

```
1. IsOwnerCollider?     → true → スルー（return）
2. hitMask にレイヤーが含まれる?  → false → スルー（return）
3. IDamageable を取得
4. IsFriendlyCollider?  → true → スルー（return）
5. IDamageable が null? → プール返却（地形等）
6. CanReceiveDamage?    → false → スルー（return）
7. TakeDamage()         → プール返却
```

## 拡張方針

- **新しい弾種**: `BulletData` アセットを新規作成するだけ。
- **新しい武器**: `WeaponData` アセットを新規作成し、`bulletData` を参照させるだけ。
- **既存の simultaneousShotCount + spreadAngle + sequenceShotCount + sequenceInterval` モデルで表現できない撃ち方が必要になった場合のみ** `ShooterCore` を拡張する。それまで fire-mode ストラテジークラスは追加しない。
- **敵の新しい攻撃手段（近接など）**: `EnemyAttack` を継承した別コンポーネントを作る（`ShooterCore` は不使用）。
