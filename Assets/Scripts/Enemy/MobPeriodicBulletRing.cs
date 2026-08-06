using UnityEngine;

// 보스가 소환하는 잡몹(1_Ink_A 등)용 탄막 발사. 보스의 BossAttackController.circleBulletPattern과
// 발상은 같지만(360도를 균등 분할해 투사체를 동시에 발사), 보스 인스턴스에 의존하지 않고 잡몹 스스로
// 완결적으로 동작하도록 별도 컴포넌트로 분리했다 - 보스가 죽거나 없어도, 심지어 이 잡몹이 다른 곳(다른
// 소환 시스템)에서 쓰이더라도 그대로 동작한다. 데미지 판정은 기존 RangedProjectile을 그대로 재사용한다.
//
// 발동 조건은 두 가지다 -
//   (A) 근접 공격: 플레이어가 근접 판정 거리(attackRange, 근접 유닛 EnemyAttacker와 동일한 의미의
//       필드) 안으로 "새로 들어오는 순간" 공격 모드로 전환해 즉시 발동한다(진입 엣지 트리거 - 이미
//       범위 안에 계속 머물러 있어도 매 프레임 재발동하지 않는다).
//   (B) 주기 공격: attackCooldown마다 범위와 무관하게 자동으로 발동한다.
// 두 트리거는 실행 동작(FireBurst)이 완전히 동일하고, 하나의 공유 쿨타임(cooldownRemaining)만 쓴다 -
// 어느 쪽이 먼저 발동하든 그 즉시 쿨타임이 가득 차므로, 두 조건이 겹쳐도 동시에/연속으로 두 번
// 발동하는 일은 없다. 근접 진입이 쿨타임을 다 채우기 전에 미리 소모시킬 수는 있지만(급습 반응),
// 그 경우에도 정확히 한 번만 발동하고 이후엔 다시 attackCooldown을 온전히 기다려야 한다.
public class MobPeriodicBulletRing : MonoBehaviour
{
    [Header("근접 판정")]
    [Tooltip("플레이어가 이 거리 안으로 새로 들어오면 공격 모드로 전환해 즉시 발동한다 (근접 유닛 EnemyAttacker의 attackRange와 동일한 의미)")]
    [SerializeField] private float attackRange = 0.8f;

    [Header("주기 공격 (B)")]
    [Tooltip("꺼두면 (B) 주기 공격은 발동하지 않는다 - (A) 근접 공격(진입 시 즉시 발동)은 이 설정과 " +
        "무관하게 그대로 작동한다. 밸런스 조정 중 주기 공격만 임시로 끄고 싶을 때 사용.")]
    [SerializeField] private bool enablePeriodicAttack = false;

    [Header("탄막 설정")]
    [Tooltip("발사할 투사체 프리팹 (RangedProjectile 컴포넌트를 포함해야 함, 예: Projectile_Ranged_Temp)")]
    [SerializeField] private GameObject bulletPrefab;

    [Tooltip("한 번에 원형으로 뿌려지는 투사체 개수")]
    [SerializeField] private int bulletCount = 8;

    [Tooltip("투사체 이동 속도(월드 단위/초)")]
    [SerializeField] private float bulletSpeed = 3f;

    [Tooltip("투사체 1발당 데미지")]
    [SerializeField] private int bulletDamage = 1;

    [Tooltip("근접 트리거(A)와 주기 트리거(B)가 공유하는 쿨타임(초) - 어느 쪽이 발동하든 이 시간만큼 " +
        "다시 채워지므로 두 트리거가 동시에/연속으로 겹쳐 발동하지 않는다")]
    [SerializeField] private float attackCooldown = 2.5f;

    private Transform targetTransform;
    private float cooldownRemaining;
    private bool wasInMeleeRange;

    private void Start()
    {
        var priorityTarget = FindObjectOfType<PriorityTarget>();
        if (priorityTarget != null)
        {
            targetTransform = priorityTarget.transform;
        }
        else
        {
            var playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory != null)
                targetTransform = playerInventory.transform;
        }

        // 소환되자마자 바로 쏘지 않고 첫 주기만큼 기다렸다가 발사 - 등장하자마자 사방으로
        // 터지는 것보다 "일정 주기로 운다"는 리듬감에 맞다.
        cooldownRemaining = attackCooldown;
    }

    private void Update()
    {
        if (bulletPrefab == null)
            return;

        cooldownRemaining -= Time.deltaTime;

        bool inMeleeRange = targetTransform != null
            && Vector2.Distance(transform.position, targetTransform.position) <= attackRange;
        bool justEnteredMeleeRange = inMeleeRange && !wasInMeleeRange;
        wasInMeleeRange = inMeleeRange;

        // (B) 주기 공격: enablePeriodicAttack이 켜져 있고 쿨타임이 자연히 다 되면 범위와 무관하게 발동.
        // (A) 근접 공격: enablePeriodicAttack 설정과 무관하게, 쿨타임이 아직 남아있어도 방금 근접
        // 판정 거리 안으로 들어온 순간이면 공격 모드로 전환해 즉시 발동한다 - 계속 범위 안에
        // 머물러 있어도 진입 순간에만 한 번 발동하므로 난사되지 않는다.
        bool periodicReady = enablePeriodicAttack && cooldownRemaining <= 0f;
        if (periodicReady || justEnteredMeleeRange)
        {
            cooldownRemaining = attackCooldown;
            FireBurst();
        }
    }

    /// <summary>탄막 한 발(bulletCount개)을 즉시 발사한다. 쿨타임과 무관하게 즉시 발동 - 자폭
    /// 시퀀스의 최후 탄막(MineSelfDestructSequence)처럼 외부에서 강제로 한 번 쏘게 할 때 사용한다.</summary>
    public void FireBurst()
    {
        if (bulletPrefab == null)
            return;

        int count = Mathf.Max(1, bulletCount);
        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i;
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            GameObject bulletInstance = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
            var projectile = bulletInstance.GetComponent<RangedProjectile>();
            if (projectile != null)
                projectile.Initialize(direction, bulletDamage, bulletSpeed);
        }
    }
}
