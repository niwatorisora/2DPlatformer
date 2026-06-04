# 2D Platformer Prototype — ドキュメント索引

## 概要

Unity 6000.4.5f1 で作られた 2D プラットフォーマーのプロトタイプです。プレイヤー移動・マウス照準射撃・弾プール・ScriptableObject 駆動の弾/武器データ・陣営ベースの味方弾フィルタリング・State/Factory パターンによる敵 AI が実装されています。

主なシーン: `Assets/Scenes/SampleScene.unity`

## 技術スタック

| 項目 | 内容 |
|------|------|
| エンジン | Unity 6000.4.5f1 |
| 言語 | C# |
| レンダーパイプライン | URP 2D |
| 物理 | `Rigidbody2D`, `Collider2D`, Trigger コールバック |
| 入力 | Unity レガシー Input API |
| データ定義 | ScriptableObject |

## ドキュメント一覧

| ファイル | 内容 |
|---------|------|
| [architecture-overview.md](architecture-overview.md) | ディレクトリ構成・システム間依存関係・コアデータフロー・型の配置 |
| [shooting.md](shooting.md) | BulletData / WeaponData / ShooterCore / BulletPool / Bullet の詳細 |
| [combat.md](combat.md) | Health / IDamageable / DamageContext / TeamAffiliation / 弾命中ルール |
| [enemy-ai.md](enemy-ai.md) | EnemyData / EnemyController / State 遷移 / 移動・攻撃コンポーネント |
| [spawn-system.md](spawn-system.md) | EnemySpawnPoint / SpawnContext / EnemySpawnPattern / Runner |
| [data-assets.md](data-assets.md) | 既存アセット一覧・新規追加手順・hitMask 設定指針 |

## 実装済みシステム

- **プレイヤー**: 左右移動（`PlayerMovement`）、マウス照準射撃（`PlayerShooter`）
- **射撃共通基盤**: `ShooterCore`（spread / burst）、`BulletPool`（`ObjectPool<Bullet>`）
- **戦闘**: `Health`（HP・ダメージ・イベント）、`TeamAffiliation`（陣営フィルタ）
- **敵 AI**: State Machine（Idle / Patrol / Chase / Attack / Dead）、Factory スポーン、移動バリエーション（地上・ジャンプ・飛行）

## 今後の拡張方針

- 新しい弾種 → `BulletData` アセットを追加
- 新しい武器 → `WeaponData` アセットを追加
- 新しい敵種 → `EnemyData` アセットと Prefab を追加（陣営/HPなどの共通値は `EnemyData`、移動/攻撃の種類と固有値は Prefab component）
- 新しい攻撃手段（近接など）→ `EnemyAttack` を継承した新コンポーネント
- 弾を止めるだけのオブジェクト → `IDamageable` を付けず `hitMask` に対象レイヤーを含める
- ダメージ軽減・耐性 → `Health.CalculateDamage` を override
- 視線遮蔽（壁越し検知）→ `EnemySensor` 内部を `Physics2D.Raycast` に差し替え
- 敵のプール化 → `EnemyFactory.Create` の内部だけ差し替え
