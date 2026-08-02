using UnityEngine;

// 정사각형 대형(BossOrbFormation)을 이루는 개별 오브 하나의 피격 판정만 담당한다. 다만 대형
// 전체가 "하나의 투사체"로 취급되어야 하므로, 이 오브가 명중시키면 자기 자신만 지우는 게 아니라
// 부모 BossOrbFormation.ConsumeHit()을 호출해서 대형 전체를 없앤다 - 같은 공격에 두 번 맞는
// 일이 없도록 하기 위함. 데미지는 DamageUtil을 통해 Hurtbox가 있는 대상은 반드시 그쪽으로만
// 받게 해서, 플레이어의 이동용 콜라이더가 이중으로 맞는 것을 막는다.
[RequireComponent(typeof(Collider2D))]
public class BossOrbHit : MonoBehaviour
{
    private int damage;
    private BossOrbFormation formation;

    public void Initialize(int hitDamage)
    {
        damage = hitDamage;
    }

    private void Awake()
    {
        formation = GetComponentInParent<BossOrbFormation>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 대형이 이미 한 번 맞아서 소진됐으면(파괴 대기 중이어도) 더 이상 판정하지 않는다.
        if (formation != null && formation.HasHit)
            return;

        if (other.CompareTag("Enemy"))
            return;

        var health = DamageUtil.ResolveHealth(other);
        if (health != null)
        {
            health.TakeDamage(damage);

            if (formation != null)
                formation.ConsumeHit();
            else
                Destroy(gameObject);
        }
    }
}
