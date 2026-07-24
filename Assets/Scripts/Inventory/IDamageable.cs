using System;

public interface IDamageable
{
    event Action OnDeath;
    void TakeDamage(int amount);
}
