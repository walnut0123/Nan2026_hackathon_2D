using UnityEngine;

/// <summary>Chases the player on the XY plane. Finds the player via PlayerInventory at Start,
/// matching the codebase's convention of locating the player by component (see CameraFollow)
/// rather than tag lookup. Moves the enemy's existing Dynamic Rigidbody2D via velocity, with
/// simple whisker-based steering to route around walls/obstacles blocking the direct path.</summary>
public class EnemyChaser : MonoBehaviour
{
    [Header("추적 설정")]
    [Tooltip("플레이어를 향해 이동하는 속도")]
    [SerializeField] private float moveSpeed = 1.5f;

    [Tooltip("이 거리 밖의 플레이어는 감지하지 않음 (0 이하면 무제한 감지)")]
    [SerializeField] private float chaseRange = 8f;

    [Tooltip("StopDistance(콜라이더가 맞닿는 거리) 위에 추가로 두는 여유 간격 - 스프라이트/스케일이 바뀌어도 항상 이 값만큼은 떨어져서 멈춘다")]
    [SerializeField] private float stopGap = 0.05f;

    [Header("장애물 회피")]
    [Tooltip("플레이어 방향으로 이 거리 안에 장애물(벽/부술 수 있는 오브젝트 등 Rigidbody2D가 없는 정적 콜라이더)이 있으면 우회 방향을 찾는다")]
    [SerializeField] private float obstacleLookAhead = 0.6f;

    [Tooltip("회피 방향을 한 번 고르면 최소 이 시간(초) 동안은 유지한다 - 없으면 매 FixedUpdate마다 플레이어 방향으로 재계산하면서 다시 막혀 제자리에서 진동(vibration)하게 된다")]
    [SerializeField] private float avoidCommitDuration = 0.25f;

    // 정면이 막혔을 때 순서대로 시도할 우회 각도(도) - 앞쪽 각도부터 시도해서 가장 덜 돌아가는 길을 우선한다.
    // ±150도까지만 있으면 뒤쪽(오목한 코너 안쪽 등 전/측면이 다 막히고 왔던 방향만 뚫린 경우)으로는
    // 절대 탈출할 수 없어 벽을 미는 채로 완전히 멈춰버린다 - 그래서 거의 정반대 방향까지 커버한다.
    private static readonly float[] AvoidAngles =
        { 35f, -35f, 60f, -60f, 90f, -90f, 120f, -120f, 150f, -150f, 165f, -165f, 180f };

    private Rigidbody2D rb;
    private CircleCollider2D myCollider;
    private Transform playerTransform;
    private CircleCollider2D playerCollider;

    // 회피 방향 커밋 상태 - 코너를 완전히 돌아나갈 때까지 같은 회피 방향을 유지하기 위함.
    private Vector2 committedDirection;
    private float avoidCommitTimer;

    // CircleCastNonAlloc용 재사용 버퍼 - FixedUpdate마다(적마다) GC 할당이 생기지 않도록.
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    // 킨네마틱 몸체가 플레이어를 밀어붙이는 문제의 근본 원인은, 예전 stopDistance가 실제
    // 콜라이더 반지름 합(스케일 반영)보다 훨씬 작은 고정값이었다는 것 - 그래서 콜라이더끼리 겹칠
    // 때까지 계속 다가갔었다. 이제는 실제 반지름 합으로 매번 계산해서, 스케일/콜라이더 크기가
    // 바뀌어도 항상 "닿기 직전"에 멈추도록 한다.
    public float StopDistance
    {
        get
        {
            float myRadius = myCollider != null ? myCollider.radius * transform.lossyScale.x : 0f;
            float otherRadius = (playerCollider != null && playerTransform != null)
                ? playerCollider.radius * playerTransform.lossyScale.x
                : 0f;
            return myRadius + otherRadius + stopGap;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<CircleCollider2D>();
    }

    private void Start()
    {
        var priorityTarget = FindObjectOfType<PriorityTarget>();
        if (priorityTarget != null)
        {
            playerTransform = priorityTarget.transform;
            playerCollider = priorityTarget.GetComponent<CircleCollider2D>();
            return;
        }

        var playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            playerTransform = playerInventory.transform;
            playerCollider = playerInventory.GetComponent<CircleCollider2D>();
        }
        else
        {
            Debug.LogWarning("[EnemyChaser] 씬에서 타겟(PriorityTarget/PlayerInventory)을 찾을 수 없습니다.");
        }
    }

