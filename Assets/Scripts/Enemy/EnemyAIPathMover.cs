using UnityEngine;
using Pathfinding;

/// <summary>EnemyChaser를 A* Pathfinding Project 기반 이동으로 교체한 버전. Seeker/AILerp가 그래프
/// 경로를 따라 벽/장애물을 자동으로 우회하므로 EnemyChaser의 휘스커 회피 로직이 필요 없다.
///
/// AIPath(조향+가속도 물리 모델) 대신 AILerp를 쓴다 - AIPath는 매 프레임 "지금 속도에서 목표 방향으로
/// 얼마나 가속할지"를 계산하는 자동차형 스티어링이라, 장애물을 끼고 코너를 여러 번 꺾어야 하는 경로에서는
/// 꺾일 때마다 다시 가속해야 해서 체감상 느려지고 벽에 비비는 것처럼 보였다. AILerp는 가속도 개념이
/// 아예 없이 "경로 위 진행 거리 += deltaTime * speed"로 그냥 정속으로 경로를 따라 걷기만 하므로, 코너에서도
/// 속도가 줄지 않고 항상 일정하게 유지된다.
///
/// EnemyAttacker/RangedAttacker/EnemyAnimator가 기대하는 EnemyChaser의 공개 표면(StopDistance,
/// enabled를 통한 이동 정지/재개)은 그대로 유지한다.</summary>
[RequireComponent(typeof(Seeker), typeof(AILerp), typeof(AIDestinationSetter))]
public class EnemyAIPathMover : MonoBehaviour
{
    [Header("추적 설정")]
    [Tooltip("이 거리 밖의 타겟은 추적하지 않음 (0 이하면 무제한 추적)")]
    [SerializeField] private float chaseRange = 15f;

    [Tooltip("StopDistance(콜라이더가 맞닿는 거리) 위에 추가로 두는 여유 간격")]
    [SerializeField] private float stopGap = 0.05f;

    private AILerp aiLerp;
    private AIDestinationSetter destinationSetter;
    private CircleCollider2D myCollider;
    private Transform targetTransform;
    private CircleCollider2D targetCollider;
    private float stopDistance;

    // EnemyAttacker.EffectiveAttackRange가 기대하던 것과 동일한 값 - "여기서 더 다가가지 않고 멈춘다"는
    // 의미. AILerp에는 AIPath의 endReachedDistance 같은 내장 정지 거리 개념이 없어서 직접 들고 있다가
    // Update()에서 isStopped 판정에 쓴다.
    public float StopDistance => stopDistance;

    private void Awake()
    {
        aiLerp = GetComponent<AILerp>();
        destinationSetter = GetComponent<AIDestinationSetter>();
        myCollider = GetComponent<CircleCollider2D>();

        // 탑다운 스프라이트 회전은 EnemyAnimator(SpriteRenderer.flipX)가 담당하므로, AILerp가
        // Transform을 이동 방향으로 회전시키지 않도록 끈다.
        aiLerp.enableRotation = false;

        // 우리 Grid Graph는 2D용으로 (-90,270,90) 회전되어 있어서 월드 Z축이 평면의 법선(off-plane)
        // 축이 된다 - YAxisForward로 두면 월드 Y(우리 평면에 실제로 놓여있는 축)를 기준으로 삼는다.
        // (AILerp는 AIPath와 달리 이 값이 이동 자체에는 영향을 주지 않고 회전 계산에만 쓰이지만,
        // enableRotation을 나중에 켤 수도 있으니 일관되게 맞춰둔다.)
        aiLerp.orientation = Pathfinding.OrientationMode.YAxisForward;
    }

    private void Start()
    {
        var priorityTarget = FindObjectOfType<PriorityTarget>();
        if (priorityTarget != null)
        {
            targetTransform = priorityTarget.transform;
            targetCollider = priorityTarget.GetComponent<CircleCollider2D>();
        }
        else
        {
            var playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory != null)
            {
                targetTransform = playerInventory.transform;
                targetCollider = playerInventory.GetComponent<CircleCollider2D>();
            }
            else
            {
                Debug.LogWarning("[EnemyAIPathMover] 씬에서 타겟(PriorityTarget/PlayerInventory)을 찾을 수 없습니다.");
            }
        }

        destinationSetter.target = targetTransform;
        UpdateStopDistance();
    }

    private void UpdateStopDistance()
    {
        float myRadius = myCollider != null ? myCollider.radius * transform.lossyScale.x : 0f;
        float otherRadius = (targetCollider != null && targetTransform != null)
            ? targetCollider.radius * targetTransform.lossyScale.x
            : 0f;
        stopDistance = myRadius + otherRadius + stopGap;
    }

    // 테이블/벽처럼 직선상에 장애물이 있으면 실제로 걸어야 하는 경로 거리가 직선거리보다 훨씬 길다.
    // 직선거리로만 chaseRange를 판정하면, 장애물 반대편(경로상으로는 충분히 가까운)에 있는데도
    // "너무 멀다"고 오판해서 추적을 멈추는 문제가 생긴다 - 경로가 이미 잡혀 있으면 AILerp가 계산한
    // 실제 남은 경로 거리(remainingDistance)를 쓰고, 아직 경로가 없을 때만 직선거리로 대체한다.
    // StopDistance 안쪽으로 들어오면 그 자리에서 멈춘다(AIPath의 endReachedDistance를 직접 구현).
    private void Update()
    {
        if (targetTransform == null || aiLerp == null)
            return;

        float distance = aiLerp.hasPath
            ? aiLerp.remainingDistance
            : Vector2.Distance(transform.position, targetTransform.position);

        aiLerp.isStopped = (chaseRange > 0f && distance > chaseRange) || distance <= stopDistance;
    }

    // EnemyAttacker/RangedAttacker가 예전 EnemyChaser에 하던 것과 똑같이 이 컴포넌트의 enabled를
    // 꺼서 공격 중 이동을 멈춘다 - AILerp.canMove로 그대로 전달한다.
    private void OnEnable()
    {
        if (aiLerp != null)
            aiLerp.canMove = true;
    }

    private void OnDisable()
    {
        if (aiLerp != null)
            aiLerp.canMove = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (chaseRange > 0f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, chaseRange);
        }
    }
}
