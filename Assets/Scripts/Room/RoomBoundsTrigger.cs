using UnityEngine;

// 방 경계에 붙는 트리거 콜라이더. 플레이어가 들어오면 RoomController에게 알리기만 하고,
// 실제로 잠글지 말지(몬스터 존재 여부, 이미 Idle이 아닌 상태 등) 판단은 전부
// RoomController.NotifyPlayerEntered()에 위임한다.
[RequireComponent(typeof(Collider2D))]
public class RoomBoundsTrigger : MonoBehaviour
{
    [SerializeField] private RoomController room;

    private void Reset()
    {
        room = GetComponentInParent<RoomController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // GetComponentInParent가 아니라 GetComponent로 정확히 이 콜라이더의 오브젝트에서만
        // PlayerInventory를 찾는다 - PlayerInventory는 Player 루트(발밑 이동 콜라이더와 같은
        // 오브젝트)에만 붙어 있고 HurtBox/InteractionTrigger 등 자식 콜라이더에는 없으므로,
        // 이렇게 하면 발밑 콜라이더가 실제로 닿았을 때만 반응한다. GetComponentInParent를 쓰면
        // 상호작용 판정 범위처럼 room 판정과 무관한 다른 콜라이더 크기에 따라 타이밍이 흔들린다.
        if (other.GetComponent<PlayerInventory>() == null)
            return;

        if (room != null)
            room.NotifyPlayerEntered();
    }
}
