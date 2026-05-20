using UnityEngine;

/// <summary>
/// Enemy walks back and forth within patrolDistance from the spawn position.
/// Transitions to Chase as soon as the target enters detection range.
/// </summary>
public class EnemyPatrolState : EnemyState
{
    Vector2 originPosition;
    int direction = 1;

    public EnemyPatrolState(EnemyController controller) : base(controller) { }

    public override void Enter()
    {
        originPosition = controller.transform.position;
        direction = 1;
    }

    public override void Tick(float deltaTime)
    {
        if (controller.Sensor.TryDetectTarget(out _))
        {
            controller.ChangeState(controller.ChaseState);
            return;
        }

        float halfDistance = controller.Data.patrolDistance * 0.5f;
        float relativeX = controller.transform.position.x - originPosition.x;

        if (relativeX >= halfDistance)
            direction = -1;
        else if (relativeX <= -halfDistance)
            direction = 1;

        Vector2 target = (Vector2)controller.transform.position + Vector2.right * direction;
        controller.Movement.MoveToward(target);
    }

    public override void Exit()
    {
        controller.Movement.Stop();
    }
}
