using System;
using UnityEngine;

/// <summary>
/// ゲーム全体のスコアを集計する。EnemyController.OnEnemyKilled（静的イベント）を
/// 購読し、倒された敵の獲得点を加算する。表示側(HUD)は OnScoreChanged を購読する。
/// シーンに1つだけ配置すること（複数あると多重加算になる）。
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public event Action OnScoreChanged;

    public int Score { get; private set; }

    void OnEnable()
    {
        EnemyController.OnEnemyKilled += AddScore;
    }

    void OnDisable()
    {
        EnemyController.OnEnemyKilled -= AddScore;
    }

    /// <summary>スコアを加算する。0以下は無視。</summary>
    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        Score += amount;
        OnScoreChanged?.Invoke();
    }

    /// <summary>スコアを0に戻す（リスタート等で使用）。</summary>
    public void ResetScore()
    {
        Score = 0;
        OnScoreChanged?.Invoke();
    }
}