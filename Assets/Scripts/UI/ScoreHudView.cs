using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 現在スコアを表示するスクリーン空間HUDウィジェット。ScoreManager にバインドし、
/// OnScoreChanged で更新する。依存は一方向（このビューがロジックを参照する）。
/// </summary>
public class ScoreHudView : EventBoundView<ScoreManager>
{
    [SerializeField] HudTheme theme;
    [SerializeField] Image panel;
    [SerializeField] Text scoreLabel;            // "SCORE 1200"
    [SerializeField] string prefix = "SCORE ";
    [SerializeField] HudPunch punch;

    bool hasValue;
    int previousScore;

    void Awake() => ApplyTheme();

    public void ApplyTheme()
    {
        if (theme == null) return;
        theme.ApplyImage(panel, theme.PanelFrame(), theme.PanelDark());
        theme.ApplyText(scoreLabel, theme.BoneCream());
    }

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
        ApplyTheme();
        Refresh();
    }

    void Refresh()
    {
        if (Target == null || scoreLabel == null) return;
        bool changed = hasValue && previousScore != Target.Score;
        previousScore = Target.Score;
        hasValue = true;
        scoreLabel.text = $"{prefix}{Target.Score}";
        if (changed) punch?.Punch();
    }
}
