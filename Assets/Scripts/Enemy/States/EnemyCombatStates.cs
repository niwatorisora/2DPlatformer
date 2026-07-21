using UnityEngine;

/// <summary>標的を追跡し、攻撃可能距離へ移動する状態。</summary>
public class EnemyChaseState : EnemyState
{
    public EnemyChaseState(EnemyController controller) : base(controller) { }

    public override void Tick(float deltaTime)
    {
        if (controller.Target == null || controller.Sensor.HasLostSight(controller.Target))
        {
            controller.ChangeState(controller.Data.patrolEnabled
                ? (EnemyState)controller.PatrolState : controller.IdleState);
            return;
        }

        if (controller.Sensor.IsInAttackRange(controller.Target, controller.Data.attackRange))
        {
            controller.ChangeState(controller.AttackState);
            return;
        }

        controller.Movement.MoveToward(controller.Target.position);
    }

    public override void Exit() => controller.Movement.Stop();
}

/// <summary>標的が射程内にいる間、武器設定に従って攻撃する状態。</summary>
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
                ? (EnemyState)controller.PatrolState : controller.IdleState);
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

        if (controller.Attack.CanAttack(controller.Target)) controller.ChangeState(controller.ChaseState);
    }
}

/// <summary>死亡演出の待機後に敵をプールへ返却する状態。</summary>
public class EnemyDeadState : EnemyState
{
    const float DespawnDelay = 0.5f;
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
        if (timer >= DespawnDelay) controller.Despawn();
    }
}
