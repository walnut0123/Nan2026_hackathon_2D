using UnityEngine;

// 지정한 방에 처음 입장하는 순간 targetToActivate를 켠다 - 예: 보스 체력바 UI를 입장 전엔
// 숨겨두다가 방에 들어서면 바로 보여주는 용도. 몬스터 스폰 딜레이와는 무관하게
// OnPlayerEnteredRoom 시점(문이 잠기는 것과 동시)에 즉시 발동한다.
public class RoomEntryActivator : MonoBehaviour
{
    [SerializeField] private RoomController room;
    [SerializeField] private GameObject targetToActivate;

    private void Start()
    {
        if (room != null)
            room.OnPlayerEnteredRoom += HandleEntered;
    }

    private void OnDestroy()
    {
        if (room != null)
            room.OnPlayerEnteredRoom -= HandleEntered;
    }

    private void HandleEntered()
    {
        // OnPlayerEnteredRoom은 재입장 때도 항상 발생한다(미니맵 등 다른 구독자를 위해) - 이
        // 핸들러만 최초 입장(State가 아직 Idle일 때)에 한해 동작해야 한다. 그렇지 않으면 방을
        // 클리어하고 나간 뒤 되돌아올 때마다(예: 보스룸을 지나 다음 맵으로 이동하며 방 경계를
        // 다시 스치는 경우) 이미 죽어서 SetActive(false)된 보스 체력바를 여기서 다시 켜버린다.
        if (room != null && room.State != RoomState.Idle)
            return;

        if (targetToActivate != null)
            targetToActivate.SetActive(true);
    }
}
