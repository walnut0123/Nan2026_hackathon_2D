using System.Collections;
using UnityEngine;

// 3-3 mop 전용 돌진 공격. 보스의 일직선 패턴(BossAttackController.LinePattern)과 동일한 형태의
// 인디케이터(보스 위치에서 사거리 끝까지 자라나는 라인)로 먼저 예열한 뒤, 예열이 끝나는 순간
// 그 라인 위에 즉발 데미지를 주는 동시에 몸체도 그 방향으로 빠르게 돌진한다 - 텔레그래프로
// 플레이어가 피할 시간을 주면서도 "몸통박치기"라는 몬스터다운 연출을 함께 표현한다.
[RequireComponent(typeof(Collider2D))]
public class DashChargeAttacker : MonoBehaviour
{
    [Header("공격 설정")]
    [Tooltip("이 거리 이하로 타겟이 가까워지면 돌진을 준비한다")]
    [SerializeField] private float attackRange = 4f;
    [Tooltip("돌진 라인 위에 있을 때 즉발로 주는 데미지")]
    [SerializeField] private int attackDamage = 2;
    [Tooltip("돌진 1회 종료 후 다음 시전까지의 쿨타임(초)")]
    [SerializeField] private float attackCooldown = 3f;
    [Tooltip("돌진 사거리(월드 단위) - 인디케이터가 뻗어나가는 거리이자 실제 돌진 거리")]
    [SerializeField] private float dashDistance = 3f;

    [Header("인디케이터 설정(보스 일직선 패턴과 동일한 방식)")]
    [Tooltip("시전 시작부터 실제 피격 판정까지의 예열 시간(초)")]
    [SerializeField] private float indicatorWarmupTime = 0.8f;
    [Tooltip("라인 길이가 0에서 사거리 끝까지 그려지는 연출 시간(초)")]
    [SerializeField] private float drawInDuration = 0.25f;
    [Tooltip("라인(및 피격 판정) 두께(월드 단위)")]
    [SerializeField] private float indicatorWidth = 0.5f;
    [SerializeField] private Color indicatorStartColor = new Color(1f, 0.3f, 0.3f, 0.15f);
    [SerializeField] private Color indicatorEndColor = new Color(1f, 0f, 0f, 0.75f);

    [Header("돌진 연출")]
    [Tooltip("예열이 끝난 뒤 실제로 몸체가 라인 끝까지 이동하는 데 걸리는 시간(초)")]
    [SerializeField] private float dashDuration = 0.2f;

    private Animator animator;
    private EnemyAIPathMover chaser;
    private Transform targetTransform;

    private float cooldownRemaining;
    private bool isAttacking;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        chaser = GetComponent<EnemyAIPathMover>();
    }

    private void Start()
    {
        var priorityTarget = FindObjectOfType<PriorityTarget>();
        if (priorityTarget != null)
        {
            targetTransform = priorityTarget.transform;
            return;
        }

        var playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
            targetTransform = playerInventory.transform;
    }

    private void Update()
    {
        if (targetTransform == null || isAttacking)
            return;

        float distance = Vector2.Distance(transform.position, targetTransform.position);
        bool inRange = distance <= attackRange;

        if (chaser != null)
            chaser.enabled = !inRange;

        if (!inRange)
            return;

        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
            return;
        }

        StartCoroutine(ChargeRoutine());
        cooldownRemaining = attackCooldown;
    }

    private IEnumerator ChargeRoutine()
    {
        isAttacking = true;
        if (chaser != null)
            chaser.enabled = false;
        if (animator != null)
            animator.SetTrigger("Attack");

        // 방향은 시전 시작 시점에 고정한다 - 인디케이터가 보여준 그대로 돌진해야 텔레그래프로서
        // 의미가 있다(보스 LineCastRoutine과 동일한 원칙).
        Vector2 direction = ((Vector2)(targetTransform.position - transform.position));
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;
        direction.Normalize();

        Vector3 origin = transform.position;
        Vector3 endPoint = origin + (Vector3)(direction * dashDistance);

        GameObject indicator = CreateLineIndicator(origin, direction, indicatorWidth, indicatorStartColor);
        var indicatorRenderer = indicator.GetComponent<SpriteRenderer>();

        float elapsed = 0f;
        while (elapsed < indicatorWarmupTime)
        {
            elapsed += Time.deltaTime;
            float drawT = drawInDuration > 0f ? Mathf.Clamp01(elapsed / drawInDuration) : 1f;
            float colorT = indicatorWarmupTime > 0f ? Mathf.Clamp01(elapsed / indicatorWarmupTime) : 1f;

            indicator.transform.localScale = new Vector3(dashDistance * drawT, indicatorWidth, 1f);
            indicatorRenderer.color = Color.Lerp(indicatorStartColor, indicatorEndColor, colorT);

            yield return null;
        }

        DealLineDamage(origin, endPoint, indicatorWidth, attackDamage);
        Destroy(indicator);

        yield return DashMove(origin, endPoint);

        isAttacking = false;
    }

    private IEnumerator DashMove(Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = dashDuration > 0f ? Mathf.Clamp01(elapsed / dashDuration) : 1f;
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        transform.position = to;
    }

    // origin에서 direction 방향으로 뻗어나갈 직사각형 라인 인디케이터를 길이 0으로 생성한다 -
    // BossAttackController.CreateLineIndicator와 동일한 방식(BossIndicatorUtil 사각형 스프라이트 재사용).
    private GameObject CreateLineIndicator(Vector3 origin, Vector2 direction, float width, Color color)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        var indicator = new GameObject("DashIndicator");
        indicator.transform.position = origin;
        indicator.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        indicator.transform.localScale = new Vector3(0f, width, 1f);

        var renderer = indicator.AddComponent<SpriteRenderer>();
        renderer.sprite = BossIndicatorUtil.GetRectangleSprite();
        renderer.color = color;
        renderer.sortingOrder = 2;

        return indicator;
    }

    // a-b 라인 위(두께 width의 직사각형 판정)에 있는 대상에게 즉발 데미지를 준다.
    private void DealLineDamage(Vector3 a, Vector3 b, float width, int damage)
    {
        Vector2 diff = b - a;
        float length = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        Vector2 center = (Vector2)a + diff * 0.5f;

        var hits = Physics2D.OverlapBoxAll(center, new Vector2(length, width), angle);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy") || hit.CompareTag("Breakable"))
                continue;

            var health = DamageUtil.ResolveHealth(hit);
            if (health != null)
                health.TakeDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
