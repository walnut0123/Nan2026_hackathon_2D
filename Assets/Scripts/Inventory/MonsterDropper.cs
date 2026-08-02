using UnityEngine;

public class MonsterDropper : MonoBehaviour
{
    [SerializeField] private LootTable lootTable;
    [SerializeField] private float popForce = 2f;

    [Tooltip("true면 LootTable에서 카드 타입 아이템이 뽑혀도 드랍하지 않는다.")]
    [SerializeField] private bool disableCardDrops = true;

    [Header("TEMP - Step 5 manual verification only (remove once real combat/Health exists)")]
    [SerializeField] private KeyCode testSpawnDropsKey = KeyCode.K;

    private void Awake()
    {
        // No real combat system yet. Once a Health-style component implementing
        // IDamageable exists, this auto-wires SpawnDrops() to its death event -
        // no changes needed here when combat lands.
        var damageable = GetComponent<IDamageable>();
        if (damageable != null)
            damageable.OnDeath += SpawnDrops;
    }

    private void Update()
    {
        if (Input.GetKeyDown(testSpawnDropsKey))
            SpawnDrops();
    }

    /// <summary>몬스터 1마리당 아이템 1개만 드랍한다(LootTable.GetSingleDrop). 오프셋/스캐터 없이
    /// 죽은 자리(transform.position)에 정확히 생성한다 - popForce는 위치 변경이 아니라 스폰 직후
    /// 순수 물리 임펄스라 살짝 튀어오르는 연출만 남는다.</summary>
    public void SpawnDrops()
    {
        if (lootTable == null)
        {
            Debug.LogWarning("[MonsterDropper] No LootTable assigned.");
            return;
        }

        var drop = lootTable.GetSingleDrop();
        if (drop == null)
        {
            Debug.Log("[MonsterDropper] No drop this time.");
            return;
        }

        var (item, count) = drop.Value;
        if (disableCardDrops && item.itemType == ItemType.Card)
        {
            Debug.Log($"[MonsterDropper] 카드 드랍 비활성화됨 - {item.itemName} 드랍 취소.");
            return;
        }

        if (item.worldPrefab == null)
        {
            Debug.LogWarning($"[MonsterDropper] {item.itemName} has no worldPrefab; skipping drop.");
            return;
        }

        Vector3 spawnPos = transform.position;

        var dropped = Instantiate(item.worldPrefab, spawnPos, Quaternion.identity);
        dropped.AddComponent<DroppedItemMarker>();

        var pickup = dropped.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.itemData = item;
            pickup.count = count;
            pickup.MarkAsFieldDrop();
        }

        var rb = dropped.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.AddForce(Vector2.up * popForce, ForceMode2D.Impulse);

        Debug.Log($"[MonsterDropper] Dropped {item.itemName} x{count} at {spawnPos}");
    }
}
