using System;
using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    // 카드 데미지가 소수점 한 자리까지 나오므로(CardDamageSystem 참고, BASE_SCALE 10배 스케일 폐지)
    // 체력도 float로 받아야 그 정밀도가 실제로 반영된다.
    [SerializeField] private float maxHealth = 5f;

    [Tooltip("사망 애니메이션 재생 시간 확보를 위한 파괴 지연(초). 0이면 기존처럼 즉시 파괴.")]
    [SerializeField] private float deathDestroyDelay = 0f;

    [Tooltip("true면 피격 이벤트(데미지 텍스트/체력바/Hit 애니메이션)는 그대로 발생하지만 실제 체력은 줄지 않고 사망하지 않는다. 테스트용 더미 타겟(허수아비 등)에 사용.")]
    [SerializeField] private bool isInvincible = false;

    private float currentHealth;
    private bool isDead;
    private bool initialized;

    public event Action OnDeath;
    public event Action<float> OnDamaged;

    // EnemyHealthBar 등 UI가 현재/최대 체력을 읽을 수 있도록 노출. CurrentHealth는 지연
    // 초기화한다 - 비활성 상태로 배치해뒀다가 나중에 활성화되는 오브젝트(보스 등)는 Awake가
    // 아직 안 돌았을 수 있는데, 그 사이 UI가 먼저 CurrentHealth를 읽으면 필드 기본값 0이
    // 아니라 maxHealth를 봐야 하기 때문.
    public float CurrentHealth
    {
        get { EnsureInitialized(); return currentHealth; }
    }
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        currentHealth = maxHealth;
        initialized = true;
    }

    /// <summary>개발 테스트용: 최대 체력을 바꾸고 그만큼 완전 회복시킨다(예: 방 스킵용 체력 1 몬스터).</summary>
    public void SetMaxHealthAndFullHeal(float value)
    {
        maxHealth = value;
        currentHealth = value;
        initialized = true;
    }

    public void TakeDamage(float amount)
    {
        EnsureInitialized();

        if (isDead || amount <= 0f)
            return;

        if (isInvincible)
        {
            OnDamaged?.Invoke(amount);
            return;
        }

        currentHealth -= amount;
        Debug.Log($"[Health] {gameObject.name} took {amount:F1} damage ({Mathf.Max(currentHealth, 0f):F1}/{maxHealth:F1})");
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
