using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>1 つのアセットにウェーブ全体を定義するデータテーブル。</summary>
[CreateAssetMenu(fileName = "NewWaveTable", menuName = "Combat/Wave Table")]
public class WaveTable : ScriptableObject
{
    [Serializable]
    public class SpawnRow
    {
        public EnemyData enemy;
        [Tooltip("この行の敵の見た目。未指定ならEnemyDataのデフォルトスキン")]
        [SerializeField] public EnemySkin skin;
        [Tooltip("この行の敵の移動挙動。未指定ならEnemyDataのデフォルト")]
        [SerializeField] public MovementProfile movementProfile;
        [Tooltip("この行の敵の攻撃手段。未指定ならEnemyDataのデフォルト")]
        [SerializeField] public AttackProfile attackProfile;
        [Min(0)] public int count = 1;
        [Min(0)] public int spawnPointIndex;
        [Min(0f)] public float spawnInterval = 0.5f;
    }

    [Serializable]
    public class Wave
    {
        public string label;
        [Min(0f)] public float startDelay = 1f;
        public List<SpawnRow> rows = new();
    }

    public List<Wave> waves = new();
}
