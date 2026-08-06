using UnityEngine;

// 탄환 차단 블록 - 이 컴포넌트 하나만 붙이면 그 오브젝트가 RangedProjectile(적/보스 탄막)을 막는
// 벽이 된다. 향후 비슷한 2x2 블록 이미지를 추가로 배치할 때도 이 스크립트만 연결하면 즉시 동일하게
// 작동하도록 범용으로 만들었다 - 블록 쪽은 투사체의 구체적인 종류(보스 탄막인지 마인 탄막인지)를
// 몰라도 되고, 투사체 쪽(RangedProjectile)도 이 블록의 존재를 몰라도 된다. 트리거 콜라이더인
// 투사체가 다른 콜라이더(트리거든 아니든)에 겹치면 양쪽 모두에서 OnTriggerEnter2D가 발생하므로,
// 이 블록의 Collider2D는 플레이어를 막는 일반 콜라이더(비-트리거)여도 상관없이 그대로 동작한다.
public class ProjectileBlocker : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var projectile = other.GetComponent<RangedProjectile>();
        if (projectile != null)
            Destroy(other.gameObject);
    }
}
