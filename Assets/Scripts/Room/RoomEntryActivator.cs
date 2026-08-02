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
        if (targetToActivate != null)
            targetToActivate.SetActive(true);
    }
}