    private void OnDisable()
    {
        // Dynamic 강체는 Kinematic과 달리 마지막으로 넣어준 velocity가 그대로 유지된다 -
        // EnemyAttacker/RangedAttacker가 공격 중 이 컴포넌트를 꺼도 미끄러지듯 계속 밀려나지
        // 않도록, 비활성화되는 순간 반드시 멈춰 세운다.
        if (rb != null)
            rb.velocity = Vector2.zero;
        avoidCommitTimer = 0f;
    }

    // 예전에는 Kinematic 강체 + MovePosition으로 이동했다 - Kinematic MovePosition은 다른
    // 콜라이더와의 충돌을 물리 엔진이 아예 처리하지 않으므로(트리거 콜백만 발생), 벽/오브젝트를
    // 그대로 뚫고 지나가는 문제가 있었다. 지금은 Rigidbody2D를 Dynamic으로 바꾸고(중력 0,
    // 회전 고정) Player의 AgentMover와 동일하게 velocity를 통해서만 이동시킨다 - 이러면 Unity의
    // 2D 물리 충돌 처리(Solver)가 정지 오브젝트와의 충돌을 자동으로 막아준다.
    private void FixedUpdate()
    {
        if (playerTransform == null || rb == null)
            return;

        Vector2 toPlayer = (Vector2)playerTransform.position - rb.position;
        float distance = toPlayer.magnitude;

        if ((chaseRange > 0f && distance > chaseRange) || distance <= StopDistance)
        {
            rb.velocity = Vector2.zero;
            avoidCommitTimer = 0f;
            return;
        }

        Vector2 direction = toPlayer / distance;
        rb.velocity = ChooseSteerDirection(direction) * moveSpeed;
    }

    // 직선 추적만으로는 플레이어와의 사이에 벽(또는 다른 정적 장애물)이 끼면 그 벽을 미는 채로
    // 멈춰버린다 - 정면 CircleCast를 쏴서 막혀 있으면 각도를 늘려가며 뚫린 방향을 찾는다.
    // 회피 방향을 찾자마자 매 프레임 목표 방향(플레이어 쪽)으로 다시 계산해버리면, 오목한 코너
    // 안에서는 "회피 방향 한 틱 이동 -> 살짝 뚫려 보여서 플레이어 방향 재시도 -> 다시 막힘"이
    // 반복되며 순 이동거리 없이 제자리에서 진동한다. 그래서 한 번 고른 회피 방향은
    // avoidCommitDuration 동안 유지해 코너를 실제로 돌아나갈 시간을 준다.
    private Vector2 ChooseSteerDirection(Vector2 desired)
    {
        if (avoidCommitTimer > 0f)
        {
            avoidCommitTimer -= Time.fixedDeltaTime;

            // 커밋 중이라도 직선 경로가 열렸으면 즉시 복귀 - 코너를 다 돌았다는 뜻이므로 굳이
            // 남은 커밋 시간을 다 쓰면서 돌아갈 필요가 없다.
            if (!IsBlocked(desired))
            {
                avoidCommitTimer = 0f;
                return desired;
            }

            if (!IsBlocked(committedDirection))
                return committedDirection;

            // 커밋해둔 방향마저 막혔다 - 코너 형태가 바뀐(또는 더 깊이 들어온) 것이므로 재탐색.
        }

        Vector2 result = FindUnblockedDirection(desired);
        if (result != desired)
        {
            committedDirection = result;
            avoidCommitTimer = avoidCommitDuration;
        }
        else
        {
            avoidCommitTimer = 0f;
        }

        return result;
    }

    private Vector2 FindUnblockedDirection(Vector2 desired)
    {
        if (!IsBlocked(desired))
            return desired;

        foreach (float angle in AvoidAngles)
        {
            Vector2 candidate = Rotate(desired, angle);
            if (!IsBlocked(candidate))
                return candidate;
        }

        // 사방이 다 막힘 - 원래 방향을 유지해서 벽에 붙어 대기(다음 프레임에 다시 시도).
        return desired;
    }

    // Rigidbody2D가 없는 콜라이더(벽, Breakable 등 정적 오브젝트)만 장애물로 취급한다 - 플레이어와
    // 다른 적은 모두 Dynamic Rigidbody2D를 가지고 있으므로 이 캐스트에서 자연스럽게 제외된다.
    private bool IsBlocked(Vector2 direction)
    {
        float radius = myCollider != null ? myCollider.radius * transform.lossyScale.x : 0.1f;
        int hitCount = Physics2D.CircleCastNonAlloc(rb.position, radius, direction, castHits, obstacleLookAhead);

        for (int i = 0; i < hitCount; i++)
        {
            var hit = castHits[i];
            if (hit.collider == null || hit.collider == myCollider || hit.collider.isTrigger)
                continue;
            if (hit.rigidbody != null)
                continue;

            return true;
        }

        return false;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
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
