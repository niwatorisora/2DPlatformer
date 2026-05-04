using System;
using UnityEngine;

/// <summary>
/// Simple damage receiver for validating bullet hits in prototype scenes.
/// </summary>
public class DummyTarget : MonoBehaviour, IDamageable
{
    [SerializeField] int maxHp = 100;
    int currentHp;

    public event Action<int> OnDamaged;

    void Awake()
    {
        currentHp = maxHp;
        // Keep this logging local to the dev component so production damage code stays quiet.
        OnDamaged += amount => Debug.Log($"[DummyTarget] {name} took {amount} damage. HP: {currentHp}/{maxHp}");
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        currentHp -= amount;
        OnDamaged?.Invoke(amount);
    }

    void OnValidate() => maxHp = Mathf.Max(1, maxHp);
}
