using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 現在スコアを表示するスクリーン空間HUDウィジェット。ScoreManager にバインドし、
/// OnScoreChanged で更新する。依存は一方向（このビューがロジックを参照する）。
/// </summary>
public class ScoreHudView : EventBoundView<ScoreManager>
{
    [SerializeField] Text scoreLabel;            // "SCORE 1200"
    [SerializeField] string prefix = "SCORE ";

    protected override void Subscribe(ScoreManager scoreManager)
    {
        scoreManager.OnScoreChanged += Refresh;
    }

    protected override void Unsubscribe(ScoreManager scoreManager)
    {
        scoreManager.OnScoreChanged -= Refresh;
    }

    protected override void OnTargetBound(ScoreManager scoreManager)
    {
        Refresh();
    }

    void Refresh()
    {
        if (Target == null || scoreLabel == null) return;
        scoreLabel.text = $"{prefix}{Target.Score}";
    }
}
