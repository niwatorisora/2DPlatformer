/// <summary>
/// Enemy moves toward the target. Transitions to Attack when in range,
/// or back to Idle/Patrol when the target is lost.
/// </summary>
public class EnemyChaseState : EnemyState
{
    public EnemyChaseState(EnemyController controller) : base(controller) { }

    public override void Tick(float deltaTime)
    {
        if (controller.Target == null || controller.Sensor.HasLostSight(controller.Target))
        {
            controller.ChangeState(controller.Data.patrolEnabled
                ? (EnemyState)controller.PatrolState
                : controller.IdleState);
            return;
        }

        if (controller.Sensor.IsInAttackRange(controller.Target, controller.Data.attackRange))
        {
            controller.ChangeState(controller.AttackState);
            return;
        }

        controller.Movement.MoveToward(controller.Target.position);
    }

    public override void Exit()
    {
        controller.Movement.Stop();
    }
}
