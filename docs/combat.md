# 戦闘システム

HP 管理・ダメージ処理・陣営判定・弾の命中ルールをまとめます。

## 型の配置

| 型 | 種別 | ファイル |
|----|------|---------|
| `DamageContext` | readonly struct | `Health.cs` |
| `IDamageable` | interface | `Health.cs` |
| `Health` | MonoBehaviour | `Health.cs` |
| `TeamId` | enum | `TeamAffiliation.cs` |
| `TeamAffiliation` | MonoBehaviour | `TeamAffiliation.cs` |
| `CombatDamageLog` | MonoBehaviour | `CombatDamageLog.cs` |

## 陣営（TeamAffiliation / TeamId）

`TeamAffiliation` コンポーネントが陣営 ID を保持します。Layer 名だけでは味方判定になりません。

### TeamId の値

```csharp
public enum TeamId
{
    Neutral = 0,
    Ally    = 1,
    Enemy   = 2,
}
```

> **注意**: `Player` という enum 値は存在しません。プレイヤーには `Ally` を設定します。

### Inspector での設定

| 対象 | 設定場所 | teamId |
|------|----------|--------|
| Player | `TeamAffiliation` | `Ally`（= 1） |
| Enemy | `EnemyData` | `Enemy`（= 2） |

### 味方判定（AreFriendly）

```csharp
// Neutral は誰とも友好でない
public static bool AreFriendly(TeamId a, TeamId b)
    => a != TeamId.Neutral && a == b;
```

- `Ally` 同士 → 友好（弾が貫通する）
- `Enemy` 同士 → 友好（弾が貫通する）
- `Ally` と `Enemy` → 非友好（弾が当たる）
- どちらかが `Neutral` → 非友好

## HP 管理（Health）

`Health` は HP の保持、被ダメ拒否、イベント発火を担います。死亡後の処理（Destroy や演出）は `Health.OnDied` を購読する別コンポーネントが行います。

### 主要 API

```csharp
// スポーン時に MaxHp を上書きしたい場合に呼ぶ
// Inspector 値のままでよければ呼ばなくてもよい
void Initialize(int maxHp);

// Bullet が命中前に確認する
bool CanReceiveDamage(DamageContext ctx);

// IDamageable 実装（外部から直接は呼ばない）
void TakeDamage(DamageContext ctx);

// ダメージ量のカスタマイズ用（override で軽減・耐性を実装できる）
protected virtual int CalculateDamage(DamageContext ctx);
```

### イベント

```csharp
event Action<int> OnDamaged;  // 引数: 実際に減ったHP量
event Action       OnDied;
```

### 関連コンポーネント

`Health` には `[RequireComponent(typeof(TeamAffiliation))]` が付いています。HP を持つ GameObject には `TeamAffiliation` も必ず必要です。

## ダメージコンテキスト（DamageContext）

```csharp
public readonly struct DamageContext
{
    public readonly int         amount;
    public readonly GameObject  source;       // 発射した GameObject
    public readonly TeamId      sourceTeam;
    public readonly Collider2D  hitCollider;  // 命中したコライダー
}
```

`Bullet` が命中時に生成し、`IDamageable.TakeDamage` に渡します。

## 弾の命中ルール

弾（`Bullet`）は以下の優先順位でコライダーを処理します。

| 条件 | 動作 |
|------|------|
| 発射者自身・発射者の子 Collider | 無視して貫通 |
| `hitMask` 外のレイヤー | 完全に無視（OnTrigger すら呼ばれない） |
| 同陣営の `Health` | `CanReceiveDamage` が false → 貫通 |
| `IDamageable` を持つ有効な対象 | ダメージを与えてプールに返却 |
| `IDamageable` を持たない有効な対象 | ダメージなしでプールに返却（地形など） |

### 地形・障害物の設定

地形は `Health` / `IDamageable` を付けず、`BulletData.hitMask` に対象レイヤーを含めるだけで「弾を止めるだけのオブジェクト」として扱えます。

### トラブルシューティング

- 敵の弾がプレイヤーに当たらない → 敵 `WeaponData` → `BulletData.hitMask` にプレイヤーのレイヤーが含まれているか確認
- 同陣営なのに弾が当たる → 発射者と対象の `TeamAffiliation.TeamId` が同じ値か確認
- 弾が地形で止まらない → `BulletData.hitMask` に地形レイヤーが含まれているか確認

## CombatDamageLog

同じ GameObject に付けると `Health.OnDamaged` / `Health.OnDied` を `GameLog` 経由でコンソール出力します。本番では外すか、ビルドフラグで無効化してください。

```csharp
// 使用例：Enemy.prefab や Player に付けるだけ
[RequireComponent(typeof(Health))]
public class CombatDamageLog : MonoBehaviour { ... }
```
