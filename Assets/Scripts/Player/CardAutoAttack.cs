using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardAutoAttack : MonoBehaviour
{
    [Header("공격 설정")]
    [Tooltip("자동 공격 기능 On/Off 스위치")]
    public bool isAutoAttackEnabled = true;

    [Tooltip("적 인식 및 공격 가능 거리 (Scene 뷰에서 기즈모로 확인 가능)")]
    public float attackRange = 10.0f;

    [Tooltip("공격 속도 (초 단위)")]
    public float attackCooldown = 1.0f;

    [Header("투사체 설정")]
    [Tooltip("발사할 카드 프리팹 리스트 (등록된 52종 카드 프리팹)")]
    public List<GameObject> cardPrefabs = new List<GameObject>();

    [Tooltip("카드가 생성될 위치 (지정하지 않으면 플레이어 위치)")]
    public Transform firePoint;

    [Tooltip("한 번 공격할 때 연속으로 던지는 카드 장수")]
    public int cardsPerBurst = 5;

    [Tooltip("연속으로 던지는 카드 사이의 딜레이 (초)")]
    public float cardThrowDelay = 0.2f;

    [Header("탐색 설정")]
    [Tooltip("적 감지 주기 (초 단위, 성능 최적화를 위해 매 프레임 탐색하지 않음)")]
    public float searchInterval = 0.2f;

    // 현재 타겟으로 지정된 적
    private Transform currentTarget;
    public Transform CurrentTarget => currentTarget;

    // 플레이어가 화면을 클릭해서 직접 지정한 우선 타겟 (위험도 높은 적을 수동으로 집중 공격하기 위함).
    // 사거리 밖으로 나가도 유지되며(다시 들어오면 재우선), 죽거나(파괴됨) 콜라이더가 꺼지면(사망 연출)
    // 자동으로 해제되어 일반 자동 타겟팅으로 돌아간다. 빈 곳을 클릭하면 즉시 해제된다.
    private Transform manualTarget;
    private Collider2D manualTargetCollider;

    // Physics2D.OverlapCircleNonAlloc 최적화용 버퍼
    private Collider2D[] hitColliders = new Collider2D[20];
    private float searchTimer = 0f;
    private float attackTimer = 0f;

    /// <summary>화면 클릭으로 지정된 우선 타겟을 설정한다. null을 넘기면 해제(자동 타겟팅으로 복귀).</summary>
    public void SetManualTarget(Transform target)
    {
        manualTarget = target;
        manualTargetCollider = target != null ? target.GetComponent<Collider2D>() : null;

        if (manualTarget != null)
            Debug.Log($"[CardAutoAttack] 우선 타겟 지정: {manualTarget.name}");
        else
            Debug.Log("[CardAutoAttack] 우선 타겟 해제 - 자동 타겟팅으로 복귀");
    }

    private void Update()
    {
        if (!isAutoAttackEnabled)
        {
            if (currentTarget != null)
            {
                currentTarget = null;
                Debug.Log("[CardAutoAttack] 자동 공격 비활성화 - 타겟 해제됨");
            }
            return;
        }

        // 1. 지정한 주기마다 주변 적 탐색
        searchTimer += Time.deltaTime;
        if (searchTimer >= searchInterval)
        {
            searchTimer = 0f;
            FindNearestTarget();
        }

        // 2. 타겟이 존재할 경우 공격 쿨타임 계산 및 발사
        if (currentTarget != null)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackCooldown)
            {
                attackTimer = 0f;
                ShootCard();
            }
        }
        else
        {
            attackTimer = 0f;
        }
    }

    private void FindNearestTarget()
    {
        // 수동 지정 타겟이 죽지 않고(콜라이더 활성) 사거리 안에 있으면 자동 탐색보다 우선한다.
        // 사거리를 벗어나면 그동안은 자동 타겟팅으로 넘어가되, manualTarget 자체는 계속 기억해뒀다가
        // 다시 사거리에 들어오면 재우선한다 - 죽거나(파괴/콜라이더 비활성) 빈 곳을 클릭해야만 완전히 해제된다.
        bool manualTargetAlive = manualTarget != null && manualTargetCollider != null && manualTargetCollider.enabled;
        if (!manualTargetAlive && manualTarget != null)
        {
            manualTarget = null;
            manualTargetCollider = null;
        }

        if (manualTargetAlive)
        {
            float distanceToManual = Vector3.Distance(transform.position, manualTarget.position);
            if (distanceToManual <= attackRange)
            {
                if (currentTarget != manualTarget)
                {
                    currentTarget = manualTarget;
                    Debug.Log($"[CardAutoAttack] 우선 타겟 공격 중: {currentTarget.name} (거리: {distanceToManual:F2}m)");
                }
                return;
            }
        }

        int numFound = Physics2D.OverlapCircleNonAlloc(transform.position, attackRange, hitColliders);

        Transform nearestEnemy = null;
        float shortestDistance = Mathf.Infinity;

        for (int i = 0; i < numFound; i++)
        {
            Collider2D col = hitColliders[i];

            if (col != null && col.CompareTag("Enemy"))
            {
                float distanceToEnemy = Vector3.Distance(transform.position, col.transform.position);

                if (distanceToEnemy < shortestDistance)
                {
                    shortestDistance = distanceToEnemy;
                    nearestEnemy = col.transform;
                }
            }
        }

        if (currentTarget != nearestEnemy)
        {
            currentTarget = nearestEnemy;

            if (currentTarget != null)
            {
                Debug.Log($"[CardAutoAttack] 현재 감지된 적: {currentTarget.name} (거리: {shortestDistance:F2}m)");
            }
            else
            {
                Debug.Log("[CardAutoAttack] 범위 내에 적이 없습니다.");
            }
        }
    }

    /// <summary>
    /// 한 번의 공격에 카드 여러 장을 약간의 시간차를 두고 연속으로 던집니다.
    /// </summary>
    private void ShootCard()
    {
        if (cardPrefabs == null || cardPrefabs.Count == 0)
        {
            Debug.LogWarning("[CardAutoAttack] Card Prefabs 리스트가 비어 있거나 할당되지 않았습니다.");
            return;
        }

        StartCoroutine(ShootCardBurst());
    }

    private IEnumerator ShootCardBurst()
    {
        for (int i = 0; i < cardsPerBurst; i++)
        {
            ShootSingleCard();

            if (i < cardsPerBurst - 1)
                yield return new WaitForSeconds(cardThrowDelay);
        }
    }

    /// <summary>
    /// 등록된 카드 프리팹 리스트 중 하나를 랜덤으로 선택해 발사합니다.
    /// </summary>
    private void ShootSingleCard()
    {
        if (currentTarget == null)
            return;

        int randomIndex = Random.Range(0, cardPrefabs.Count);
        GameObject selectedPrefab = cardPrefabs[randomIndex];

        if (selectedPrefab == null) return;

        Vector3 spawnPosition = (firePoint != null) ? firePoint.position : transform.position;

        GameObject cardInstance = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);

        CardProjectile projectile = cardInstance.GetComponent<CardProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(currentTarget);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (currentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}
