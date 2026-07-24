using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int capacity = 20;

    public Inventory Inventory { get; private set; }

    private void Awake()
    {
        Inventory = new Inventory(capacity);
    }

    public int TryAddItem(ItemData item, int count)
    {
        return Inventory.AddItem(item, count);
    }

    public bool TryRemoveItem(ItemData item, int count)
    {
        return Inventory.RemoveItem(item, count);
    }

    public bool TryDropItem(int slotIndex, int count)
    {
        if (slotIndex < 0 || slotIndex >= Inventory.Slots.Count)
            return false;

        var slot = Inventory.Slots[slotIndex];
        if (slot.IsEmpty || slot.count < count)
            return false;

        var item = slot.item;
        if (item.worldPrefab == null)
        {
            Debug.LogWarning($"[PlayerInventory] {item.itemName} has no worldPrefab assigned; cannot drop.");
            return false;
        }

        if (!Inventory.RemoveItem(item, count))
            return false;

        Vector3 spawnPos = GetDropPosition();
        var dropped = Instantiate(item.worldPrefab, spawnPos, Quaternion.identity);
        dropped.AddComponent<DroppedItemMarker>();

        var pickup = dropped.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.itemData = item;
            pickup.count = count;
        }

        Debug.Log($"[PlayerInventory] Dropped {item.itemName} x{count} at {spawnPos}");
        return true;
    }

    // 2D에서는 바닥 높이를 찾기 위한 레이캐스트가 필요 없다 - 플레이어 주변 XY 평면에 살짝 흩뿌리듯 배치
    private Vector3 GetDropPosition()
    {
        Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
        return transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
    }
}
