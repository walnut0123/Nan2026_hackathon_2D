using UnityEngine;

// 원거리 공격 상태 머신. EnemyAttacker(근접, 접촉 즉시 판정)와 달리 실제 RangedProjectile을
// 생성해서 날려보내고, 데미지는 그 투사체의 Collider2D 충돌로만 발생한다(여기서는 직접 주지 않음).
// 사거리 안에 들어오면 EnemyChaser를 꺼서 그 자리에 멈춰 서서 쏘고, 대상이 사거리를 벗어나면
// 다시 추적을 재개한다 - 근접 유닛처럼 끝까지 파고들지 않는 "원거리 유닛다운" 움직임을 위함.
public class RangedAttacker : MonoBehaviour
{
    [Header("공격 설정")]
    [Tooltip("이 거리 이하로 타겟이 가까워지면 멈춰서 투사체를 발사합니다")]
    [SerializeField] private float attackRange = 5f;

    [Tooltip("투사체 1발당 데미지")]
    [SerializeField] private int attackDamage = 1;

    [Tooltip("발사 후 다음 발사까지의 쿨타임(초)")]
    [SerializeField] private float attackCooldown = 2f;

    [Header("투사체 발사 설정")]
    [Tooltip("투사체가 실제로 생성되는 위치(총구 끝). 비워두면 이 오브젝트 위치를 사용")]
    [SerializeField] private Transform muzzle;

    [Tooltip("발사할 투사체 프리팹 (RangedProjectile 포함)")]
    [SerializeField] private GameObject projectilePrefab;

    private Animator animator;
    private EnemyChaser chaser;
    private Transform targetTransform;

    private float cooldownRemaining;

    // RangedWeaponAim 등 무기 비주얼이 같은 타겟을 겨눌 수 있도록 노출.
    public Transform TargetTransform => targetTransform;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        chaser = GetComponent<EnemyChaser>();
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
        if (targetTransform == null)
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

        Fire();
        cooldownRemaining = attackCooldown;
    }

    private void Fire()
    {
        if (projectilePrefab == null || targetTransform == null)
            return;

        Vector3 spawnPosition = muzzle != null ? muzzle.position : transform.position;
        Vector2 direction = (Vector2)(targetTransform.position - spawnPosition);

        var projectileInstance = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        var projectile = projectileInstance.GetComponent<RangedProjectile>();
        if (projectile != null)
            projectile.Initialize(direction, attackDamage);

        if (animator != null)
            animator.SetTrigger("Attack");
    }

    // EnemyAttacker의 attackRange 기즈모와 동일한 방식 - Inspector에서 값을 바꾸면 Scene 뷰의
    // 빨간 원이 즉시 갱신된다.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
