using UnityEngine;

public enum ItemType
{
    Material,
    Consumable,
    Equipment,
    QuestItem,
    CraftedItem,
    Card
}

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemId;
    public string itemName;
    [TextArea]
    public string description;
    public Sprite icon;
    public ItemType itemType;
    public int maxStackCount = 99;
    public GameObject worldPrefab;
    public bool isCombinable;

    // Cards only: rank value used by CardProjectile's damage formula (A=1 ... K=13).
    public int cardValue;
}
