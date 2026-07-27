using UnityEngine;

// 원거리 적의 총(Weapon_gun) 오브젝트가 항상 타겟을 겨누도록 위치/회전을 갱신한다.
// EnemyWeaponSwing과 같은 일반화된 반전(flipX)+회전 공식을 쓰지만, 총은 휘두르지 않으므로
// 스윙 오프셋은 없다 - 평소에도 공격 중에도 그냥 계속 타겟을 겨눈다.
[RequireComponent(typeof(SpriteRenderer))]
public class RangedWeaponAim : MonoBehaviour
{
    [Tooltip("타겟을 읽어올 RangedAttacker. 비워두면 부모에서 자동으로 찾음")]
    [SerializeField] private RangedAttacker rangedAttacker;

    [Tooltip("이 스프라이트를 회전/반전 없이 그대로 두었을 때 총구가 향하는 각도 " +
        "(0=+X/오른쪽, 90=+Y/위쪽, CCW 기준). Weapon_gun.png는 픽셀 측정 결과 정확히 180도(왼쪽)였다.")]
    [SerializeField] private float defaultFacingAngle = 180f;

    private Transform enemyTransform;
    private SpriteRenderer spriteRenderer;

    // 적 중심으로부터 총이 위치할 원의 반지름(월드 단위). EnemyWeaponSwing과 동일하게, 이 오브젝트를
    // 프리팹에서 실제로 배치해 둔 초기 위치로부터 자동 계산한다 - 직접 드래그해서 위치를 조절하면
    // 그 값이 그대로 인게임에 반영된다.
    private float orbitRadius;

    private void Awake()
    {
        if (rangedAttacker == null)
            rangedAttacker = GetComponentInParent<RangedAttacker>();

        enemyTransform = rangedAttacker != null ? rangedAttacker.transform : transform.parent;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (rangedAttacker == null)
            Debug.LogWarning("[RangedWeaponAim] RangedAttacker를 찾지 못했습니다. Inspector에서 직접 연결해주세요.");

        if (enemyTransform != null)
            orbitRadius = Vector2.Distance(transform.position, enemyTransform.position);
    }

    private void LateUpdate()
    {
        if (rangedAttacker == null || enemyTransform == null) return;

        Transform target = rangedAttacker.TargetTransform;
        if (target == null) return;

        Vector2 direction = (Vector2)(target.position - enemyTransform.position);
        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        // flipX는 defaultFacingAngle을 Y축 기준으로 거울 반사시킨다(각도 f -> 180-f). 목표 각도에
        // 도달하기 위해 반전 없이/반전해서 각각 필요한 회전량을 구해서, 더 적게 회전하는 쪽을 쓴다.
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        float unflippedFacing = defaultFacingAngle;
        float flippedFacing = 180f - defaultFacingAngle;

        float unflippedDelta = Mathf.DeltaAngle(unflippedFacing, baseAngle);
        float flippedDelta = Mathf.DeltaAngle(flippedFacing, baseAngle);

        bool useFlip = Mathf.Abs(flippedDelta) < Mathf.Abs(unflippedDelta);

        if (spriteRenderer != null)
            spriteRenderer.flipX = useFlip;

        float rotationZ = useFlip ? flippedDelta : unflippedDelta;

        transform.position = (Vector2)enemyTransform.position + direction * orbitRadius;
        transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
    }
}
