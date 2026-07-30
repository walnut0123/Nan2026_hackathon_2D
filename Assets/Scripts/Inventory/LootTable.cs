using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class LootEntry
{
    public ItemData itemData;
    [Range(0f, 1f)] public float dropChance = 1f;
    public int minCount = 1;
    public int maxCount = 1;
}

[CreateAssetMenu(fileName = "NewLootTable", menuName = "Inventory/Loot Table")]
public class LootTable : ScriptableObject
{
    public List<LootEntry> entries = new List<LootEntry>();

    /// <summary>독립 확률로 여러 개가 동시에 떨어질 수 있는 방식. 부서지는 오브젝트(ItemSpawner)
    /// 등 "한 번에 여러 개" 드랍이 실제로 필요한 곳에서만 쓸 것 - 몬스터 처치 드랍은
    /// GetSingleDrop()을 쓴다.</summary>
    public List<(ItemData item, int count)> GetDrops()
    {
        var results = new List<(ItemData, int)>();

        foreach (var entry in entries)
        {
            if (entry.itemData == null) continue;
            if (Random.value > entry.dropChance) continue;

            int count = Random.Range(entry.minCount, entry.maxCount + 1);
            if (count <= 0) continue;

            results.Add((entry.itemData, count));
        }

        return results;
    }

    /// <summary>
    /// 항목 하나만 뽑는다(몬스터 처치당 아이템 1개 드랍용). dropChance를 독립 확률이 아니라
    /// 상대 가중치로 재해석해서 가중치에 비례한 확률로 정확히 하나를 고른다 - 기존 dropChance
    /// 데이터를 새로 손보지 않고도 "1개 보장" 요구사항에 맞출 수 있다.
    /// 유효한 항목이 없으면 null.
    /// </summary>
    public (ItemData item, int count)? GetSingleDrop()
    {
        var candidates = entries.Where(e => e.itemData != null && e.dropChance > 0f).ToList();
        if (candidates.Count == 0)
            return null;

        float totalWeight = candidates.Sum(e => e.dropChance);
        float roll = Random.value * totalWeight;
        float cumulative = 0f;

        foreach (var entry in candidates)
        {
            cumulative += entry.dropChance;
            if (roll <= cumulative)
                return (entry.itemData, Mathf.Max(1, Random.Range(entry.minCount, entry.maxCount + 1)));
        }

        var last = candidates[candidates.Count - 1];
        return (last.itemData, Mathf.Max(1, Random.Range(last.minCount, last.maxCount + 1)));
    }
}
