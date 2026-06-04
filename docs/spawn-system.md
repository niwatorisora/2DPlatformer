# 敵出現パターンシステム

Factory パターンの上に重ねる再利用可能なスポーン実行層。WaveSystem は将来作られ、この Spawner は外部から何度も呼ばれる「パターン実行器」として設計される。

## 型の配置

| 型 | 種別 | ファイル |
|----|------|---------|
| `EnemySpawnPoint` | MonoBehaviour | `EnemySpawnPoint.cs` |
| `SpawnContext` | struct | `SpawnContext.cs` |
| `EnemyCandidate` | struct | `EnemySpawnEntry.cs` |
| `EnemySpawnEntry` | class (Serializable) | `EnemySpawnEntry.cs` |
| `EnemySpawnPattern` | ScriptableObject | `EnemySpawnPattern.cs` |
| `EnemySpawnPatternSet` | ScriptableObject | `EnemySpawnPatternSet.cs` |
| `EnemySpawnPatternRunner` | MonoBehaviour | `EnemySpawnPatternRunner.cs` |

## データモデル

### EnemySpawnPoint

シーン上に配置する出現地点。`groupId` でグループ化し、Gizmo で範囲を表示する。

| フィールド | 説明 |
|-----------|------|
| `groupId` | グループ識別子。Entry がこの値で地点を参照する |
| `radius` | Gizmo 表示と塞がり判定の半径 |
| `blockageMask` | 塞がり判定に使う LayerMask |

`IsBlocked()` は `Physics2D.OverlapCircle` で半径内にコライダーがあるか判定する。

### SpawnContext

Play() 実行時の入力。

| フィールド | 説明 |
|-----------|------|
| `waveIndex` | ウェーブ/ラウンド識別子。ログと将来の条件分岐用 |
| `difficulty` | 難易度倍率（1.0f = ベースライン） |
| `useSeed` | true のとき `seed` を使ってランダム結果を再現 |
| `seed` | `useSeed` が true のとき使う整数シード |

### EnemySpawnEntry

1 つの出現イベント。

| フィールド | 説明 |
|-----------|------|
| `delay` | 出現を待機する秒数 |
| `groupIdList` | 参照する SpawnPoint グループ ID 一覧 |
| `pointCount` | 選択する SpawnPoint 数。-1 = 全地点 |
| `baseCount` | 1地点あたりの基本出現数 |
| `difficultyBonusPerLevel` | 難易度による追加数 |
| `countMultiplierCurve` | 難易度から出現数倍率を計算する AnimationCurve（設定時は上のボーナス無視） |
| `candidates` | 重み付き敵候補リスト |

出現数の計算:
- `countMultiplierCurve` が設定されていれば `baseCount * curve.Evaluate(difficulty)`
- そうでない場合 `baseCount + difficultyBonusPerLevel * difficulty`

### EnemySpawnPattern

時間差付きの Entry 列。`Create → Combat → Enemy Spawn Pattern` で作成。

| フィールド | 説明 |
|-----------|------|
| `weight` | PatternSet からの選択重み |
| `entries` | スポーンエントリ列 |

### EnemySpawnPatternSet

複数の Pattern 候補を保持。`Create → Combat → Enemy Spawn Pattern Set` で作成。

| フィールド | 説明 |
|-----------|------|
| `patterns` | スポーンパターン一覧 |

## 実行（EnemySpawnPatternRunner）

シーン上の GameObject にアタッチし、外部から `Play(patternSet, context)` を呼ぶ。

### API

```csharp
// 指定した PatternSet と Context で実行（Coroutine として返す）
IEnumerator Play(EnemySpawnPatternSet set, SpawnContext context);

// Inspector で設定した PatternSet を使うオーバーロード
IEnumerator Play(SpawnContext context);

// 未実行分のスポーンを停止。既に出した敵は残す。
void Stop();
```

### イベント

```csharp
// 全エントリが通常完了したときに発生
event Action<SpawnContext> OnCompleted;

// Stop() で中断されたときに発生
event Action<SpawnContext> OnStopped;
```

### 実行フロー

1. `Play(set, context)` が呼ばれる
2. 既に実行中の場合は拒否（Warning ログ）
3. 重み付きランダムで Pattern を選択
4. 各 Entry について:
   a. `delay` 秒待機
   b. `groupIdList` に一致する SpawnPoint を収集
   c. `IsBlocked()` で塞がった地点を除外
   d. `pointCount` だけランダムに選択（重複回避）
   e. 各地点で `CalculateCount(difficulty)` 分の敵を出現
   f. 敵は `EnemyCandidate` の重み付きランダムで選択
5. 全 Entry 完了 → `OnCompleted` 発生
6. `Stop()` 中断時は `OnCompleted` ではなく `OnStopped` 発生

### 制約

- 同じ Runner の多重実行は拒否
- `Stop()` は未実行分だけを止め、既に出した敵は消さない
- `Stop()` 中断時は通常完了扱いにしない
- 完了条件は「指定分を出し終えた」だけで、全滅待ちは WaveSystem の責務
- ContextMenu から `Test Play` / `Stop` で Inspector 上でテスト可能

## 使用例

```csharp
public class WaveManager : MonoBehaviour
{
    [SerializeField] EnemySpawnPatternRunner runner;
    [SerializeField] EnemySpawnPatternSet patternSet;

    void StartWave(int waveIndex, float difficulty)
    {
        var context = new SpawnContext(waveIndex, difficulty);
        StartCoroutine(runner.Play(patternSet, context));
    }
}
```

## 拡張方針

- **JSON 入出力**: 初期実装には含まない。必要になったら SO へのインポート機能として追加
- **WaveSystem**: 敵の生存数追跡やウェーブ終了判定は将来の別システム
- **SpawnPoint 管理**: 現在は `FindObjectsOfType` で全地点を収集。将来的に Runner が参照を持つ形に変更可能
