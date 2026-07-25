using System;
using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 5;

    [Tooltip("사망 애니메이션 재생 시간 확보를 위한 파괴 지연(초). 0이면 기존처럼 즉시 파괴.")]
    [SerializeField] private float deathDestroyDelay = 0f;

    private int currentHealth;
    private bool isDead;

    public event Action OnDeath;
    public event Action<int> OnDamaged;

    // EnemyHealthBar 등 UI가 현재/최대 체력을 읽을 수 있도록 노출.
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

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
        OnDamaged?.Invoke(amount);

        if (currentHealth <= 0)
        {
            isDead = true;
            Debug.Log($"[Health] {gameObject.name} died.");
            OnDeath?.Invoke();

            var entity = GetComponent<PersistentWorldEntity>();
            if (entity != null)
                GameManager.Instance?.MarkWorldObjectRemoved(entity.Id);

            if (deathDestroyDelay > 0f)
                StartCoroutine(DestroyAfterDelay());
            else
                Destroy(gameObject);
        }
    }


private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(deathDestroyDelay);
        Destroy(gameObject);
    }
}
