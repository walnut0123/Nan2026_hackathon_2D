using System.Collections.Generic;
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
}
