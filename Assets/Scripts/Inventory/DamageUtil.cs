using UnityEngine;

// 트리거/오버랩 충돌 결과(Collider2D)에서 실제로 데미지를 받아야 할 Health를 찾는 공용 로직.
// 대상이 Hurtbox(피격판정 전용 자식 콜라이더)를 두고 있다면 반드시 그 콜라이더를 통해서만
// 데미지를 받아야 한다 - 그렇지 않으면 이동/벽 충돌용 콜라이더가 같은 Health를 다시 찾아내
// 한 번의 공격에 두 번 맞는 이중 판정이 생긴다.
public static class DamageUtil
{
    public static Health ResolveHealth(Collider2D other)
    {
        var hurtbox = other.GetComponent<Hurtbox>();
        if (hurtbox != null)
            return hurtbox.Health;

        var health = other.GetComponent<Health>();
        if (health != null && health.GetComponentInChildren<Hurtbox>() == null)
            return health;

        return null;
    }
}
