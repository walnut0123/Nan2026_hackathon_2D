using UnityEngine;

// 방 클리어 보상 상자. 지정한 방이 활성화(클리어 후 문 개방)되면 스프라이트를 opened로
// 바꾸고, cardSpawnPoint 위치에 필드 드랍과 동일한 방식(ItemPickup+DroppedItemMarker,
// popForce로 살짝 튀어오름)으로 무작위 카드 하나를 스폰한다. MonsterDropper와 같은
// LootTable.GetSingleDrop() 방식을 그대로 쓴다.
public class TreasureBoxReward : MonoBehaviour
{
    [SerializeField] private RoomController room;
    [SerializeField] private SpriteRenderer boxRenderer;
    [SerializeField] private Sprite openedSprite;
    [SerializeField] private Transform cardSpawnPoint;
    [SerializeField] private LootTable cardLootTable;
    [SerializeField] private float popForce = 2f;

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

    private void HandleRoomActivated()
    {
        if (boxRenderer != null && openedSprite != null)
            boxRenderer.sprite = openedSprite;

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
