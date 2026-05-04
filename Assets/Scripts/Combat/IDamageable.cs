using System;

/// <summary>
/// Contract for objects that can receive combat damage.
/// </summary>
public interface IDamageable
{
    event Action<int> OnDamaged;
    bool CanReceiveDamage(DamageContext context);
    void TakeDamage(DamageContext context);
}
