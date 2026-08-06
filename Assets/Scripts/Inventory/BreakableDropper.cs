using UnityEngine;

/// <summary>MonsterDropper's counterpart for breakable props (vases, crates, ...): same
/// LootTable-driven, IDamageable.OnDeath-triggered shape, but delegates the actual drop/scatter
/// presentation to ItemSpawner instead of spawning+AddForce inline.</summary>
public class BreakableDropper : MonoBehaviour
{
    [SerializeField] private LootTable lootTable;

    private void Awake()
    {
        var damageable = GetComponent<IDamageable>();
        if (damageable != null)
            damageable.OnDeath += SpawnDrops;
    }

    // GetDrops()(항목마다 독립 확률로 굴려서 여러 개가 동시에 나올 수 있는 방식)를 쓰면 항목 수가
    // 많은 테이블(예: 카드 52장 전체를 넣어둔 테이블)에서는 평균적으로 여러 장이 한꺼번에 나오거나
    // 아예 하나도 안 나올 수 있다. 화병은 항상 정확히 1장만 드랍해야 하므로, 가중치 비례로 항목
    // 하나만 고르는 GetSingleDrop()을 쓰고 개수도 1로 강제한다(항목의 minCount/maxCount 설정과
    // 무관하게 항상 1장을 보장).
    private void SpawnDrops()
    {
        if (lootTable == null)
        {
            Debug.LogWarning($"[BreakableDropper] {name} has no LootTable assigned.");
            return;
        }

        if (ItemSpawner.Instance == null)
        {
            Debug.LogWarning("[BreakableDropper] No ItemSpawner in scene.");
            return;
        }

        var drop = lootTable.GetSingleDrop();
        if (drop == null)
            return;

        ItemSpawner.Instance.SpawnDrop(drop.Value.item, 1, transform.position);
    }
}
