using System.Collections;
using UnityEngine;

// 1-1/2-1/2-2 mop이 공유하는 원거리 공격 상태 머신. RangedAttacker와 거의 동일하지만 한 번의
// 공격에서 여러 발을 연달아 쏘거나(shotsPerBurst), 방향에 무작위 편차(spreadAngle)를 줄 수 있다 -
// 세 몬스터의 차이(2발 직선 / ±30도 부채꼴 1발 / 4발 직선)를 스크립트 복제 없이 Inspector 값만으로
// 표현하기 위함.
public class RangedBurstAttacker : MonoBehaviour
{
    [Header("공격 설정")]
    [Tooltip("이 거리 이하로 타겟이 가까워지면 멈춰서 투사체를 발사합니다")]
    [SerializeField] private float attackRange = 5f;

    [Tooltip("투사체 1발당 데미지")]
    [SerializeField] private int attackDamage = 1;

    [Tooltip("공격 1회(연사 전체) 종료 후 다음 공격까지의 쿨타임(초)")]
    [SerializeField] private float attackCooldown = 2f;

    [Header("연사 설정")]
    [Tooltip("공격 1회당 발사하는 탄환 수")]
    [SerializeField] private int shotsPerBurst = 1;
    [Tooltip("연사 중 탄환 사이의 간격(초). 0이면 전부 동시에 나간다.")]
    [SerializeField] private float shotInterval = 0.12f;
    [Tooltip("타겟 방향 기준으로 좌우 합쳐 벌어지는 전체 각도(도). 0이면 항상 타겟을 정확히 조준한다. " +
        "60을 넣으면 타겟 방향 ±30도의 부채꼴 범위 안에서 매 발마다 무작위 각도로 나간다.")]
    [SerializeField] private float spreadAngle = 0f;

    [Header("투사체 발사 설정")]
    [Tooltip("투사체가 실제로 생성되는 위치. 비워두면 이 오브젝트(입/본체) 위치를 사용한다.")]
    [SerializeField] private Transform muzzle;
    [Tooltip("발사할 투사체 프리팹 (RangedProjectile 포함)")]
    [SerializeField] private GameObject projectilePrefab;

    private Animator animator;
    private EnemyAIPathMover chaser;
    private Transform targetTransform;

    private float cooldownRemaining;
    private bool isBursting;

    public Transform TargetTransform => targetTransform;

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
        Tick(Time.deltaTime);
    }

    private void Tick(float dt)
    {
        if (targetTransform == null || isBursting)
            return;

        float distance = Vector2.Distance(transform.position, targetTransform.position);
        bool inRange = distance <= attackRange;

        // 사거리 안에서는 멈춰서 쏘고, 벗어나면 다시 쫓아간다.
        if (chaser != null)
            chaser.enabled = !inRange;

        if (!inRange)
            return;

        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= dt;
            return;
        }

        StartCoroutine(FireBurst());
        cooldownRemaining = attackCooldown;
    }

    private IEnumerator FireBurst()
    {
        isBursting = true;
        if (chaser != null)
            chaser.enabled = false;

        if (animator != null)
            animator.SetTrigger("Attack");

        int count = Mathf.Max(1, shotsPerBurst);
        for (int i = 0; i < count; i++)
        {
            FireOne();
            if (i < count - 1 && shotInterval > 0f)
                yield return new WaitForSeconds(shotInterval);
        }

        isBursting = false;
    }

    private void FireOne()
    {
        if (projectilePrefab == null || targetTransform == null)
            return;

        Vector3 spawnPosition = muzzle != null ? muzzle.position : transform.position;
        Vector2 baseDirection = (Vector2)(targetTransform.position - spawnPosition);
        Vector2 direction = spreadAngle > 0f ? RandomizeDirection(baseDirection) : baseDirection;

        var projectileInstance = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        var projectile = projectileInstance.GetComponent<RangedProjectile>();
        if (projectile != null)
            projectile.Initialize(direction, attackDamage);
    }

    // baseDirection을 중심으로 ±spreadAngle/2 범위 안에서 무작위 각도로 방향을 흩뿌린다(부채꼴 발사).
    private Vector2 RandomizeDirection(Vector2 baseDirection)
    {
        if (baseDirection.sqrMagnitude < 0.0001f)
            baseDirection = Vector2.right;

        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
        float offset = Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f);
        float rad = (baseAngle + offset) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    // RangedAttacker의 attackRange 기즈모와 동일한 방식 - Inspector에서 값을 바꾸면 Scene 뷰의
    // 빨간 원이 즉시 갱신된다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
