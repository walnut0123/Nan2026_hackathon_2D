using UnityEngine;

// CardAutoAttack이 찾아낸 타겟 방향으로 총(Weapon) 오브젝트의 위치/회전을 갱신한다.
// 플레이어를 중심으로 한 원(orbitRadius) 위에 총을 배치해서 "총구가 적을 겨눈다"는 느낌을 준다.
// 적이 감지되지 않은 동안에는 비주얼을 꺼서, 공격 중일 때만 총이 보이도록 한다.
public class WeaponAim : MonoBehaviour
{
    [Tooltip("타겟을 읽어올 CardAutoAttack. 비워두면 부모에서 자동으로 찾음")]
    [SerializeField] private CardAutoAttack cardAutoAttack;

    [Tooltip("플레이어 중심으로부터 총이 위치할 원의 반지름 (월드 단위)")]
    [SerializeField] private float orbitRadius = 1.4f;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (cardAutoAttack == null)
            cardAutoAttack = GetComponentInParent<CardAutoAttack>();

        playerTransform = cardAutoAttack != null ? cardAutoAttack.transform : transform.parent;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (cardAutoAttack == null)
            Debug.LogWarning("[WeaponAim] CardAutoAttack을 찾지 못했습니다. Inspector에서 직접 연결해주세요.");

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    private void LateUpdate()
    {
        if (cardAutoAttack == null || playerTransform == null) return;

        Transform target = cardAutoAttack.CurrentTarget;

        if (spriteRenderer != null)
            spriteRenderer.enabled = target != null;

        if (target == null) return;

        Vector2 direction = (Vector2)(target.position - playerTransform.position);
        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        // 1. 위치: 플레이어를 중심으로 한 원 위, 적 방향에 해당하는 지점
        transform.position = (Vector2)playerTransform.position + direction * orbitRadius;

        // 2. 회전: 총구가 적을 향하도록.
        // Weapon_gun 아트는 기본적으로 왼쪽(-X)을 바라보게 그려져 있다. 오른쪽 절반을 겨눌 때
        // 그대로 180도 가까이 돌리면 총이 위아래로 뒤집혀 보이므로, 그 구간에서는 좌우 반전(flipX)한
        // 뒤 짧게만 회전시켜 항상 회전량이 90도 이내가 되도록 한다.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bool aimingRight = angle > -90f && angle < 90f;

        if (spriteRenderer != null)
            spriteRenderer.flipX = aimingRight;

        float rotationZ = aimingRight ? angle : (angle >= 0f ? angle - 180f : angle + 180f);
        transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
    }
}
