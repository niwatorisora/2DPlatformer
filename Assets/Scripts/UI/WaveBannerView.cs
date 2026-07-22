using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class WaveBannerView : EventBoundView<WaveSpawner>
{
    const float SnapDuration = 0.1f;
    const float HoldDuration = 1.2f;

    [SerializeField] HudTheme theme;
    [SerializeField] Image panel;
    [SerializeField] Text waveLabel;

    Coroutine displayRoutine;

    void Awake()
    {
        ApplyTheme();
        SetVisible(false);
    }

    public void ApplyTheme()
    {
        if (theme == null) return;
        theme.ApplyImage(panel, theme.PanelFrame(), theme.PanelDark());
        theme.ApplyText(waveLabel, theme.Gold());
        theme.ApplyOutline(waveLabel);
    }

    protected override void Subscribe(WaveSpawner spawner) => spawner.OnWaveStarted += Show;
    protected override void Unsubscribe(WaveSpawner spawner) => spawner.OnWaveStarted -= Show;

    void Show(int waveIndex)
    {
        if (waveLabel != null) waveLabel.text = $"WAVE {waveIndex + 1}";
        if (displayRoutine != null) StopCoroutine(displayRoutine);
        displayRoutine = StartCoroutine(DisplayRoutine());
    }

    IEnumerator DisplayRoutine()
    {
        SetVisible(true);
        transform.localScale = Vector3.one * 1.3f;
        yield return new WaitForSecondsRealtime(SnapDuration);
        transform.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(HoldDuration);
        SetVisible(false);
        displayRoutine = null;
    }

    void SetVisible(bool visible)
    {
        if (panel != null) panel.enabled = visible;
        if (waveLabel != null) waveLabel.enabled = visible;
    }

    protected override void OnDisable()
    {
        if (displayRoutine != null) StopCoroutine(displayRoutine);
        displayRoutine = null;
        transform.localScale = Vector3.one;
        base.OnDisable();
    }
}
