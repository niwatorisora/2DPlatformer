using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A collection of spawn patterns with selection weights. The runner picks one
/// pattern per Play() call based on these weights.
/// </summary>
[CreateAssetMenu(fileName = "NewSpawnPatternSet", menuName = "Combat/Enemy Spawn Pattern Set")]
public class EnemySpawnPatternSet : ScriptableObject
{
    public List<EnemySpawnPattern> patterns = new List<EnemySpawnPattern>();
}