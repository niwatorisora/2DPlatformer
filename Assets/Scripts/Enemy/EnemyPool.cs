using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EnemyFactory が所有する敵インスタンスのプール。
/// Prefab ごとに分けるため、異なる移動・攻撃コンポーネントを安全に再利用できる。
/// </summary>
public class EnemyPool
{
    readonly Dictionary<GameObject, Queue<EnemyController>> available = new();
    readonly Dictionary<EnemyController, GameObject> prefabByInstance = new();
    readonly HashSet<EnemyController> returnedInstances = new();

    public EnemyController Rent(GameObject prefab, Transform parent)
    {
        if (prefab == null) return null;

        EnemyController enemy = TakeAvailable(prefab);
        if (enemy == null)
        {
            GameObject instance = Object.Instantiate(prefab, parent);
            enemy = instance.GetComponent<EnemyController>();
            if (enemy == null) enemy = instance.AddComponent<EnemyController>();
            prefabByInstance[enemy] = prefab;
        }
        else
        {
            enemy.transform.SetParent(parent);
        }

        returnedInstances.Remove(enemy);
        enemy.gameObject.SetActive(true);
        return enemy;
    }

    // 呼び出し側は元 Prefab を覚えておく必要がない。
    public void Return(EnemyController enemy)
    {
        if (enemy == null || returnedInstances.Contains(enemy)) return;
        if (!prefabByInstance.TryGetValue(enemy, out GameObject prefab))
        {
            GameLog.Warning(enemy, "EnemyPool cannot return an enemy it did not rent.");
            return;
        }

        if (!available.TryGetValue(prefab, out Queue<EnemyController> queue))
        {
            queue = new Queue<EnemyController>();
            available.Add(prefab, queue);
        }

        // Destroy の代わりに無効化して返す。再使用時は EnemyController.Initialize が HP、AI、物理速度を再設定する。
        enemy.gameObject.SetActive(false);
        returnedInstances.Add(enemy);
        queue.Enqueue(enemy);
    }

    public void Return(GameObject prefab, EnemyController enemy)
    {
        if (enemy != null && !prefabByInstance.ContainsKey(enemy) && prefab != null)
            prefabByInstance.Add(enemy, prefab);

        Return(enemy);
    }

    EnemyController TakeAvailable(GameObject prefab)
    {
        if (!available.TryGetValue(prefab, out Queue<EnemyController> queue)) return null;

        while (queue.Count > 0)
        {
            EnemyController enemy = queue.Dequeue();
            if (enemy != null) return enemy;
        }

        return null;
    }
}
