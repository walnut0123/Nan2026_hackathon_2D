using System.Collections;
using UnityEngine;

// 마인 유닛(1_Ink_A) 전용 자폭 사망 연출. Health.OnDeath가 발동해도 곧바로 파괴되지 않도록 - 이
// 컴포넌트가 붙은 프리팹은 Health.deathDestroyDelay를 넉넉히 크게 잡아둬야 한다(자폭 시퀀스가
// 자기 자신을 직접 Destroy하므로 그 큰 값은 실제로는 절대 발동하지 않는 안전망일 뿐이다) - 자폭
// 타이머 동안 이동/평소 공격을 멈추고 빨간색으로 깜빡인 뒤, 최후의 탄막을 한 번 더 터뜨리고 나서야
// 실제로 파괴한다. 빨간색 깜빡임은 PlayerHitFlash와 동일한 방식(SpriteRenderer.color를 경고색으로
// 물들였다가 되돌리는 것)을 반복해서 재현했다.
[RequireComponent(typeof(Health))]
public class MineSelfDestructSequence : MonoBehaviour
{
    [Header("자폭 타이머")]
    [Tooltip("체력이 0이 된 순간부터 최후 탄막이 나가고 파괴되기까지의 시간(초)")]
    [SerializeField] private float fuseDuration = 1f;

    [Tooltip("자폭 경고색 - 플레이어 피격 연출(PlayerHitFlash)과 같은 방식으로 SpriteRenderer.color에 적용된다")]
    [SerializeField] private Color warningColor = Color.red;

    [Tooltip("경고색으로 깜빡이는 주기(초) - 값이 작을수록 빠르게 깜빡인다")]
    [SerializeField] private float blinkInterval = 0.12f;

    private Health health;
    private SpriteRenderer spriteRenderer;
    private MobLinearChaser chaser;
    private MobPeriodicBulletRing bulletRing;

    private Color originalColor;

    private void Awake()
    {
        health = GetComponent<Health>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        chaser = GetComponent<MobLinearChaser>();
        bulletRing = GetComponent<MobPeriodicBulletRing>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        // 자폭 타이머가 시작되는 순간부터 이동/평소 공격 패턴을 모두 멈춘다.
        if (chaser != null)
            chaser.enabled = false;
        if (bulletRing != null)
            bulletRing.enabled = false;

        StartCoroutine(SelfDestructRoutine());
    }

    private IEnumerator SelfDestructRoutine()
    {
        float elapsed = 0f;
        bool showingWarning = false;

        while (elapsed < fuseDuration)
        {
            if (spriteRenderer != null)
            {
                showingWarning = !showingWarning;
                spriteRenderer.color = showingWarning ? warningColor : originalColor;
            }

            float wait = Mathf.Min(blinkInterval, fuseDuration - elapsed);
            yield return new WaitForSeconds(wait);
            elapsed += wait;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = warningColor;

        // enabled = false 상태여도 public 메서드 직접 호출은 정상 동작한다 - Update만 멈춘 것이지
        // 컴포넌트 자체는 살아있기 때문. 쿨타임과 무관하게 최후의 한 발을 강제로 쏜다.
        if (bulletRing != null)
            bulletRing.FireBurst();

        Destroy(gameObject);
    }
}
