using UnityEngine;

/// <summary>
/// Terminal state: stops movement and destroys the enemy GameObject after a short delay
/// so death animations or effects have time to play before the object is removed.
/// </summary>
public class EnemyDeadState : EnemyState
{
    const float DestroyDelay = 0.5f;

    float timer;

    public EnemyDeadState(EnemyController controller) : base(controller) { }

    public override void Enter()
    {
        controller.Movement.Stop();
        timer = 0f;
    }

    public override void Tick(float deltaTime)
    {
        timer += deltaTime;
        if (timer >= DestroyDelay)
            Object.Destroy(controller.gameObject);
    }
}
