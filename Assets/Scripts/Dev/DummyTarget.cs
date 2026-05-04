using UnityEngine;

/// <summary>
/// Prototype-only damage log for validating bullet hits in scenes.
/// </summary>
[RequireComponent(typeof(Health))]
public class DummyTarget : MonoBehaviour
{
    Health health;

    void Awake()
    {
        health = GetComponent<Health>();
        // Keep this logging local to the dev component so production damage code stays quiet.
        health.OnDamaged += LogDamage;
    }

    void OnDestroy()
    {
        if (health != null)
            health.OnDamaged -= LogDamage;
    }

    void LogDamage(int amount)
        => GameLog.Debug(this, $"{name} took {amount} damage. HP: {health.CurrentHp}/{health.MaxHp}");
}
