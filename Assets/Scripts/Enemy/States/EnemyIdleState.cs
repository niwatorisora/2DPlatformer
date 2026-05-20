/// <summary>
/// Enemy stands still. Transitions to Patrol when patrolEnabled is true,
/// or to Chase immediately if the target is already in detection range.
/// </summary>
public class EnemyIdleState : EnemyState
{
    public EnemyIdleState(EnemyController controller) : base(controller) { }

    public override void Enter()
    {
        controller.Movement.Stop();
    }

    public override void Tick(float deltaTime)
    {
        if (controller.Sensor.TryDetectTarget(out _))
        {
            controller.ChangeState(controller.ChaseState);
            return;
        }

        if (controller.Data.patrolEnabled)
        {
            controller.ChangeState(controller.PatrolState);
        }
    }
}
