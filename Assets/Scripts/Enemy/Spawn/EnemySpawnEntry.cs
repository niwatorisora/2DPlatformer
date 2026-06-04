using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single weighted candidate for enemy selection inside an EnemySpawnEntry.
/// </summary>
[System.Serializable]
public struct EnemyCandidate
{
    public EnemyData data;

    [Tooltip("Higher weight = more likely to be chosen.")]
    public float weight;
}

/// <summary>
/// One timed spawn event inside a pattern.
/// </summary>
[System.Serializable]
public class EnemySpawnEntry
{
    [Tooltip("Seconds to wait before this entry runs.")]
    public float delay = 0f;

    [Tooltip("Spawn point group IDs to draw from. Must match EnemySpawnPoint.GroupId values.")]
    public List<string> groupIdList = new List<string>();

    [Tooltip("Number of unique spawn points to pick. -1 means all unblocked points in the listed groups.")]
    public int pointCount = 1;

    [Tooltip("Enemy count spawned at each selected spawn point before difficulty scaling.")]
    public int baseCount = 1;

    [Tooltip("Additional per-point count added by difficulty when no curve is assigned.")]
    public float difficultyBonusPerLevel = 0.5f;

    [Tooltip("Optional multiplier curve evaluated by difficulty. When set, count = baseCount * curve(difficulty).")]
    public AnimationCurve countMultiplierCurve;

    [Tooltip("Enemy candidates to randomly pick from, weighted per spawned enemy.")]
    public List<EnemyCandidate> candidates = new List<EnemyCandidate>();

    /// <summary>
    /// Calculates the number of enemies spawned per selected spawn point.
    /// Total spawned by this entry is selectedPointCount * this value.
    /// </summary>
    public int CalculateCount(float difficulty)
    {
        int sanitizedBaseCount = Mathf.Max(0, baseCount);

        if (countMultiplierCurve != null && countMultiplierCurve.keys.Length > 0)
        {
            float multiplier = Mathf.Max(0f, countMultiplierCurve.Evaluate(difficulty));
            return Mathf.RoundToInt(sanitizedBaseCount * multiplier);
        }

        if (difficultyBonusPerLevel > 0f)
        {
            float scaledCount = sanitizedBaseCount + difficultyBonusPerLevel * Mathf.Max(0f, difficulty);
            return Mathf.Max(0, Mathf.RoundToInt(scaledCount));
        }

        return sanitizedBaseCount;
    }
}
