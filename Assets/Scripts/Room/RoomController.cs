using System;
using System.Collections;
using UnityEngine;

// 엔터 더 건전 방식 방 클리어 상태 머신. Idle(입장 전) -> Locked(전투 중) -> Cleared -> Active
// 순서로만 전진하고 되돌아가지 않는다 - 한 번 Active가 되면 재입장해도 다시 잠기지 않는다
// (Idle 상태에서만 NotifyPlayerEntered가 잠금을 시작하므로 자연히 보장됨).
// 문/스포너/보상 오브젝트를 직접 참조하는 건 이 컨트롤러뿐이고, 그 아래 컴포넌트들끼리는
// 서로를 모른다 - 결합도를 낮추기 위한 설계 문서의 의도를 그대로 따랐다.
public enum RoomState
{
    Idle,
    Locked,
    Cleared,
    Active,
}

public class RoomController : MonoBehaviour
{
    [Header("방 식별")]
    [Tooltip("디버그/미니맵 표시용 이름")]
    [SerializeField] private string roomName = "Room";
    [Tooltip("미니맵에서 이 방이 그려질 격자 좌표")]
    [SerializeField] private Vector2Int gridCoordinate;

    [Header("구성 요소")]
    [Tooltip("이 방의 모든 출입구 문 - 잠금/해제 시 전부 함께 처리된다")]
    [SerializeField] private DoorController[] doors;
    [Tooltip("이 방의 몬스터 스폰 담당. 비워두면(또는 스폰 목록이 비어있으면) 몬스터 없는 방으로 " +
        "취급되어 입장해도 잠기지 않는다.")]
    [SerializeField] private RoomMonsterSpawner monsterSpawner;
    [Tooltip("클리어 후 활성화할 breakable/포탈 등을 담당. 비워둘 수 있다.")]
    [SerializeField] private RoomRewardActivator rewardActivator;

    [Tooltip("true면 게임 시작 시 플레이어가 이미 이 방 안에 있다고 보고 즉시 입장 처리한다. " +
        "시작 방은 Bounds 트리거로 감지할 수 없으므로(플레이어가 트리거 밖에서 안으로 들어오는 " +
        "게 아니라 처음부터 안에 있으므로) 이 방식이 필요하다.")]
    [SerializeField] private bool lockOnStart = false;

    [Tooltip("문이 잠긴 뒤 몬스터가 실제로 나타나기까지의 지연 시간(초). 0이면 즉시 스폰.")]
    [SerializeField] private float spawnDelay = 0f;

    public RoomState State { get; private set; } = RoomState.Idle;
    public Vector2Int GridCoordinate => gridCoordinate;
    public string RoomName => roomName;

    public event Action OnPlayerEnteredRoom;
    public event Action OnRoomLocked;
    public event Action OnRoomCleared;
    public event Action OnRoomActivated;

    private void OnEnable()
    {
        if (monsterSpawner != null)
            monsterSpawner.OnAllMonstersDefeated += HandleAllMonstersDefeated;
    }

    private void OnDisable()
    {
        if (monsterSpawner != null)
            monsterSpawner.OnAllMonstersDefeated -= HandleAllMonstersDefeated;
    }

    // 시작 방(플레이어가 트리거를 거치지 않고 처음부터 안에 있는 방)을 위한 진입점.
    private void Start()
    {
        if (lockOnStart)
            NotifyPlayerEntered();
    }

    // RoomBoundsTrigger가 플레이어 진입을 감지하면 호출한다. Idle 상태가 아니면(이미 잠겼거나
    // 클리어됐으면) 아무 일도 하지 않는다 - 재입장 시 다시 잠기지 않는 것이 이 한 줄로 보장된다.
    public void NotifyPlayerEntered()
    {
        // 미니맵 등 "지금 어느 방에 있는지"만 필요한 구독자를 위해 재입장이어도 항상 알린다.
        // 잠금/스폰 판정(아래)만 최초 입장(Idle)으로 게이팅한다.
        Debug.Log($"[Room] 현재 방: {roomName}");
        OnPlayerEnteredRoom?.Invoke();

        if (State != RoomState.Idle)
            return;

        bool hasMonsters = monsterSpawner != null && monsterSpawner.HasMonsters;
        if (!hasMonsters)
        {
            // 클리어 조건(몬스터)이 없는 방(입구방 등)은 잠글 필요 없이 즉시 클리어 처리한다 -
            // 그래야 이 방이 소유한 문/보상도(있다면) 상태 머신을 제대로 거쳐 해제된다.
            State = RoomState.Cleared;
            Debug.Log($"[Room] {roomName} 클리어!");
            OnRoomCleared?.Invoke();

            ActivateRoom();
            return;
        }

        LockRoom();
    }

    private void LockRoom()
    {
        State = RoomState.Locked;
        Debug.Log($"[Room] {roomName} 잠김");

        foreach (var door in doors)
            door.SetLocked(true);

        OnRoomLocked?.Invoke();

        StartCoroutine(SpawnMonstersAfterDelay());
    }

    private IEnumerator SpawnMonstersAfterDelay()
    {
        if (spawnDelay > 0f)
            yield return new WaitForSeconds(spawnDelay);

        Debug.Log($"[Room] {roomName} 몬스터 출현");
        monsterSpawner.SpawnAll();
    }

    private void HandleAllMonstersDefeated()
    {
        if (State != RoomState.Locked)
            return;

        State = RoomState.Cleared;
        Debug.Log($"[Room] {roomName} 클리어!");
        OnRoomCleared?.Invoke();

        ActivateRoom();
    }

    private void ActivateRoom()
    {
        State = RoomState.Active;

        foreach (var door in doors)
            door.SetLocked(false);

        if (rewardActivator != null)
            rewardActivator.Activate();

        Debug.Log($"[Room] {roomName} 활성화 (문 개방)");
        OnRoomActivated?.Invoke();
    }
}
