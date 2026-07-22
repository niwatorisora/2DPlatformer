using UnityEngine;

/// <summary>
/// Delays event binding until its target exists, supporting runtime-created gameplay objects.
/// </summary>
public abstract class EventBoundView<TTarget> : MonoBehaviour where TTarget : Component
{
    const float RetryInterval = 0.5f;

    [SerializeField]
    TTarget target;

    bool subscribed;
    float nextSearchTime;

    protected TTarget Target => target;

    protected virtual void OnEnable()
    {
        TryBind();
    }

    protected virtual void LateUpdate()
    {
        // Runtime-spawned targets can appear later; throttle scene searches to avoid per-frame work.
        TryBind();
    }

    protected virtual void OnDisable()
    {
        Unbind();
    }

    protected virtual void OnDestroy()
    {
        Unbind();
    }

    // Connect this view to events exposed by the resolved target.
    protected abstract void Subscribe(TTarget target);

    // Remove the event connections created by Subscribe.
    protected abstract void Unsubscribe(TTarget target);

    // Refresh view state immediately after a target has been connected.
    protected virtual void OnTargetBound(TTarget target) { }

    // Override when the target must be found using a relationship other than the scene-wide default.
    protected virtual TTarget FindTarget()
    {
        return FindFirstObjectByType<TTarget>();
    }

    void TryBind()
    {
        if (subscribed) return;

        if (target == null)
        {
            if (Time.unscaledTime < nextSearchTime) return;

            nextSearchTime = Time.unscaledTime + RetryInterval;
            target = FindTarget();
            if (target == null) return;
        }

        Subscribe(target);
        subscribed = true;
        OnTargetBound(target);
    }

    void Unbind()
    {
        if (!subscribed) return;

        if (target != null)
            Unsubscribe(target);

        subscribed = false;
    }
}
