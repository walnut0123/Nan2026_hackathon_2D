using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> items = new List<ItemData>();

    private Dictionary<string, ItemData> lookup;

    private void BuildLookup()
    {
        lookup = new Dictionary<string, ItemData>();
        foreach (var item in items)
        {
            if (item == null || string.IsNullOrEmpty(item.itemId))
                continue;

            if (lookup.ContainsKey(item.itemId))
            {
                Debug.LogWarning($"[ItemDatabase] Duplicate itemId detected: {item.itemId}");
                continue;
            }

            lookup.Add(item.itemId, item);
        }
    }

    public ItemData GetItemById(string itemId)
    {
        if (lookup == null)
            BuildLookup();

        lookup.TryGetValue(itemId, out var result);
        return result;
    }

    [ContextMenu("Test Lookup (Console Log)")]
    private void TestLookup()
    {
        BuildLookup();

        foreach (var item in items)
        {
            if (item == null)
                continue;

            var found = GetItemById(item.itemId);
            var resultText = found == item ? "OK" : "FAIL";
            Debug.Log($"[ItemDatabase] Lookup '{item.itemId}' -> {(found != null ? found.itemName : "null")} [{resultText}]");
        }
    }
}
