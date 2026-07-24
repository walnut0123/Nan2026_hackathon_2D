using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Persistent (DontDestroyOnLoad) coordinator for the save/load flow between the
/// Title and Main scenes. Lives on a GameObject placed in the Title scene.</summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const string TitleSceneName = "Title";
    public const string MainSceneName = "Main";

    [SerializeField] private ItemDatabase itemDatabase;

    [Tooltip("Where the player spawns on a brand new game (no save data yet).")]
    [SerializeField] private Vector3 newGameSpawnPosition = new Vector3(56f, 0f, 0f);

    private SaveData pendingLoadData;

    // IDs of pre-placed monsters/pickups killed or picked up this session, so a later
    // SaveGame() knows they should stay gone. Repopulated from disk in ApplyLoadedData.
    private readonly HashSet<string> removedWorldObjectIds = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public bool HasSaveGame => SaveSystem.SaveExists();

    /// <summary>Called by Health/ItemPickup when a persistent world object is killed or
    /// picked up, so it doesn't reappear after a save/load.</summary>
    public void MarkWorldObjectRemoved(string id)
    {
        if (!string.IsNullOrEmpty(id))
            removedWorldObjectIds.Add(id);
    }

    /// <summary>"새로운 게임" - discards any existing save and starts fresh.</summary>
    public void NewGame()
    {
        SaveSystem.DeleteSave();
        pendingLoadData = null;
        removedWorldObjectIds.Clear();
        SceneManager.LoadScene(MainSceneName);
    }

    /// <summary>"Play start" - continues the existing save, or starts fresh if none exists.</summary>
    public void PlayStart()
    {
        if (SaveSystem.SaveExists())
        {
            pendingLoadData = SaveSystem.Load();
        }
        else
        {
            pendingLoadData = null;
            removedWorldObjectIds.Clear();
        }

        SceneManager.LoadScene(MainSceneName);
    }

    /// <summary>"게임 저장" - writes the current player + world state to disk without changing scenes.</summary>
    public void SaveGame()
    {
        var playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogWarning("[GameManager] No PlayerInventory found in scene; nothing to save.");
            return;
        }

        var data = new SaveData();
        var pos = playerInventory.transform.position;
        data.playerPosX = pos.x;
        data.playerPosY = pos.y;
        data.playerPosZ = pos.z;

        var slots = playerInventory.Inventory.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty)
                continue;

            data.inventorySlots.Add(new InventorySlotSaveData
            {
                slotIndex = i,
                itemId = slots[i].item.itemId,
                count = slots[i].count
            });
        }

        data.removedWorldObjectIds.AddRange(removedWorldObjectIds);

        foreach (var marker in FindObjectsOfType<DroppedItemMarker>())
        {
            var pickup = marker.GetComponent<ItemPickup>();
            if (pickup == null || pickup.itemData == null)
                continue;

            var itemPos = pickup.transform.position;
            data.worldItems.Add(new WorldItemSaveData
            {
                itemId = pickup.itemData.itemId,
                count = pickup.count,
                posX = itemPos.x,
                posY = itemPos.y,
                posZ = itemPos.z
            });
        }

        SaveSystem.Save(data);
    }

    /// <summary>설정 패널의 "저장하고 나가기" - saves, then returns to the Title scene.</summary>
    public void SaveAndExitToTitle()
    {
        SaveGame();
        SceneManager.LoadScene(TitleSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != MainSceneName)
            return;

        if (pendingLoadData != null)
        {
            ApplyLoadedData(pendingLoadData);
            pendingLoadData = null;
        }
        else
        {
            var playerInventory = FindObjectOfType<PlayerInventory>();
            if (playerInventory != null)
                playerInventory.transform.position = newGameSpawnPosition;
        }
    }

    private void ApplyLoadedData(SaveData data)
    {
        var playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory == null)
        {
            Debug.LogWarning("[GameManager] No PlayerInventory found in Main scene; cannot apply save data.");
            return;
        }

        playerInventory.transform.position = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);

        playerInventory.Inventory.Clear();
        if (itemDatabase != null)
        {
            foreach (var slotData in data.inventorySlots)
            {
                var item = itemDatabase.GetItemById(slotData.itemId);
                if (item == null)
                {
                    Debug.LogWarning($"[GameManager] Save references unknown itemId '{slotData.itemId}'; skipping.");
                    continue;
                }

                playerInventory.Inventory.SetSlot(slotData.slotIndex, item, slotData.count);
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] ItemDatabase not assigned; inventory contents could not be restored.");
        }

        playerInventory.Inventory.NotifyChanged();

        removedWorldObjectIds.Clear();
        removedWorldObjectIds.UnionWith(data.removedWorldObjectIds);

        foreach (var entity in FindObjectsOfType<PersistentWorldEntity>())
        {
            if (removedWorldObjectIds.Contains(entity.Id))
                Destroy(entity.gameObject);
        }

        if (itemDatabase != null)
        {
            foreach (var worldItem in data.worldItems)
            {
                var item = itemDatabase.GetItemById(worldItem.itemId);
                if (item == null || item.worldPrefab == null)
                {
                    Debug.LogWarning($"[GameManager] Could not restore world item '{worldItem.itemId}'.");
                    continue;
                }

                var spawnPos = new Vector3(worldItem.posX, worldItem.posY, worldItem.posZ);
                var spawned = Instantiate(item.worldPrefab, spawnPos, Quaternion.identity);
                spawned.AddComponent<DroppedItemMarker>();

                var pickup = spawned.GetComponent<ItemPickup>();
                if (pickup != null)
                {
                    pickup.itemData = item;
                    pickup.count = worldItem.count;
                }
            }
        }
    }
}
