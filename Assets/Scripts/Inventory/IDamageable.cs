using System;

public interface IDamageable
{
    event Action OnDeath;
    void TakeDamage(float amount);
}
