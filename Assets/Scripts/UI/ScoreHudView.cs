using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 現在スコアを表示するスクリーン空間HUDウィジェット。ScoreManager にバインドし、
/// OnScoreChanged で更新する。依存は一方向（このビューがロジックを参照する）。
/// </summary>
public class ScoreHudView : MonoBehaviour
{
    [SerializeField] ScoreManager scoreManager;  // 未設定ならシーンから自動取得
    [SerializeField] Text scoreLabel;            // "SCORE 1200"
    [SerializeField] string prefix = "SCORE ";

    bool subscribed;

    void OnEnable() => TrySubscribe();
    void Start() => TrySubscribe();

    void LateUpdate()
    {
        if (!subscribed) TrySubscribe();
    }

    void OnDisable()
    {
        if (!subscribed || scoreManager == null) return;
        scoreManager.OnScoreChanged -= Refresh;
        subscribed = false;
    }

    void TrySubscribe()
    {
        if (subscribed) return;
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager == null) return;

        scoreManager.OnScoreChanged += Refresh;
        subscribed = true;
        Refresh();
    }

    void Refresh()
    {
        if (scoreManager == null || scoreLabel == null) return;
        scoreLabel.text = $"{prefix}{scoreManager.Score}";
    }
}