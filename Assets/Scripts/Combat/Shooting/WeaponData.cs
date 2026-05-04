using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Bullet")]
    public BulletData bulletData;

    [Header("Fire")]
    public float cooldown = 0.2f;
    // Simultaneous shots and sequence shots cover single, shotgun, and burst patterns.
    public int simultaneousShotCount = 1;
    public float spreadAngle = 30f;
    public int sequenceShotCount = 1;
    public float sequenceInterval = 0.08f;

    void OnValidate()
    {
        cooldown = Mathf.Max(0f, cooldown);
        simultaneousShotCount = Mathf.Max(1, simultaneousShotCount);
        spreadAngle = Mathf.Max(0f, spreadAngle);
        sequenceShotCount = Mathf.Max(1, sequenceShotCount);
        sequenceInterval = Mathf.Max(0f, sequenceInterval);
    }
}
