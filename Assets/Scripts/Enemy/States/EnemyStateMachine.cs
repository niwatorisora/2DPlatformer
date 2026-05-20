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
