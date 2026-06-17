# データアセット

ScriptableObject アセットと Prefab の配置・命名規則、および新規追加の手順をまとめます。

## フォルダ構成

```
Assets/
├── Scripts/Combat/BulletDatas/   # BulletData アセット
├── Scripts/Combat/WeaponDatas/   # WeaponData アセット
└── Scripts/Enemy/EnemyDatas/     # EnemyData・SpawnPattern 系アセット
    Prefab/                       # 敵 Prefab（プロジェクト内の配置は Prefab/ 配下）
```

## 既存アセット

### Assets/Scripts/Combat/BulletDatas/

| アセット名 | 型 | 主な設定 |
|-----------|-----|---------|
| `ShotgunBullet.asset` | `BulletData` | speed 10, damage 10, hitMask でプレイヤー・敵レイヤーを対象 |

### Assets/Scripts/Combat/WeaponDatas/

| アセット名 | 型 | 主な設定 |
|-----------|-----|---------|
| `Shotgun.asset` | `WeaponData` | simultaneous=8, spread=10, magazine=8, reload=3s, reserve=120 |
| `AssaultRifle.asset` | `WeaponData` | autoFire, simultaneous=8, spread=0, magazine=30, reload=1.5s, reserve=120 |

### Assets/Prefab/ および Assets/Scripts/Enemy/EnemyDatas/

| アセット名 | 型 | 説明 |
|-----------|-----|------|
| `BasicEnemy.prefab` | Prefab | 基本地上敵（`EnemyGroundMovement` + `EnemyShooterAttack`） |
| `JumpingEnemy.prefab` | Prefab | ジャンプ敵（`EnemyJumpingGroundMovement` + `EnemyShooterAttack`） |
| `BasicEnemy.asset` | `EnemyData` | `BasicEnemy.prefab` を使用する敵データ |
| `JumpingEnemyData.asset` | `EnemyData` | `JumpingEnemy.prefab` を使用する敵データ |

## 新しい弾種の追加

1. `Assets/Scripts/Combat/BulletDatas/` を右クリック → `Create → Combat → Bullet Data`
2. 名前を `<種別>Bullet.asset` に設定（例: `SniperBullet.asset`）
3. Inspector で speed / damage / lifeTime / hitMask を設定
4. `WeaponData` の `bulletData` フィールドに参照をセット

## 新しい武器の追加

1. `Assets/Scripts/Combat/WeaponDatas/` を右クリック → `Create → Combat → Weapon Data`
2. 名前を `<武器名>.asset` に設定（例: `SniperRifle.asset`）
3. Inspector で `bulletData` を参照し、cooldown / simultaneous / spread / sequence / interval / magazine 系フィールドを設定
4. **武器スプライトを設定したい場合**: `weaponSprite` フィールドに Sprite アセットを割り当てる（`AmmoHudView` の `weaponIconImage` に Image コンポーネントを参照させると HUD に反映される）
5. プレイヤーに弾数制限を付ける場合: プレイヤー GameObject に `Magazine` を追加し、`PlayerShooter` の **Weapon Data** と同じ `WeaponData` を参照させる（`Awake` で自動 `Configure`）
6. `PlayerShooter` または `EnemyShooterAttack` Prefab の **Weapon Data** フィールドに参照をセット

**パラメータ設計の指針:**

| 武器種 | simultaneous | spread | sequence | cooldown |
|--------|-------------|--------|----------|---------|
| ピストル | 1 | 0 | 1 | 0.5 |
| ショットガン | 6–8 | 5–15 | 1 | 0.8 |
| バーストライフル | 1 | 0 | 3 | 0.6 |
| マシンガン | 1 | 1–3 | 1 | 0.1 |

## キャラクターのスプライト設定（プレイヤー・敵共通）

### Prefab 構成（VisualRoot パターン）

プレイヤー/敵の見た目はルートではなく子オブジェクト `VisualRoot` で管理します。物理演算（Rigidbody2D）がルートを回転させても、`CharacterVisualController` が `VisualRoot` のワールド回転を毎フレーム正立に戻すため、スプライトが転がることはありません。

```
キャラクタールート （Rigidbody2D, Collider2D, CharacterVisualController）
└── VisualRoot  （SpriteRenderer を配置する）
```

`CharacterVisualController` のフィールド:

