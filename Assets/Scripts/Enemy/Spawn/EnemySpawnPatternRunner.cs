using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Execution-time input passed to EnemySpawnPatternRunner.Play().
/// Carries wave index, difficulty scaling, and optional deterministic random seed.
/// </summary>
public struct SpawnContext
{
    /// <summary>Wave round identifier.</summary>
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

/// <summary>
/// Executes spawn patterns selected from a pattern set.
/// The runner does not track spawned enemy survival; that belongs to the WaveSystem.
/// </summary>
public class EnemySpawnPatternRunner : MonoBehaviour
{
    [Tooltip("The pattern set to pick patterns from.")]
    [SerializeField] EnemySpawnPatternSet patternSet;
    public EnemySpawnPatternSet PatternSet => patternSet;

    [Tooltip("EnemyFactory used to create spawned enemies.")]
    [SerializeField] EnemyFactory enemyFactory;
    public EnemyFactory EnemyFactory => enemyFactory;

    [Header("Debug Playback")]
    [Tooltip("Play the assigned PatternSet when the scene starts. Wave systems can leave this off and call Play().")]
    [SerializeField] bool playOnStart;

    [SerializeField] int debugWaveIndex;

    [SerializeField] float debugDifficulty = 1f;

    [SerializeField] bool debugUseSeed;

    [SerializeField] int debugSeed;

    bool isRunning;
    bool isStopped;
    bool stopNotified;
    Coroutine currentRoutine;
    SpawnContext currentContext;
    int runVersion;

    /// <summary>
    /// Fired only when all entries of the selected pattern have spawned.
    /// Does not wait for spawned enemies to die.
    /// </summary>
    public event Action<SpawnContext> OnCompleted;

    /// <summary>
    /// Fired when Stop() cancels an in-progress execution.
    /// Already spawned enemies remain alive.
    /// </summary>
    public event Action<SpawnContext> OnStopped;

    public bool IsRunning => isRunning;

    void Start()
    {
        if (!playOnStart)
        {
            GameLog.Debug(this, "Play On Start is off. Call Play() from a wave system, or enable Play On Start for scene testing.");
            return;
        }

        if (!CanPlayAssignedSet("Play On Start"))
        {
            return;
        }

        currentRoutine = StartCoroutine(Play(CreateDebugContext()));
    }

    /// <summary>
    /// Plays a weighted pattern from the given set. Duplicate calls are rejected.
    /// </summary>
    public IEnumerator Play(EnemySpawnPatternSet set, SpawnContext context)
    {
        if (isRunning)
        {
            GameLog.Warning(this, "Already running. Ignoring Play() call.");
            yield break;
        }

        if (set == null || set.patterns == null || set.patterns.Count == 0)
        {
            GameLog.Error(this, "PatternSet is null or has no patterns.");
            yield break;
        }

        if (enemyFactory == null)
        {
            GameLog.Error(this, "EnemyFactory is not assigned.");
            yield break;
        }

        isRunning = true;
        isStopped = false;
        stopNotified = false;
        currentContext = context;
        int version = ++runVersion;

        System.Random rng = CreateRandom(context);
        EnemySpawnPattern pattern = SelectPattern(set, rng);
        if (pattern == null)
        {
            GameLog.Error(this, "Failed to select a pattern.");
            ResetRunState(version);
            yield break;
        }

        if (pattern.entries == null || pattern.entries.Count == 0)
        {
            GameLog.Warning(this, $"Pattern \"{pattern.name}\" has no entries.");
            ResetRunState(version);
            yield break;
        }

        GameLog.Debug(this, $"Playing pattern \"{pattern.name}\" with wave={context.waveIndex}, difficulty={context.difficulty}.");

        yield return ExecutePattern(pattern, context, rng, version);

        if (version != runVersion)
            yield break;

        bool completedNormally = !isStopped;
        ResetRunState(version);

        if (completedNormally)
            OnCompleted?.Invoke(context);
        else
            NotifyStopped(context);
    }

    /// <summary>
    /// Convenience overload using the Inspector-assigned PatternSet.
    /// </summary>
    public IEnumerator Play(SpawnContext context)
    {
        return Play(patternSet, context);
    }

    /// <summary>
    /// Stops the current pattern execution. Unspawned entries are skipped.
    /// Already spawned enemies remain alive.
    /// </summary>
    public void Stop()
    {
        if (!isRunning) return;

        isStopped = true;
        runVersion++;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        ResetRunState();
        NotifyStopped(currentContext);
        GameLog.Debug(this, "Pattern execution stopped.");
    }

