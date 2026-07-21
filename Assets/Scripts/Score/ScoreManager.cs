using System;
using UnityEngine;

/// <summary>
/// ゲーム全体のスコアを集計する。WaveSpawner から注入された敵撃破通知を受け取り、
/// 倒された敵の獲得点を加算する。表示側(HUD)は OnScoreChanged を購読する。
/// </summary>
public class ScoreManager : MonoBehaviour, IEnemyKillListener
{
    public event Action OnScoreChanged;

    public int Score { get; private set; }

    public void OnEnemyKilled(int scoreValue) => AddScore(scoreValue);

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
