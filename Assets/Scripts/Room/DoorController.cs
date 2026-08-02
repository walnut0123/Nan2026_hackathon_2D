using UnityEngine;

// 문 하나의 잠금/해제만 담당한다. 잠기면 차단용 콜라이더가 켜지고 "닫힘" 스프라이트로,
// 풀리면 콜라이더가 꺼지고 "열림" 스프라이트로 바뀐다. openSprite/closedSprite를 비워두면
// 스프라이트 전환 없이 콜라이더만으로 동작한다(플레이스홀더 아트 없이도 기능 테스트 가능).
public class DoorController : MonoBehaviour
{
    [SerializeField] private Collider2D blockingCollider;
    [SerializeField] private SpriteRenderer doorRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    public bool IsLocked { get; private set; }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;

        if (blockingCollider != null)
            blockingCollider.enabled = locked;

        if (doorRenderer != null)
        {
            Sprite target = locked ? closedSprite : openSprite;
            if (target != null)
                doorRenderer.sprite = target;
        }
    }
}
