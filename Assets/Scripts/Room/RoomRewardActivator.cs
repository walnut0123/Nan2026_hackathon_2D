using UnityEngine;

// 방 클리어 후 활성화할 오브젝트(breakable, 포탈 등)를 모아둔다. 클리어 전에는 전부
// 비활성 상태로 씬에 배치해두고, RoomController가 Active로 전환될 때 Activate()를 호출한다.
public class RoomRewardActivator : MonoBehaviour
{
    [SerializeField] private GameObject[] objectsToActivate;

    public void Activate()
    {
        if (objectsToActivate == null)
            return;

        foreach (var obj in objectsToActivate)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }
}
