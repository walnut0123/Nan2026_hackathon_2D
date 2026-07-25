using UnityEngine;

/// <summary>Melee attack state machine: only ever deals damage through this explicit
/// attack-state timer (see Tick), never on physical contact - touching the enemy's collider
/// alone deals no damage. Pauses EnemyChaser while attacking so the lunge reads as a deliberate
/// action rather than the enemy still chasing mid-swing.</summary>
public class EnemyAttacker : MonoBehaviour
{
    [Header("공격 설정")]
    [Tooltip("이 거리 이하로 플레이어가 가까워지면 공격 상태에 돌입합니다. 실제 판정에는 EnemyChaser의 " +
        "StopDistance(콜라이더가 맞닿는 거리)보다 작지 않도록 자동으로 보정된 값이 쓰인다 - 그렇지 않으면 " +
        "정지 거리에 도달하기 전까지는 공격 상태에 들어가지 못해 계속 파고들다가 플레이어를 밀어내게 된다.")]
    [SerializeField] private float attackRange = 0.8f;

    [Tooltip("공격 1회당 데미지")]
    [SerializeField] private int attackDamage = 1;

    [Tooltip("공격이 끝난 뒤 다음 공격까지의 쿨타임(초)")]
    [SerializeField] private float attackCooldown = 1.5f;

    [Tooltip("공격 애니메이션 재생 시간(초) - Mushroom_Attack 클립 길이(1초)에 맞춤")]
    [SerializeField] private float attackDuration = 1f;

    [Tooltip("공격 시작 후 실제 데미지가 들어가기까지의 지연(초) - 애니메이션 타격 타이밍에 맞춰 조절")]
    [SerializeField] private float hitDelay = 0.4f;

    private Animator animator;
    private EnemyChaser chaser;
    private Transform playerTransform;
    private Health playerHealth;

    private bool isAttacking;
    private float attackElapsed;
    private bool hasDealtDamageThisAttack;
    private float cooldownRemaining;

    // EnemyWeaponSwing 등 무기 비주얼이 공격 타이밍에 맞춰 휘두르기 동작을 재생할 수 있도록 노출.
    public bool IsAttacking => isAttacking;
    public float AttackProgress01 => attackDuration > 0f ? Mathf.Clamp01(attackElapsed / attackDuration) : 0f;
    public Transform PlayerTransform => playerTransform;

    // 디자이너가 지정한 attackRange가 EnemyChaser의 정지 거리보다 짧으면(즉 콜라이더가 닿기도 전에는
    // 공격 판정에 못 들어가면) 정지 거리까지 끌어올려서 쓴다 - "멈춰서 공격"이 항상 성립하도록 보장.
    private float EffectiveAttackRange => chaser != null ? Mathf.Max(attackRange, chaser.StopDistance) : attackRange;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        chaser = GetComponent<EnemyChaser>();
    }

    private void Start()
    {
        var playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            playerTransform = playerInventory.transform;
            playerHealth = playerInventory.GetComponent<Health>();
        }
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    private void Tick(float dt)
    {
        if (playerTransform == null)
            return;

        if (isAttacking)
        {
            attackElapsed += dt;

            if (!hasDealtDamageThisAttack && attackElapsed >= hitDelay)
            {
                hasDealtDamageThisAttack = true;
                float distance = Vector2.Distance(transform.position, playerTransform.position);
                if (playerHealth != null && distance <= EffectiveAttackRange)
                    playerHealth.TakeDamage(attackDamage);
            }

            if (attackElapsed >= attackDuration)
            {
                isAttacking = false;
                cooldownRemaining = attackCooldown;
                if (chaser != null)
                    chaser.enabled = true;
            }

            return;
        }

        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= dt;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= EffectiveAttackRange)
            BeginAttack();
    }

    private void BeginAttack()
    {
        isAttacking = true;
        attackElapsed = 0f;
        hasDealtDamageThisAttack = false;

        if (chaser != null)
            chaser.enabled = false;

        if (animator != null)
            animator.SetTrigger("Attack");
    }
}
