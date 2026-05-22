using UnityEngine;

/// <summary>
/// Logs damage and death events from the sibling Health component to the Unity Console.
/// Attach to any actor (player, enemy, ally) that needs hit feedback during development.
/// All output goes through GameLog so it follows the standard [Level:ClassName] prefix.
/// </summary>
[RequireComponent(typeof(Health))]
public class CombatDamageLog : MonoBehaviour
{
    Health health;

    void Awake()
    {
        health = GetComponent<Health>();
    }

    void OnEnable()
    {
        health.OnDamaged += HandleDamaged;
        health.OnDied    += HandleDied;
    }

    void OnDisable()
    {
        health.OnDamaged -= HandleDamaged;
        health.OnDied    -= HandleDied;
    }

    void HandleDamaged(int amount)
        => GameLog.Debug(this, $"{name} took {amount} damage. HP: {health.CurrentHp}/{health.MaxHp}");

    void HandleDied()
        => GameLog.Debug(this, $"{name} died.");
}