    IEnumerator ExecutePattern(EnemySpawnPattern pattern, SpawnContext context, System.Random rng, int version)
    {
        Dictionary<string, List<EnemySpawnPoint>> spawnPointLookup = BuildSpawnPointLookup();

        for (int i = 0; i < pattern.entries.Count; i++)
        {
            if (IsExecutionCancelled(version)) yield break;

            EnemySpawnEntry entry = pattern.entries[i];
            if (entry == null)
            {
                GameLog.Warning(this, $"Pattern \"{pattern.name}\" has a null entry at index {i}. Skipping.");
                continue;
            }

            if (entry.delay > 0f)
            {
                float remaining = entry.delay;
                while (remaining > 0f && !IsExecutionCancelled(version))
                {
                    float wait = Mathf.Min(0.1f, remaining);
                    yield return new WaitForSeconds(wait);
                    remaining -= wait;
                }
            }

            if (IsExecutionCancelled(version)) yield break;
            yield return SpawnEntry(entry, context, rng, spawnPointLookup, version);
        }
    }

    IEnumerator SpawnEntry(
        EnemySpawnEntry entry,
        SpawnContext context,
        System.Random rng,
        Dictionary<string, List<EnemySpawnPoint>> lookup,
        int version)
    {
        if (entry.candidates == null || entry.candidates.Count == 0)
        {
            GameLog.Warning(this, "Entry has no candidates. Skipping.");
            yield break;
        }

        if (entry.groupIdList == null || entry.groupIdList.Count == 0)
        {
            GameLog.Warning(this, "Entry has no groupIdList. Skipping.");
            yield break;
        }

        int countPerPoint = entry.CalculateCount(context.difficulty);
        if (countPerPoint <= 0)
        {
            GameLog.Warning(this, "Entry calculated zero enemies per point. Skipping.");
            yield break;
        }

        List<EnemySpawnPoint> availablePoints = CollectSpawnPoints(entry.groupIdList, lookup);
        if (availablePoints.Count == 0)
        {
            GameLog.Warning(this, $"No spawn points found for groups: {string.Join(", ", entry.groupIdList)}");
            yield break;
        }

        List<EnemySpawnPoint> unblockedPoints = new List<EnemySpawnPoint>();
        foreach (EnemySpawnPoint point in availablePoints)
        {
            if (point == null) continue;

            if (!point.IsBlocked())
                unblockedPoints.Add(point);
            else
                GameLog.Debug(this, $"Spawn point [{point.GroupId}] at {point.transform.position} is blocked.");
        }

        if (unblockedPoints.Count == 0)
        {
            GameLog.Warning(this, $"All spawn points blocked for groups: {string.Join(", ", entry.groupIdList)}");
            yield break;
        }

        int pointsToPick = entry.pointCount < 0
            ? unblockedPoints.Count
            : Mathf.Min(entry.pointCount, unblockedPoints.Count);

        if (pointsToPick <= 0)
        {
            GameLog.Warning(this, "Entry selected zero spawn points. Skipping.");
            yield break;
        }

        Shuffle(unblockedPoints, rng);
        List<EnemySpawnPoint> selectedPoints = unblockedPoints.GetRange(0, pointsToPick);

        foreach (EnemySpawnPoint point in selectedPoints)
        {
            if (IsExecutionCancelled(version)) yield break;

            for (int j = 0; j < countPerPoint; j++)
            {
                if (IsExecutionCancelled(version)) yield break;

                EnemyData data = PickCandidate(entry.candidates, rng);
                if (data == null)
                {
                    GameLog.Warning(this, "No valid enemy candidate found.");
                    continue;
                }

                enemyFactory.Create(data, point.transform.position);
            }

            yield return null;
        }
    }

    EnemySpawnPattern SelectPattern(EnemySpawnPatternSet set, System.Random rng)
    {
        float totalWeight = 0f;
        foreach (EnemySpawnPattern pattern in set.patterns)
        {
            if (pattern != null)
                totalWeight += Mathf.Max(0f, pattern.weight);
        }

        if (totalWeight <= 0f)
            return FirstValidPattern(set.patterns);

        float roll = NextFloat(rng) * totalWeight;
        float cumulative = 0f;
        EnemySpawnPattern lastValid = null;

        foreach (EnemySpawnPattern pattern in set.patterns)
        {
            if (pattern == null) continue;

            lastValid = pattern;
            cumulative += Mathf.Max(0f, pattern.weight);
            if (roll <= cumulative)
                return pattern;
        }

        return lastValid;
    }

