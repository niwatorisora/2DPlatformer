/// <summary>
/// 敵撃破を受け取る依存先。WaveSpawner から明示的に注入する。
/// </summary>
public interface IEnemyKillListener
{
    void OnEnemyKilled(int scoreValue);
}
