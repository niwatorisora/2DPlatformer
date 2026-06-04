using UnityEngine;

/// <summary>
/// Spawns enemies from EnemyData assets and wires up their runtime dependencies.
/// Centralising Instantiate + Initialize here keeps scene setup minimal and makes
/// future pooling a drop-in replacement inside Create without changing callers.
/// </summary>
public class EnemyFactory : MonoBehaviour
{
    [SerializeField] BulletPool bulletPool;
    [SerializeField] Transform playerTarget;

    [Header("Debug / prototype spawn")]
    [Tooltip("Assign an EnemyData asset; Create() is not called unless something references it (e.g. spawn on start or Context Menu).")]
    [SerializeField] EnemyData debugEnemyData;
    [SerializeField] bool spawnOnStart;
    [Tooltip("World position offset from this transform when using spawn-on-start or Debug Spawn Here.")]
    [SerializeField] Vector2 spawnOffset;

    /// <summary>
    /// Instantiates the enemy defined by data at the given world position and
    /// initialises it with the shared bullet pool and player target.
    /// Returns the EnemyController of the spawned instance, or null on failure.
    /// </summary>
    public EnemyController Create(EnemyData data, Vector2 position)
    {
        if (data == null)
        {
            GameLog.Error(this, "EnemyData is null; cannot spawn enemy.");
            return null;
        }

        if (data.prefab == null)
        {
            GameLog.Error(this, $"{data.name} has no prefab assigned.");
            return null;
        }

        GameObject instance = Instantiate(data.prefab, position, Quaternion.identity);
        var controller = instance.GetComponent<EnemyController>();
        if (controller == null)
            controller = instance.AddComponent<EnemyController>();

        if (!controller.Initialize(data, playerTarget, bulletPool))
        {
            Destroy(instance);
            return null;
        }

        GameLog.Debug(this, $"Spawned {data.name} at {position}.");
        return controller;
    }

    void Start()
    {
        if (!spawnOnStart || debugEnemyData == null) return;

        Vector2 pos = (Vector2)transform.position + spawnOffset;
        Create(debugEnemyData, pos);
    }

    /// <summary>
    /// Convenience method for quick in-Editor spawning during development.
    /// Assign <see cref="debugEnemyData"/> on this component and use the gear menu on the Inspector header.
    /// </summary>
    [ContextMenu("Debug Spawn Here")]
    void DebugSpawnHere()
    {
        if (debugEnemyData == null)
        {
            GameLog.Warning(this, "Assign Debug Enemy Data on EnemyFactory before using Debug Spawn Here.");
            return;
        }

        Vector2 pos = (Vector2)transform.position + spawnOffset;
        Create(debugEnemyData, pos);
    }
}
