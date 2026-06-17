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
    [Tooltip("Seconds to wait before this entry spawns after the previous entry.")]
    public float delay = 0f;

    [Tooltip("Spawn point group IDs to draw from. Must match EnemySpawnPoint.GroupId.")]
    public List<string> groupIdList = new List<string>();

    [Tooltip("How many distinct spawn points to use for this entry.")]
    public int pointCount = 1;

    [Tooltip("Base number of enemies to spawn for this entry before difficulty scaling.")]
    public int baseCount = 1;

    [Tooltip("Added to base count per difficulty point when Count Multiplier Curve is empty.")]
    public float difficultyBonusPerLevel = 0.5f;

    [Tooltip("Optional multiplier evaluated by difficulty. If empty, Base Count + Difficulty Bonus is used.")]
    public AnimationCurve countMultiplierCurve;

    [Tooltip("Weighted enemy choices for this entry.")]
    public List<EnemyCandidate> candidates = new List<EnemyCandidate>();

    public int CalculateCount(float difficulty)
    {
        int sanitizedBaseCount = Mathf.Max(0, baseCount);

        if (countMultiplierCurve != null && countMultiplierCurve.length > 0)
        {
            float multiplier = Mathf.Max(0f, countMultiplierCurve.Evaluate(difficulty));
            return Mathf.Max(0, Mathf.RoundToInt(sanitizedBaseCount * multiplier));
        }

        float scaledCount = sanitizedBaseCount + difficultyBonusPerLevel * Mathf.Max(0f, difficulty);
        return Mathf.Max(0, Mathf.RoundToInt(scaledCount));
    }
}

/// <summary>
/// A sequence of timed spawn entries. Selected from EnemySpawnPatternSet with a weight.
/// Each entry defines a delay, spawn point groups, count, and enemy candidates.
/// </summary>
[CreateAssetMenu(fileName = "NewSpawnPattern", menuName = "Combat/Enemy Spawn Pattern")]
public class EnemySpawnPattern : ScriptableObject
{
    [Tooltip("Weight for selection from EnemySpawnPatternSet. Higher = more likely.")]
    public float weight = 1f;

    public List<EnemySpawnEntry> entries = new List<EnemySpawnEntry>();
}
