using UnityEngine;

/// <summary>待機と巡回を担当する敵 AI 状態。</summary>
public class EnemyIdleState : EnemyState
{
    public EnemyIdleState(EnemyController controller) : base(controller) { }

    public override void Enter() => controller.Movement.Stop();

    public override void Tick(float deltaTime)
    {
        if (controller.Sensor.TryDetectTarget(out _))
        {
            controller.ChangeState(controller.ChaseState);
            return;
        }

        if (controller.Data.patrolEnabled) controller.ChangeState(controller.PatrolState);
    }
}

/// <summary>出現位置を基準に往復する巡回状態。</summary>
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
        if (relativeX >= halfDistance) direction = -1;
        else if (relativeX <= -halfDistance) direction = 1;

        Vector2 target = (Vector2)controller.transform.position + Vector2.right * direction;
        controller.Movement.MoveToward(target);
    }

    public override void Exit() => controller.Movement.Stop();
}
