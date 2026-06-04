using System.Collections.Generic;
using UnityEngine;

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