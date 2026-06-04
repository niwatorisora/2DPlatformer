/// <summary>
/// Execution-time input passed to EnemySpawnPatternRunner.Play().
/// Carries wave index, difficulty scaling, and optional deterministic random seed.
/// </summary>
public struct SpawnContext
{
    /// <summary>Wave or round identifier. Used for logging and future branching.</summary>
    public int waveIndex;

    /// <summary>Difficulty value used by EnemySpawnEntry to scale spawn counts.</summary>
    public float difficulty;

    /// <summary>When true, seed is used for reproducible pattern, point, and enemy selection.</summary>
    public bool useSeed;

    /// <summary>Deterministic random seed used only when useSeed is true.</summary>
    public int seed;

    public SpawnContext(int waveIndex, float difficulty)
    {
        this.waveIndex = waveIndex;
        this.difficulty = difficulty;
        useSeed = false;
        seed = 0;
    }

    public SpawnContext(int waveIndex, float difficulty, int seed)
    {
        this.waveIndex = waveIndex;
        this.difficulty = difficulty;
        this.seed = seed;
        useSeed = true;
    }
}
