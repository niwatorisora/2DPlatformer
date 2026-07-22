using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHpHudView : EventBoundView<Health>
{
    const int MaxSegments = 10;
    static readonly Vector2 SegmentSize = new(32f, 32f);

    [SerializeField] HudTheme theme;
    [SerializeField] RectTransform segmentContainer;

    readonly List<Image> segmentPool = new();
    int activeSegmentCount;

    protected override Health FindTarget()
    {
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        return player != null ? player.GetComponent<Health>() : null;
    }

    protected override void Subscribe(Health health) => health.OnDamaged += OnDamaged;
    protected override void Unsubscribe(Health health) => health.OnDamaged -= OnDamaged;

    protected override void OnTargetBound(Health health)
    {
        activeSegmentCount = Mathf.Min(MaxSegments, Mathf.Max(1, health.MaxHp));
        EnsurePool(activeSegmentCount);
        Refresh();
    }

    void OnDamaged(int _) => Refresh();

    void EnsurePool(int count)
    {
        RectTransform root = segmentContainer != null ? segmentContainer : (RectTransform)transform;
        while (segmentPool.Count < count)
        {
            GameObject item = new($"HpSegment{segmentPool.Count + 1}",
                typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            item.transform.SetParent(root, false);
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.sizeDelta = SegmentSize;
            LayoutElement layout = item.GetComponent<LayoutElement>();
            layout.preferredWidth = SegmentSize.x;
            layout.preferredHeight = SegmentSize.y;
            segmentPool.Add(item.GetComponent<Image>());
        }

        for (int i = 0; i < segmentPool.Count; i++)
            segmentPool[i].gameObject.SetActive(i < count);
    }

    void Refresh()
    {
        if (Target == null || theme == null) return;
        float ratio = (float)Target.CurrentHp / Mathf.Max(1, Target.MaxHp);
        int fullCount = Mathf.CeilToInt(ratio * activeSegmentCount);
        for (int i = 0; i < activeSegmentCount; i++)
        {
            bool full = i < fullCount;
            theme.ApplyImage(segmentPool[i], theme.HpSegment(full),
                full ? theme.Gold() : theme.PanelDark());
        }
    }
}
