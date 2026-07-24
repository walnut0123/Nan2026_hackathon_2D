using UnityEngine;

public class CardProjectile : MonoBehaviour
{
    // 이동 속도 및 판정 범위
    private float speed = 15.0f;
    private float hitThreshold = 0.2f;

    // 타겟 조준 높이 보정 (2D에서는 Y축 = 화면상 위쪽)
    private float targetHeightOffset = 0f;

    // 카드 스프라이트는 세로(위쪽이 카드 앞면 상단)로 그려져 있어, 진행 방향 계산과
    // 90도 어긋난다. 이 오프셋으로 보정해서 카드가 옆면이 아닌 진행 방향을 향하도록 한다.
    [SerializeField] private float rotationOffsetDegrees = -90f;

    // 데미지 = 기본 데미지 + 현재 보유 중인 카드 5칸의 합(CardInventory.TotalCardValue).
    [SerializeField] private int baseDamage = 5;

    private Transform targetTransform;
    private Vector3 targetPosition;
    private bool isInitialized = false;

    /// <summary>
    /// 카드 발사 초기화 함수
    /// </summary>
    public void Initialize(Transform target)
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        targetTransform = target;
        UpdateTargetPosition();

        // 초기 생성 시 회전 설정
        RotateToTarget();

        // 필드에 놓인 카드에는 ItemBob(위아래 흔들림)이 붙어있는데, 던져진 투사체에도 그대로
        // 남아있으면 매 프레임 위치를 되돌려버려 MoveTowards 이동이 무효화된다(카드가 제자리에서
        // 흔들리기만 하고 적에게 날아가지 않음). 투사체로 초기화되는 순간 꺼서 이동을 방해하지 않게 한다.
        var bob = GetComponent<ItemBob>();
        if (bob != null)
            bob.enabled = false;

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized) return;

        // 1. 타겟 위치 최신화
        UpdateTargetPosition();

        // 2. 진행 방향(적 방향)으로 회전
        RotateToTarget();

        // 3. 적을 향해 이동
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // 4. 도착 여부 확인
        if (HasReachedTarget())
        {
            OnHitTarget();
        }
    }

    /// <summary>
    /// 명중 판정: 적의 중심점이 아니라, 화면에 보이는 스프라이트 크기와 일치하는 실제
    /// Collider2D 표면까지의 거리로 판정합니다(돈스타브 방식). 이렇게 하면 눈에 보이는 스프라이트에
    /// 카드가 닿는 순간 곧바로 명중 처리되며, 덩치가 큰 적일수록 중심까지 파고들 필요 없이
    /// 화면상 접촉 지점에서 바로 맞습니다.
    /// </summary>
    private bool HasReachedTarget()
    {
        // 타겟이 (다른 카드에 맞아) 이미 파괴된 경우 - 더 쫓아갈 대상이 없으니 바로
        // "도착"으로 처리해서 OnHitTarget()에서 정리(Destroy)되게 한다. 이 체크가 없으면
        // 아래 GetComponent 호출에서 매 프레임 예외가 나서 이 카드가 영원히 허공을 떠돌게 된다.
        if (targetTransform == null)
            return true;

        var targetCollider = targetTransform.GetComponent<Collider2D>();
        if (targetCollider != null)
        {
            Vector2 closestPoint = targetCollider.ClosestPoint(transform.position);
            return Vector2.Distance(transform.position, closestPoint) <= hitThreshold;
        }

        return Vector3.Distance(transform.position, targetPosition) <= hitThreshold;
    }

    private void UpdateTargetPosition()
    {
        if (targetTransform != null)
        {
            // 2D 평면(XY) 이동이므로 타겟의 실제 XY 위치를 그대로 조준한다
            Vector3 rawTargetPos = targetTransform.position;
            targetPosition = new Vector3(rawTargetPos.x, rawTargetPos.y + targetHeightOffset, transform.position.z);
        }
    }

    /// <summary>
    /// 카드가 진행 방향(적 방향)을 바라보도록 Z축만 회전시킵니다 (2D 평면 회전).
    /// </summary>
    private void RotateToTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffsetDegrees;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    private void OnHitTarget()
    {
        if (targetTransform != null)
        {
            var damageable = targetTransform.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int cardValue = CardInventory.Instance != null ? CardInventory.Instance.TotalCardValue : 0;
                int damage = baseDamage + cardValue;

                damageable.TakeDamage(damage);
                Debug.Log($"[CardProjectile] {targetTransform.name}에게 {damage} 데미지 (기본 {baseDamage} + 카드 합 {cardValue})");
                DamageTextDisplay.ShowDamage(damage, targetTransform);
            }
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 에디터 뷰포트 창에서 카드와 적 사이의 추적 경로를 노란색 선으로 표시합니다.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!isInitialized || targetTransform == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetPosition);
        Gizmos.DrawWireSphere(targetPosition, 0.1f);
    }
}
