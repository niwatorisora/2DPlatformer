using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a world-space HP bar from the Health component found in the parent hierarchy.
/// Factory-spawned enemies may receive Health after child OnEnable runs, so binding is retried.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class HpBarUI : EventBoundView<Health>
{
    [SerializeField] Image fill;

    void Awake()
    {
        if (fill != null) return;

        fill = transform.Find("Fill")?.GetComponent<Image>();
        if (fill != null) return;

        Debug.LogError("[HpBarUI] Fill Image not found. Assign it in the Inspector or name the child 'Fill'.", this);
        enabled = false;
    }

    void OnDamaged(int _) => Refresh();

    protected override Health FindTarget()
    {
        return GetComponentInParent<Health>();
    }

    protected override void Subscribe(Health health)
    {
        health.OnDamaged += OnDamaged;
    }

    protected override void Unsubscribe(Health health)
    {
        health.OnDamaged -= OnDamaged;
    }

    protected override void OnTargetBound(Health health)
    {
        Refresh();
    }

    void Refresh()
    {
        if (Target == null || fill == null) return;

        fill.fillAmount = (float)Target.CurrentHp / Target.MaxHp;
    }
}
