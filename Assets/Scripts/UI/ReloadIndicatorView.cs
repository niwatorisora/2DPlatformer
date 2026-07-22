using UnityEngine;
using UnityEngine.UI;

public class ReloadIndicatorView : EventBoundView<Magazine>
{
    const float BlinkInterval = 0.25f;

    [SerializeField] HudTheme theme;
    [SerializeField] Text reloadLabel;

    float nextBlinkTime;
    bool blinking;

    void Awake()
    {
        if (reloadLabel != null) reloadLabel.text = "RELOADING";
        ApplyTheme();
        StopBlinking();
    }

    public void ApplyTheme()
    {
        if (theme != null) theme.ApplyText(reloadLabel, theme.BoneCream());
    }

    protected override void Subscribe(Magazine magazine)
    {
        magazine.OnReloadStarted += StartBlinking;
        magazine.OnReloadCompleted += StopBlinking;
        magazine.OnReloadCancelled += StopBlinking;
        magazine.OnAmmoChanged += HandleAmmoChanged;
    }

    protected override void Unsubscribe(Magazine magazine)
    {
        magazine.OnReloadStarted -= StartBlinking;
        magazine.OnReloadCompleted -= StopBlinking;
        magazine.OnReloadCancelled -= StopBlinking;
        magazine.OnAmmoChanged -= HandleAmmoChanged;
    }

    protected override void OnTargetBound(Magazine magazine)
    {
        ApplyTheme();
        if (magazine.IsReloading) StartBlinking();
        else StopBlinking();
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (!blinking || Time.unscaledTime < nextBlinkTime) return;
        if (reloadLabel != null) reloadLabel.enabled = !reloadLabel.enabled;
        nextBlinkTime += BlinkInterval;
    }

    void StartBlinking()
    {
        blinking = true;
        if (reloadLabel != null) reloadLabel.enabled = true;
        nextBlinkTime = Time.unscaledTime + BlinkInterval;
    }

    void StopBlinking()
    {
        blinking = false;
        if (reloadLabel != null) reloadLabel.enabled = false;
    }

    void HandleAmmoChanged()
    {
        if (Target != null && !Target.IsReloading) StopBlinking();
    }
}
