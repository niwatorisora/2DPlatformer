/// <summary>
/// Enemy stands still and fires at the target. Returns to Chase if the target
/// moves out of attack range or sight.
/// Full-auto weapons fire every cooldown; semi-auto weapons fire one burst then
/// return to Chase to reposition before the next shot.
/// </summary>
public class EnemyAttackState : EnemyState
{
    bool attackInitiated;

    public EnemyAttackState(EnemyController controller) : base(controller) { }

    public override void Enter()
    {
        controller.Movement.Stop();
        attackInitiated = false;
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

        if (controller.Attack.IsAutoFire)
        {
            controller.Attack.TryAttack(controller.Target);
        }
        else
        {
            if (!attackInitiated)
            {
                if (controller.Attack.CanAttack(controller.Target))
                {
                    controller.Attack.TryAttack(controller.Target);
                    attackInitiated = true;
                }
            }
            else if (controller.Attack.CanAttack(controller.Target))
            {
                // Burst complete and cooldown expired — reposition before next shot.
                controller.ChangeState(controller.ChaseState);
            }
        }
    }
}
