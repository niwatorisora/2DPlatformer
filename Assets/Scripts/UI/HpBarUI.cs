using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a world-space HP bar from the Health component found in the parent hierarchy.
/// Mirrors CombatDamageLog's Awake/OnEnable subscription pattern so it behaves the same
/// for factory-spawned and manually placed actors (and survives enable/disable + pooling).
/// </summary>
[RequireComponent(typeof(Canvas))]
public class HpBarUI : MonoBehaviour
{
    [SerializeField] Image fill;

    Health health;

    void Awake()
    {
        health = GetComponentInParent<Health>();

        // フォールバック: Inspectorで未設定でも子の"Fill"から自動取得
        if (fill == null)
            fill = transform.Find("Fill")?.GetComponent<Image>();
    }

    void OnEnable()
    {
        if (health == null)
        {
            Debug.LogError("[HpBarUI] Health not found in parent hierarchy.", this);
            return;
        }
        if (fill == null)
        {
            Debug.LogError("[HpBarUI] Fill Image not found. Assign it in the Inspector or name the child 'Fill'.", this);
            return;
        }

        health.OnDamaged += OnDamaged;
        Refresh();
    }

    void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= OnDamaged;
    }

    void OnDamaged(int _) => Refresh();

    void Refresh()
    {
        fill.fillAmount = (float)health.CurrentHp / health.MaxHp;
    }
}
