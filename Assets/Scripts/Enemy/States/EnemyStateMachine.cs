using UnityEngine;

/// <summary>
/// Base class for all enemy AI states. States are plain C# objects (not MonoBehaviours)
/// so they add no GameObject overhead and are created inside EnemyController.Initialize.
/// Each state receives the owning controller and calls back into it for Movement, Attack,
/// Sensor, and Data access.
/// </summary>
public abstract class EnemyState
{
    protected readonly EnemyController controller;

    protected EnemyState(EnemyController controller)
    {
        this.controller = controller;
    }

    public virtual void Enter() { }
    public virtual void Tick(float deltaTime) { }
    public virtual void Exit() { }
}

/// <summary>
/// Minimal state machine that drives enemy AI by delegating each frame to the current state.
/// Transitions are requested by states themselves via EnemyController.ChangeState.
/// </summary>
public class EnemyStateMachine
{
    public EnemyState Current { get; private set; }

    public void ChangeState(EnemyState next)
    {
        Current?.Exit();
        Current = next;
        Current?.Enter();
    }

    public void Tick(float deltaTime)
    {
        Current?.Tick(deltaTime);
    }
}

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

        if (relativeX >= halfDistance) direction = -1;
        else if (relativeX <= -halfDistance) direction = 1;

        Vector2 target = (Vector2)controller.transform.position + Vector2.right * direction;
        controller.Movement.MoveToward(target);
    }

    public override void Exit()
    {
        controller.Movement.Stop();
    }
}

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
            return;
        }

        if (!attackInitiated)
        {
            if (controller.Attack.CanAttack(controller.Target))
            {
                controller.Attack.TryAttack(controller.Target);
                attackInitiated = true;
            }

            return;
        }

        if (controller.Attack.CanAttack(controller.Target))
        {
            // Burst complete and cooldown expired; reposition before the next shot.
            controller.ChangeState(controller.ChaseState);
        }
    }
}

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
        {
            Object.Destroy(controller.gameObject);
        }
    }
}
