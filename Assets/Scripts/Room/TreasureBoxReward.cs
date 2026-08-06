using UnityEngine;

// 방 클리어 보상 상자. 예전에는 방이 활성화(클리어 후 문 개방)되는 즉시 스프라이트를 opened로
// 바꾸고 카드를 스폰했지만, 지금은 트리거만 바뀐다 - 방 클리어 시점에는 "이제 열 수 있다"는
// 외곽선만 켜두고(BreakableOutlineOnRoomClear 재사용), 실제 오픈(스프라이트 교체 + 카드 스폰)은
// 플레이어가 다가와 상호작용(IInteractable.Interact)했을 때 일어난다. 오픈 로직 자체(스프라이트
// 교체 + cardSpawnPoint 위치에 필드 드랍과 동일한 방식으로 카드 스폰)는 그대로다.
public class TreasureBoxReward : MonoBehaviour, IInteractable
{
    [SerializeField] private RoomController room;
    [SerializeField] private SpriteRenderer boxRenderer;
    [SerializeField] private Sprite openedSprite;
    [SerializeField] private Transform cardSpawnPoint;
    [SerializeField] private LootTable cardLootTable;
    [SerializeField] private float popForce = 2f;

    [Tooltip("방 클리어 시 켤 외곽선 컴포넌트. 비워두면 같은 오브젝트에서 자동으로 찾는다.")]
    [SerializeField] private BreakableOutlineOnRoomClear outline;

    private bool canOpen;
    private bool opened;

    /// <summary>방이 클리어되어 열 수 있게 됐고, 아직 열지 않은 상태에서만 상호작용 가능.</summary>
    public bool CanInteract => canOpen && !opened;

    private void Awake()
    {
        if (outline == null)
            outline = GetComponent<BreakableOutlineOnRoomClear>();
    }

    private void Start()
    {
        if (room != null)
            room.OnRoomActivated += HandleRoomActivated;
    }

    private void OnDestroy()
    {
        if (room != null)
            room.OnRoomActivated -= HandleRoomActivated;
    }

    // 더 이상 여기서 바로 열지 않는다 - "열 수 있는 상태"로만 전환한다. 외곽선은
    // BreakableOutlineOnRoomClear 자신이 같은 이벤트를 구독해 알아서 켠다.
    private void HandleRoomActivated()
    {
        canOpen = true;
    }

    public void Interact(PlayerInventory inventory)
    {
        if (!CanInteract)
            return;

        opened = true;

        if (boxRenderer != null && openedSprite != null)
            boxRenderer.sprite = openedSprite;

        if (outline != null)
            outline.SetOutlineVisible(false);

        SpawnCard();
    }

    private void SpawnCard()
    {
        if (cardLootTable == null)
        {
            Debug.LogWarning("[TreasureBoxReward] No LootTable assigned.");
            return;
        }

        var drop = cardLootTable.GetSingleDrop();
        if (drop == null)
            return;

        var (item, count) = drop.Value;
        if (item.worldPrefab == null)
        {
            Debug.LogWarning($"[TreasureBoxReward] {item.itemName} has no worldPrefab; skipping.");
            return;
        }

        Vector3 spawnPos = cardSpawnPoint != null ? cardSpawnPoint.position : transform.position;
        var dropped = Instantiate(item.worldPrefab, spawnPos, Quaternion.identity);
        dropped.AddComponent<DroppedItemMarker>();

        var pickup = dropped.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.itemData = item;
            pickup.count = count;
            pickup.MarkAsFieldDrop();
        }

        // 카드가 상자보다 항상 위에 그려지도록 - 정렬 순서만으로 보장한다 (Y축 기준 정렬은
        // 같은 순서끼리의 동점 처리용이라 이 값이 다르면 항상 이걸 따른다).
        var droppedRenderer = dropped.GetComponent<SpriteRenderer>();
        if (droppedRenderer != null && boxRenderer != null)
            droppedRenderer.sortingOrder = boxRenderer.sortingOrder + 1;

        var rb = dropped.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.AddForce(Vector2.up * popForce, ForceMode2D.Impulse);
    }
}
