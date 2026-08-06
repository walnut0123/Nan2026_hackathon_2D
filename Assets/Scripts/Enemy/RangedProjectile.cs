using UnityEngine;

// EnemyAttacker(근접, 즉시 판정)와 달리 실제로 날아가는 투사체 - 데미지는 오직 이 오브젝트의
// Collider2D(트리거) 충돌로만 발생한다. "Enemy" 태그(다른 적, 그리고 쏜 당사자 자신도 Enemy 태그)는
// 명시적으로 제외해서 플레이어/허수아비(Untagged, 둘 다 Health 보유)에게만 데미지가 들어가도록 한다.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class RangedProjectile : MonoBehaviour
{
    [Tooltip("이동 속도 (월드 단위/초)")]
    [SerializeField] private float speed = 6f;

    [Tooltip("이 시간(초)이 지나면 아무것도 맞추지 못해도 자동 파괴 (허공으로 날아가는 것 방지)")]
    [SerializeField] private float maxLifetime = 5f;

    private Vector2 direction;
    private int damage;
    private float elapsed;

    /// <summary>발사 시점에 방향과 데미지를 주입한다. overrideSpeed를 넘기면 프리팹 기본 속도 대신 사용한다.</summary>
    public void Initialize(Vector2 fireDirection, int attackDamage, float? overrideSpeed = null)
    {
        direction = fireDirection.sqrMagnitude > 0.0001f ? fireDirection.normalized : Vector2.right;
        damage = attackDamage;
        if (overrideSpeed.HasValue)
            speed = overrideSpeed.Value;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        elapsed += Time.deltaTime;
        if (elapsed >= maxLifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 다른 적이나 쏜 당사자 자신(둘 다 tag=Enemy)에게는 맞지 않는다. Breakable(항아리 등 필드
        // 오브젝트)도 제외한다 - 몬스터/보스의 원거리 공격이 아니라 오직 플레이어의 공격에만
        // 파괴되어야 한다(BossAttackController/BossOrbHit의 데미지 판정과 동일한 규칙).
        if (other.CompareTag("Enemy") || other.CompareTag("Breakable"))
            return;

        var health = DamageUtil.ResolveHealth(other);
        if (health != null)
        {
            health.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
