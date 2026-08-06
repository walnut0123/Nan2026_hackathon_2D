using UnityEngine;

// 보스가 소환하는 잡몹(1_Ink_A 등)용 저속 직선 추적. EnemyChaser/EnemyAIPathMover와 달리
// 장애물 회피나 물리 기반 이동을 전혀 쓰지 않는다 - Rigidbody2D 없이 transform.position을
// 직접 플레이어 쪽으로 옮기는 순수 직선 추적이라 "거의 직선으로 느리게 따라온다"는 요구에
// 정확히 맞는다. 근접 공격이 없는 원거리형 잡몹이므로 stopDistance까지만 다가가고 멈춘다.
//
// 무한 추적 대신 이동/정지를 번갈아 반복하는 사이클로 움직인다 - moveDuration 동안 이동하고
// stopDuration 동안은 완전히 멈춘다. 정지 상태에서는 위치를 전혀 갱신하지 않는다.
public class MobLinearChaser : MonoBehaviour
{
    [Header("추적 설정")]
    [Tooltip("플레이어를 향해 이동하는 속도 (저속 - 일반 근접몹보다 느리게)")]
    [SerializeField] private float moveSpeed = 0.7f;

    [Tooltip("이 거리까지 다가가면 멈춘다 - 원거리 공격형이라 완전히 밀착할 필요가 없다")]
    [SerializeField] private float stopDistance = 1f;

    [Header("이동/정지 사이클")]
    [Tooltip("한 번에 이동을 지속하는 시간(초)")]
    [SerializeField] private float moveDuration = 2f;
    [Tooltip("이동 후 완전히 멈춰있는 시간(초) - 이 동안은 위치가 전혀 갱신되지 않는다")]
    [SerializeField] private float stopDuration = 3f;

    private Transform targetTransform;

    // 사이클은 항상 이동 상태로 시작한다. cycleTimer는 현재 상태(이동/정지)에 머문 경과 시간이며,
    // 컴포넌트가 비활성화되는 동안(스폰 연출/자폭 시퀀스 등)에는 Update가 돌지 않으므로 자동으로
    // 그 시점에서 멈춘 채로 유지된다 - 별도의 일시정지 처리가 필요 없다.
    private bool isMoving = true;
    private float cycleTimer;

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
        if (targetTransform == null)
            return;

        TickCycle(Time.deltaTime);

        if (!isMoving)
            return;

        Vector3 toTarget = targetTransform.position - transform.position;
        if (toTarget.magnitude <= stopDistance)
            return;

        Vector3 direction = toTarget.normalized;
        transform.position += direction * (moveSpeed * Time.deltaTime);
    }

    private void TickCycle(float dt)
    {
        cycleTimer += dt;

        if (isMoving && cycleTimer >= moveDuration)
        {
            isMoving = false;
            cycleTimer = 0f;
        }
        else if (!isMoving && cycleTimer >= stopDuration)
        {
            isMoving = true;
            cycleTimer = 0f;
        }
    }
}
