using UnityEngine;

// 피격판정 전용 트리거 콜라이더에 붙이는 마커. 이동/벽 충돌용 콜라이더와 같은 오브젝트에
// Health를 두면 두 콜라이더가 동시에 데미지 판정을 받아 이중 히트가 나기 쉬우므로, 이 마커가
// 붙은 콜라이더가 있으면 데미지는 반드시 그쪽으로만 들어오게 한다(DamageUtil 참고).
// Health는 부모(또는 조상)에서 찾아 캐싱한다.
public class Hurtbox : MonoBehaviour
{
    private Health health;

    public Health Health => health != null ? health : (health = GetComponentInParent<Health>());
}
