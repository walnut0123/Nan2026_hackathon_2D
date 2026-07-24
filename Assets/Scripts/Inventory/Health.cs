using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 5;

    private int currentHealth;
    private bool isDead;

    public event Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth -= amount;
        Debug.Log($"[Health] {gameObject.name} took {amount} damage ({Mathf.Max(currentHealth, 0)}/{maxHealth})");

        if (currentHealth <= 0)
        {
            isDead = true;
            Debug.Log($"[Health] {gameObject.name} died.");
            OnDeath?.Invoke();

            var entity = GetComponent<PersistentWorldEntity>();
            if (entity != null)
                GameManager.Instance?.MarkWorldObjectRemoved(entity.Id);

            Destroy(gameObject);
        }
    }
}