    EnemySpawnPattern FirstValidPattern(List<EnemySpawnPattern> patterns)
    {
        foreach (EnemySpawnPattern pattern in patterns)
        {
            if (pattern != null)
                return pattern;
        }

        return null;
    }

    EnemyData PickCandidate(List<EnemyCandidate> candidates, System.Random rng)
    {
        float totalWeight = 0f;
        foreach (EnemyCandidate candidate in candidates)
        {
            if (candidate.data != null)
                totalWeight += Mathf.Max(0f, candidate.weight);
        }

        if (totalWeight <= 0f) return null;

        float roll = NextFloat(rng) * totalWeight;
        float cumulative = 0f;
        EnemyData lastValid = null;

        foreach (EnemyCandidate candidate in candidates)
        {
            if (candidate.data == null) continue;

            lastValid = candidate.data;
            cumulative += Mathf.Max(0f, candidate.weight);
            if (roll <= cumulative)
                return candidate.data;
        }

        return lastValid;
    }

    Dictionary<string, List<EnemySpawnPoint>> BuildSpawnPointLookup()
    {
        Dictionary<string, List<EnemySpawnPoint>> lookup = new Dictionary<string, List<EnemySpawnPoint>>();
        EnemySpawnPoint[] points = FindObjectsByType<EnemySpawnPoint>(FindObjectsInactive.Exclude);

        foreach (EnemySpawnPoint point in points)
        {
            if (point == null) continue;

            if (string.IsNullOrEmpty(point.GroupId))
            {
                GameLog.Warning(this, $"SpawnPoint has no groupId: {point.name}");
                continue;
            }

            if (!lookup.TryGetValue(point.GroupId, out List<EnemySpawnPoint> list))
            {
                list = new List<EnemySpawnPoint>();
                lookup[point.GroupId] = list;
            }

            list.Add(point);
        }

        return lookup;
    }

    List<EnemySpawnPoint> CollectSpawnPoints(List<string> groupIds, Dictionary<string, List<EnemySpawnPoint>> lookup)
    {
        List<EnemySpawnPoint> result = new List<EnemySpawnPoint>();

        foreach (string groupId in groupIds)
        {
            if (string.IsNullOrEmpty(groupId)) continue;

            if (lookup.TryGetValue(groupId, out List<EnemySpawnPoint> points))
                result.AddRange(points);
            else
                GameLog.Warning(this, $"No spawn points found for group \"{groupId}\"");
        }

        return result;
    }

    void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    System.Random CreateRandom(SpawnContext context)
        => context.useSeed ? new System.Random(context.seed) : new System.Random();

    float NextFloat(System.Random rng)
        => (float)rng.NextDouble();

    bool IsExecutionCancelled(int version)
        => isStopped || version != runVersion;

    void ResetRunState(int version)
    {
        if (version != runVersion) return;

        ResetRunState();
    }

    void ResetRunState()
    {
        isRunning = false;
        currentRoutine = null;
    }

    void NotifyStopped(SpawnContext context)
    {
        if (stopNotified) return;

        stopNotified = true;
        OnStopped?.Invoke(context);
    }

    SpawnContext CreateDebugContext()
    {
        if (debugUseSeed)
        {
            return new SpawnContext(debugWaveIndex, debugDifficulty, debugSeed);
        }

        return new SpawnContext(debugWaveIndex, debugDifficulty);
    }

    bool CanPlayAssignedSet(string source)
    {
        bool canPlay = true;

        if (patternSet == null)
        {
            GameLog.Warning(this, $"{source}: PatternSet is not assigned.");
            canPlay = false;
        }
        else if (patternSet.patterns == null || patternSet.patterns.Count == 0)
        {
            GameLog.Warning(this, $"{source}: PatternSet \"{patternSet.name}\" has no patterns.");
            canPlay = false;
        }

        if (enemyFactory == null)
        {
            GameLog.Warning(this, $"{source}: EnemyFactory is not assigned.");
            canPlay = false;
        }

        return canPlay;
    }

    [ContextMenu("Test Play (wave=0, difficulty=1)")]
    void TestPlay()
    {
        if (isRunning)
        {
            GameLog.Warning(this, "Already running. Stop first.");
            return;
        }

        if (!CanPlayAssignedSet("Test Play"))
        {
            return;
        }

        currentRoutine = StartCoroutine(Play(new SpawnContext(waveIndex: 0, difficulty: 1f)));
    }

    [ContextMenu("Stop")]
    void TestStop()
    {
        Stop();
    }
}
