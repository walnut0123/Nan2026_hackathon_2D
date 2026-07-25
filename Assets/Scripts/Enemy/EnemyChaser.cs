using UnityEngine;

/// <summary>Chases the player on the XY plane. Finds the player via PlayerInventory at Start,
/// matching the codebase's convention of locating the player by component (see CameraFollow)
/// rather than tag lookup. Moves the enemy's existing kinematic Rigidbody2D via MovePosition.</summary>
public class EnemyChaser : MonoBehaviour
{
    [Header("추적 설정")]
    [Tooltip("플레이어를 향해 이동하는 속도")]
    [SerializeField] private float moveSpeed = 1.5f;

    [Tooltip("이 거리 밖의 플레이어는 감지하지 않음 (0 이하면 무제한 감지)")]
    [SerializeField] private float chaseRange = 8f;

    [Tooltip("StopDistance(콜라이더가 맞닿는 거리) 위에 추가로 두는 여유 간격 - 스프라이트/스케일이 바뀌어도 항상 이 값만큼은 떨어져서 멈춘다")]
    [SerializeField] private float stopGap = 0.05f;

    private Rigidbody2D rb;
    private CircleCollider2D myCollider;
    private Transform playerTransform;
    private CircleCollider2D playerCollider;

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
        var playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
        {
            playerTransform = playerInventory.transform;
            playerCollider = playerInventory.GetComponent<CircleCollider2D>();
        }
        else
        {
            Debug.LogWarning("[EnemyChaser] 씬에서 PlayerInventory(플레이어)를 찾을 수 없습니다.");
        }
    }

    private void FixedUpdate()
    {
        if (playerTransform == null || rb == null)
            return;

        Vector2 toPlayer = (Vector2)playerTransform.position - rb.position;
        float distance = toPlayer.magnitude;

        if (chaseRange > 0f && distance > chaseRange)
            return;

        if (distance <= StopDistance)
            return;

        Vector2 direction = toPlayer / distance;
        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
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
