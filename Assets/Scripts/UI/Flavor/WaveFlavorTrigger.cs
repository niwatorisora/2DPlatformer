using UnityEngine;

// ウェーブ開始とフレーバーテキスト表示をつなぐ最小限の接着層。
public class WaveFlavorTrigger : MonoBehaviour
{
    [SerializeField] WaveSpawner spawner;
    [SerializeField] FlavorTextService flavorTextService;

    void OnEnable()
    {
        if (spawner != null) spawner.OnWaveStarted += HandleWaveStarted;
    }

    void OnDisable()
    {
        if (spawner != null) spawner.OnWaveStarted -= HandleWaveStarted;
    }

    void HandleWaveStarted(int waveIndex)
    {
        if (flavorTextService == null) return;
        if (waveIndex == 0 && flavorTextService.Show("opening")) return;
        flavorTextService.Show($"wave{waveIndex + 1}");
    }
}
