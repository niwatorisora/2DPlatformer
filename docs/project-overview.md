# 2D Platformer Prototype Project Overview

## 概要

このプロジェクトは Unity 6000.4.5f1 で作られている 2D プラットフォーマーのプロトタイプです。現時点では、プレイヤー移動、マウス方向への射撃、弾のプール管理、ScriptableObject による弾・武器データ管理、陣営ベースの味方判定が実装されています。

主なシーンは `Assets/Scenes/SampleScene.unity` です。プレイヤー、弾プール、地形タイルマップ、カメラが配置されています。

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
  - HP、ダメージ対象、陣営、戦闘共通処理を扱います。
- `Assets/Scripts/Combat/Shooting`
  - 弾、弾プール、弾データ、武器データを扱います。
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

## HP とダメージ

HP は `Health` コンポーネントで管理します。`Health` は `IDamageable` を実装し、現在HP、最大HP、無敵状態、被ダメージ可否、被ダメージイベント、死亡イベントを扱います。

被ダメージ時のコンソールログは、`CombatDamageLog` コンポーネントを `Health` と同じGameObjectに付けると `GameLog` 経由で出力されます（プレイヤー・敵プレハブで使用）。

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

- 敵が弾を撃つ場合も、射撃コンポーネントから `BulletPool.Shoot` に発射者と `TeamId` を渡す形に揃える。
- 新しい弾種は `BulletData` を追加して作る。
- 新しい銃種は `WeaponData` を追加し、発射数・spread・連続発射数で表現する。
- 弾を止めるだけのオブジェクトは `IDamageable` を付けず、対象レイヤーを `BulletData.hitMask` に含める。
- ダメージ軽減や耐性は、まず `Health.CalculateDamage` の差し替えや周辺コンポーネントで拡張する。
