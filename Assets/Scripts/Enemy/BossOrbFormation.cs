using UnityEngine;

// 정사각형 패턴의 다이아몬드 대형 전체를 하나로 묶는 루트. 개별 오브가 각자 도는 게 아니라
// "정사각형 모양 자체"가 시전 위치(=대형의 중심)를 축으로 통째로 회전해야 하므로, 이동/회전은
// 오직 이 부모에서만 처리한다. 개별 오브(자식, BossOrbHit)는 로컬 위치를 그대로 유지한 채
// 이 부모를 따라 이동/회전하고, 피격 판정만 각자 담당한다. lifetime이 지나면 대형 전체가
// (자식 오브들과 함께) 한꺼번에 사라진다.
//
// 대형 전체가 "하나의 투사체"로 취급되도록, 자식 오브 중 하나라도 명중하면 ConsumeHit()으로
// 대형 전체를 즉시 없애서 같은 공격에 두 번 맞는 일이 없게 한다(HasHit는 같은 프레임에 여러
// 오브가 동시에 트리거되더라도 먼저 처리된 쪽이 즉시 true로 만들어 나머지를 막는다).
[RequireComponent(typeof(Rigidbody2D))]
public class BossOrbFormation : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float rotationSpeed;
    private float maxLifetime;
    private float elapsed;

    public bool HasHit { get; private set; }

    public void Initialize(Vector2 moveDirection, float moveSpeed, float formationRotationSpeed, float lifetime)
    {
        direction = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector2.right;
        speed = moveSpeed;
        rotationSpeed = formationRotationSpeed;
        maxLifetime = lifetime;
    }

    // 자식 오브 중 하나가 명중했을 때 호출한다 - 대형 전체를 "이미 한 번 맞은 투사체"로 소진시켜
    // 즉시 없앤다. 이미 소진됐으면 아무 일도 하지 않는다(중복 호출 방지).
    public void ConsumeHit()
    {
        if (HasHit)
            return;

        HasHit = true;
        Destroy(gameObject);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        elapsed += Time.deltaTime;
        if (elapsed >= maxLifetime)
            Destroy(gameObject);
    }
}
