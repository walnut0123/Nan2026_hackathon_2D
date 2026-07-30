using System;
using System.Collections.Generic;

[Serializable]
public class InventorySlotSaveData
{
    public int slotIndex;
    public string itemId;
    public int count;
}

[Serializable]
public class WorldItemSaveData
{
    public string itemId;
    public int count;
    public float posX;
    public float posY;
    public float posZ;
}

[Serializable]
public class SaveData
{
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;
    public List<InventorySlotSaveData> inventorySlots = new List<InventorySlotSaveData>();

    /// <summary>CardInventory's 5 slots, by itemId ("" = empty). Cards live in a separate
    /// inventory from the general one, so they need their own save slot list.</summary>
    public List<string> cardSlotItemIds = new List<string>();

    /// <summary>Per-slot upgrade level (same index as cardSlotItemIds), from picking up a
    /// duplicate of a card already held. See CardInventory.UpgradeLevels / CardDamageSystem.cs.
    /// Old saves predating this field deserialize to an empty list; loading treats missing
    /// entries as upgrade level 0.</summary>
    public List<int> cardSlotUpgradeLevels = new List<int>();

    /// <summary>CardInventory.TotalAcquired - total number of card pickups ever, which the
    /// player level formula (CardDamageSystem's level damage term) is derived from. Stored
    /// separately from the slots above because replaced/discarded cards' pickup history isn't
    /// otherwise recoverable from the current slot contents.</summary>
    public int cardTotalAcquired;

    /// <summary>IDs (PersistentWorldEntity.Id) of pre-placed monsters/pickups that were
    /// killed or picked up, so they don't reappear on load.</summary>
    public List<string> removedWorldObjectIds = new List<string>();

    /// <summary>Item pickups that were dropped/spawned during play (monster drops, player
    /// drops) and are still lying on the ground, so they persist across a save/load.</summary>
    public List<WorldItemSaveData> worldItems = new List<WorldItemSaveData>();
}