| フィールド | 説明 |
|-----------|------|
| `visualRoot` | SpriteRenderer を持つ子 Transform（`VisualRoot`）を割り当てる |
| `flipSpeedThreshold` | フリップ判定の最低速度（停止中に向きが揺れるのを防ぐ） |
| `defaultFacingRight` | スプライトのデフォルト向き（右向きなら `true`） |

### スプライト差し替え手順（敵 Prefab）

1. Unity で `BasicEnemy.prefab` などを開く
2. 子オブジェクト `VisualRoot` を選択し、`SpriteRenderer.sprite` に画像をドラッグ
3. 当たり判定（`BoxCollider2D`）はルートにあるため **スプライト変更とは独立**。Inspector で手動調整できる

### スプライト差し替え手順（プレイヤー、シーンオブジェクト）

シーン内のプレイヤーには `VisualRoot` 子オブジェクトが未作成の場合は手動で設定する:

1. プレイヤー GameObject の直下に空の子 `VisualRoot` を作成
2. プレイヤーの `SpriteRenderer` を `VisualRoot` に移動（ドラッグ・アンド・ドロップ）
3. プレイヤールートに `CharacterVisualController` を追加し、`visualRoot` に `VisualRoot` を設定

### 当たり判定と見た目のずれ

- `BoxCollider2D` はルートに残し、スプライトとは別に手動サイズ調整する
- スプライトの見た目に完全一致させる必要はない（適度な余裕がゲームプレイを自然にする）
- スプライトが Collider より大きい/小さい場合は Collider の `Size` / `Offset` で調整する

## 新しい敵種の追加（コード変更不要）

1. **Prefab を作成** — 既存 Enemy Prefab を複製するか新規作成
   - 必須コンポーネント: `EnemyGroundMovement` / `EnemyJumpingGroundMovement` / `EnemyFlyingMovement` など、`EnemyMovement` 実装のいずれか
   - 必須コンポーネント: `EnemyShooterAttack` など、`EnemyAttack` 実装のいずれか
   - Runtime wiring: `EnemyController`, `EnemySensor`, `Health`, `TeamAffiliation` は Factory/Controller が不足時に追加する
   - 任意コンポーネント: `CombatDamageLog`
   - **見た目**: ルート直下に `VisualRoot` 子を作り `SpriteRenderer` を置く。ルートに `CharacterVisualController` を追加して `visualRoot` を設定する
     - `EnemyShooterAttack` を使う場合は **Weapon Data** フィールドに `WeaponData` アセットをセットする
2. **EnemyData を作成** — `Assets/Scripts/Enemy/EnemyDatas/` を右クリック → `Create → Combat → Enemy Data`
   - 名前は `<種別>EnemyData.asset`（例: `FlyingEnemyData.asset`）
   - `prefab` に上で作った Prefab をセット
   - 陣営・HP・速度・射程・巡回・`scoreValue` などの共通値を設定（武器やジャンプ設定などの固有値は Prefab の movement/attack コンポーネントで設定）

3. **EnemyFactory を確認** — シーンの `EnemyFactory` の `debugEnemyData` に新しい EnemyData をセットするとデバッグスポーンで確認できます

## HUD / スコアのシーン配置

| コンポーネント | 配置先 | 備考 |
|--------------|--------|------|
| `Magazine` | プレイヤー GameObject | `PlayerShooter` と同一オブジェクト |
| `AmmoHudView` | Canvas 等 | `Magazine` を参照（未設定なら自動検索） |
| `ScoreManager` | シーンに 1 つ | 複数配置すると加算が重複する |
| `ScoreHudView` | Canvas 等 | `ScoreManager` を参照（未設定なら自動検索） |

## 陣営設定

| 対象 | 設定場所 | 通常の値 |
|------|----------|----------|
| 敵 | `EnemyData.teamId` | `Enemy`（= 2） |
| プレイヤー | `TeamAffiliation.teamId` | `Ally`（= 1） |

> `Player` という enum 値は存在しません。プレイヤーには `Ally` を設定してください。

## hitMask の設定

`BulletData.hitMask` には「弾が当たれるレイヤー」を設定します。弾の発射元（プレイヤー/敵）に関わらず物理的に衝突できるレイヤーを選びます。

陣営（味方/敵）の判定は `TeamAffiliation` が行うため、`hitMask` から除外する必要はありません。

**典型的な hitMask の構成:**

| 弾の種類 | hitMask に含めるレイヤー |
|---------|----------------------|
| プレイヤーの弾 | Enemy, Ground（地形） |
| 敵の弾 | Player（= Ally 扱い）, Ground（地形） |
