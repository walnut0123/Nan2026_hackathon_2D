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

        ItemSpawner.Instance.SpawnDrops(lootTable.GetDrops(), transform.position);
    }
}
