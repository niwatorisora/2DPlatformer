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
