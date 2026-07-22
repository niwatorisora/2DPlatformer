using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// この 1 コンポーネントがウェーブ進行と敵生成の両方を担う。進行条件は全滅。
// aliveCount は生成成功時に増やし、EnemyController の Despawn コールバックで減らす。
public class WaveSpawner : MonoBehaviour
{
    [SerializeField] WaveTable waveTable;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] Transform playerTarget;
    [SerializeField] BulletPool bulletPool;
    [Tooltip("IEnemyKillListener を実装したコンポーネントを指定します。")]
    [SerializeField] MonoBehaviour[] killListeners;
    [SerializeField] bool playOnStart = true;

    readonly EnemyPool enemyPool = new();
    readonly List<IEnemyKillListener> cachedKillListeners = new();
    readonly HashSet<EnemyController> aliveEnemies = new();
    Coroutine waveRoutine;
    int aliveCount;

    public event Action OnAllWavesCleared;
    public event Action<int> OnWaveStarted;
    public int CurrentWaveIndex { get; private set; } = -1;

    void Awake() => CacheKillListeners();

    void Start()
    {
        if (playOnStart) Play();
    }

    public void Play()
    {
        if (waveRoutine != null)
        {
            GameLog.Warning(this, "Wave spawning is already in progress.");
            return;
        }

        if (waveTable == null)
        {
            GameLog.Error(this, "WaveTable is not assigned; cannot play waves.");
            return;
        }

        CurrentWaveIndex = -1;
        waveRoutine = StartCoroutine(PlayWaves());
    }

    IEnumerator PlayWaves()
    {
        for (int waveIndex = 0; waveIndex < waveTable.waves.Count; waveIndex++)
        {
            CurrentWaveIndex = waveIndex;
            WaveTable.Wave wave = waveTable.waves[waveIndex];
            if (wave == null)
            {
                GameLog.Warning(this, $"Wave {waveIndex} is null; skipping it.");
                continue;
            }

            OnWaveStarted?.Invoke(waveIndex);
            yield return new WaitForSeconds(Mathf.Max(0f, wave.startDelay));

            if (wave.rows != null)
            {
                for (int rowIndex = 0; rowIndex < wave.rows.Count; rowIndex++)
                {
                    WaveTable.SpawnRow row = wave.rows[rowIndex];
                    if (row == null) continue;

                    for (int spawnIndex = 0; spawnIndex < Mathf.Max(0, row.count); spawnIndex++)
                    {
                        Spawn(row, waveIndex, rowIndex);
                        if (spawnIndex < row.count - 1)
                            yield return new WaitForSeconds(Mathf.Max(0f, row.spawnInterval));
                    }
                }
            }

            yield return new WaitUntil(() => aliveCount == 0);
        }

        CurrentWaveIndex = -1;
        waveRoutine = null;
        GameLog.Debug(this, "All waves cleared.");
        OnAllWavesCleared?.Invoke();
    }

    void Spawn(WaveTable.SpawnRow row, int waveIndex, int rowIndex)
    {
        if (row.enemy == null)
        {
            GameLog.Error(this, $"Wave {waveIndex}, row {rowIndex} has no EnemyData.");
            return;
        }

        if (row.enemy.prefab == null)
        {
            GameLog.Error(this, $"{row.enemy.name} has no prefab assigned.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            GameLog.Error(this, "No spawn points are assigned; cannot spawn an enemy.");
            return;
        }

        int pointIndex = Mathf.Clamp(row.spawnPointIndex, 0, spawnPoints.Length - 1);
        if (pointIndex != row.spawnPointIndex)
            GameLog.Error(this, $"Spawn point index {row.spawnPointIndex} is invalid; using {pointIndex}.");

        Transform spawnPoint = spawnPoints[pointIndex];
        if (spawnPoint == null)
        {
            GameLog.Error(this, $"Spawn point {pointIndex} is not assigned.");
            return;
        }

        EnemyController enemy = enemyPool.Rent(row.enemy.prefab, transform);
        if (enemy == null)
        {
            GameLog.Error(this, $"Could not rent {row.enemy.name} from the enemy pool.");
            return;
        }

        enemy.transform.SetPositionAndRotation(spawnPoint.position, Quaternion.identity);
        EnemySkin skin = row.skin ?? row.enemy.defaultSkin;
        Sprite skinSprite = skin != null ? skin.PickRandomSprite() : null;
        if (!enemy.Initialize(row.enemy, playerTarget, bulletPool, cachedKillListeners,
                HandleEnemyDespawned, skinSprite))
        {
            enemyPool.Return(enemy);
            return;
        }

        aliveEnemies.Add(enemy);
        aliveCount++;
        GameLog.Debug(this, $"Spawned {row.enemy.name} at {spawnPoint.position}.");
    }

    void HandleEnemyDespawned(EnemyController enemy)
    {
        enemyPool.Return(enemy);
        if (aliveEnemies.Remove(enemy)) aliveCount--;
    }

    void CacheKillListeners()
    {
        cachedKillListeners.Clear();
        if (killListeners == null) return;

        foreach (MonoBehaviour listenerComponent in killListeners)
        {
            if (listenerComponent is IEnemyKillListener listener)
                cachedKillListeners.Add(listener);
            else if (listenerComponent != null)
                GameLog.Warning(this, $"{listenerComponent.name} must implement IEnemyKillListener.");
        }
    }
}
