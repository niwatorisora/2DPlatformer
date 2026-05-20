using UnityEngine;

/// <summary>
/// Listens to Health.OnDied and delegates to EnemyController so the state machine
/// can react to death without Health needing to know about enemy AI.
/// Follows the same subscribe/unsubscribe pattern used by DummyTarget.
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EnemyController))]
public class EnemyDeathHandler : MonoBehaviour
{
    Health health;
    EnemyController enemyController;

    void Awake()
    {
        health           = GetComponent<Health>();
        enemyController  = GetComponent<EnemyController>();
    }

    void OnEnable()
    {
        health.OnDied += HandleDeath;
    }

    void OnDisable()
    {
        health.OnDied -= HandleDeath;
    }

    void HandleDeath()
    {
        GameLog.Debug(this, $"{name} died.");
        enemyController.OnDied();
    }
}
