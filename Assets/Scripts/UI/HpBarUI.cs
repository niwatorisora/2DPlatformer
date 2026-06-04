using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a world-space HP bar from the Health component found in the parent hierarchy.
/// Factory-spawned enemies may receive Health after child OnEnable runs, so binding is retried.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class HpBarUI : MonoBehaviour
{
    [SerializeField] Image fill;

    Health health;
    bool subscribed;
    bool loggedMissingFill;

    void Awake()
    {
        CacheFill();
        TryFindHealth();
    }

    void OnEnable()
    {
        TrySubscribe(false);
    }

    void Start()
    {
        TrySubscribe(true);
    }

    void LateUpdate()
    {
        if (!subscribed)
            TrySubscribe(false);
    }

    void OnDisable()
    {
        if (!subscribed || health == null) return;

        health.OnDamaged -= OnDamaged;
        subscribed = false;
    }

    void OnDamaged(int _) => Refresh();

    bool TrySubscribe(bool logMissingHealth)
    {
        if (subscribed) return true;

        CacheFill();
        TryFindHealth();

        if (health == null)
        {
            if (logMissingHealth)
                Debug.LogError("[HpBarUI] Health not found in parent hierarchy.", this);
            return false;
        }

        if (fill == null)
        {
            if (!loggedMissingFill)
            {
                Debug.LogError("[HpBarUI] Fill Image not found. Assign it in the Inspector or name the child 'Fill'.", this);
                loggedMissingFill = true;
            }
            return false;
        }

        health.OnDamaged += OnDamaged;
        subscribed = true;
        Refresh();
        return true;
    }

    void TryFindHealth()
    {
        if (health == null)
            health = GetComponentInParent<Health>();
    }

    void CacheFill()
    {
        if (fill == null)
            fill = transform.Find("Fill")?.GetComponent<Image>();
    }

    void Refresh()
    {
        if (health == null || fill == null) return;

        fill.fillAmount = (float)health.CurrentHp / health.MaxHp;
    }
}
