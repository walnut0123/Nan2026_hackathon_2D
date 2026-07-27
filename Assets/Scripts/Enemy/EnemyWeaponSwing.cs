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

    [Tooltip("이 스프라이트를 회전/반전 없이 그대로 두었을 때, 칼끝(무기 아트의 '정면')이 향하는 각도 " +
        "(0=+X/오른쪽, 90=+Y/위쪽, CCW 기준). Cardsoldier_weapon.png는 픽셀 측정 결과 정확히 45도(오른쪽 위 대각선) " +
        "였다 - 이전 코드는 이 값을 180도(왼쪽)로 잘못 가정하고 있어서 칼끝이 대상을 향하지 않는 버그가 있었다. " +
        "다른 무기 아트를 쓸 경우 이 값만 맞게 바꿔주면 된다.")]
    [SerializeField] private float defaultFacingAngle = 45f;

    [Tooltip("휘두르기 시작 각도 (대기 각도 기준 상대값, 도)")]
    [SerializeField] private float swingStartAngle = -70f;

    [Tooltip("휘두르기 종료 각도 (대기 각도 기준 상대값, 도)")]
    [SerializeField] private float swingEndAngle = 70f;

    private Transform enemyTransform;
    private SpriteRenderer spriteRenderer;

    // 적 중심으로부터 무기가 위치할 원의 반지름(월드 단위). 별도 Inspector 수치가 아니라, 이 오브젝트를
    // 프리팹/씬에서 실제로 배치해 둔 초기 위치(Transform)로부터 자동 계산한다 - 그래야 Weapon의 위치를
    // 직접 드래그해서 조절하면 그 값이 그대로 인게임에 반영된다(이전에는 orbitRadius 필드가 따로 있어서
    // 프리팹에서 위치를 옮겨도 인게임에서는 반영되지 않는 문제가 있었다).
    private float orbitRadius;

    private void Awake()
    {
        if (enemyAttacker == null)
            enemyAttacker = GetComponentInParent<EnemyAttacker>();

        enemyTransform = enemyAttacker != null ? enemyAttacker.transform : transform.parent;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (enemyAttacker == null)
            Debug.LogWarning("[EnemyWeaponSwing] EnemyAttacker를 찾지 못했습니다. Inspector에서 직접 연결해주세요.");

        if (enemyTransform != null)
            orbitRadius = Vector2.Distance(transform.position, enemyTransform.position);
    }

    private void LateUpdate()
    {
        if (enemyAttacker == null || enemyTransform == null) return;

        Transform player = enemyAttacker.PlayerTransform;
        if (player == null) return;

        Vector2 direction = (Vector2)(player.position - enemyTransform.position);
        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        // flipX(좌우 반전)는 defaultFacingAngle을 Y축 기준으로 거울 반사시킨다(각도 f -> 180-f).
        // 목표 각도(baseAngle)에 도달하기 위해 반전 없이/반전해서 각각 필요한 회전량을 구해서, 더 적게
        // 회전하는(=180도 가까이 뒤집힐 일 없는) 쪽을 선택한다 - 위아래가 뒤집혀 보이는 것을 방지.
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        float unflippedFacing = defaultFacingAngle;
        float flippedFacing = 180f - defaultFacingAngle;

        float unflippedDelta = Mathf.DeltaAngle(unflippedFacing, baseAngle);
        float flippedDelta = Mathf.DeltaAngle(flippedFacing, baseAngle);

        bool useFlip = Mathf.Abs(flippedDelta) < Mathf.Abs(unflippedDelta);

        if (spriteRenderer != null)
            spriteRenderer.flipX = useFlip;

        // restAngle은 transform.rotation.z에 그대로 대입될 값이다 - 스프라이트 아트 자체가 이미
        // defaultFacingAngle(또는 반전 시 그 거울상) 방향으로 그려져 있으므로, 최종 보이는 각도는
        // "아트의 기본 방향 + transform 회전"이 된다. DeltaAngle이 그 필요한 회전량을 정확히 계산해준다.
        float restAngle = useFlip ? flippedDelta : unflippedDelta;

        // 공격 중일 때만 대기 각도에 휘두르기 오프셋을 더한다. 반전 상태에서는 스윙 방향도
        // 좌우로 뒤집어야 칼끝이 항상 플레이어 쪽을 향해 휘둘러진다.
        float swingOffset = 0f;
        if (enemyAttacker.IsAttacking)
            swingOffset = Mathf.Lerp(swingStartAngle, swingEndAngle, enemyAttacker.AttackProgress01);

        float mirror = useFlip ? 1f : -1f;
        float rotationZ = restAngle + swingOffset * mirror;

        transform.position = (Vector2)enemyTransform.position + direction * orbitRadius;
        transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
    }
}
