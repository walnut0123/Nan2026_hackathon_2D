using UnityEngine;

// EnemyAttacker의 공격 상태를 읽어 무기(Weapon) 오브젝트의 위치/회전을 갱신한다.
// Player의 WeaponAim과 같은 구조: 기존 전투 스크립트(EnemyAttacker)를 직접 수정하지 않고
// 별도 스크립트에서 참조해서 동작한다. 평소에는 플레이어 방향을 겨눈 채 대기하다가,
// 공격 중일 때만 대기 각도에서 휘두르는 각도로 추가 회전한다.
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyWeaponSwing : MonoBehaviour
{
    [Tooltip("공격 상태를 읽어올 EnemyAttacker. 비워두면 부모에서 자동으로 찾음")]
    [SerializeField] private EnemyAttacker enemyAttacker;

    [Tooltip("적 중심으로부터 무기가 위치할 원의 반지름 (월드 단위)")]
    [SerializeField] private float orbitRadius = 0.55f;

    [Tooltip("휘두르기 시작 각도 (대기 각도 기준 상대값, 도)")]
    [SerializeField] private float swingStartAngle = -70f;

    [Tooltip("휘두르기 종료 각도 (대기 각도 기준 상대값, 도)")]
    [SerializeField] private float swingEndAngle = 70f;

    private Transform enemyTransform;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (enemyAttacker == null)
            enemyAttacker = GetComponentInParent<EnemyAttacker>();

        enemyTransform = enemyAttacker != null ? enemyAttacker.transform : transform.parent;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (enemyAttacker == null)
            Debug.LogWarning("[EnemyWeaponSwing] EnemyAttacker를 찾지 못했습니다. Inspector에서 직접 연결해주세요.");
    }

    private void LateUpdate()
    {
        if (enemyAttacker == null || enemyTransform == null) return;

        Transform player = enemyAttacker.PlayerTransform;
        if (player == null) return;

        Vector2 direction = (Vector2)(player.position - enemyTransform.position);
        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        // Cardsoldier_weapon 아트는 기본적으로 왼쪽(-X)을 바라보게 그려져 있다. Player의 WeaponAim과
        // 동일한 방식으로, 오른쪽 절반을 겨눌 때는 좌우 반전(flipX) 후 짧게만 회전시켜 위아래가
        // 뒤집혀 보이는 것을 방지한다.
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bool aimingRight = baseAngle > -90f && baseAngle < 90f;

        if (spriteRenderer != null)
            spriteRenderer.flipX = aimingRight;

        float restAngle = aimingRight ? baseAngle : (baseAngle >= 0f ? baseAngle - 180f : baseAngle + 180f);

        // 공격 중일 때만 대기 각도에 휘두르기 오프셋을 더한다. 반전 상태에서는 스윙 방향도
        // 좌우로 뒤집어야 칼끝이 항상 플레이어 쪽을 향해 휘둘러진다.
        float swingOffset = 0f;
        if (enemyAttacker.IsAttacking)
            swingOffset = Mathf.Lerp(swingStartAngle, swingEndAngle, enemyAttacker.AttackProgress01);

        float mirror = aimingRight ? 1f : -1f;
        float rotationZ = restAngle + swingOffset * mirror;

        transform.position = (Vector2)enemyTransform.position + direction * orbitRadius;
        transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
    }
}
