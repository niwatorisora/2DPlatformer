/// <summary>
/// Enemy stands still and fires at the target. Returns to Chase if the target
/// moves out of attack range.
/// </summary>
public class EnemyAttackState : EnemyState
{
    public EnemyAttackState(EnemyController controller) : base(controller) { }

    public override void Enter()
    {
        controller.Movement.Stop();
    }

    public override void Tick(float deltaTime)
    {
        if (controller.Target == null || controller.Sensor.HasLostSight(controller.Target))
        {
            controller.ChangeState(controller.Data.patrolEnabled
                ? (EnemyState)controller.PatrolState
                : controller.IdleState);
            return;
        }

        if (!controller.Sensor.IsInAttackRange(controller.Target, controller.Data.attackRange))
        {
            controller.ChangeState(controller.ChaseState);
            return;
        }

        controller.Attack.TryAttack(controller.Target);
    }
}
