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

// Cards only: used by CardDamageSystem's poker-hand detection (Flush/Straight Flush).
public enum CardSuit
{
    Hearts,
    Spades,
    Diamonds,
    Clubs
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

    // Cards only: rank value used in CardDamageSystem's damage formula (A=1 ... K=13).
    public int cardValue;

    // Cards only: suit used by CardDamageSystem's poker-hand detection (Flush/Straight Flush).
    public CardSuit suit;
}
