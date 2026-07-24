using System.Collections;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    // No serialized cross-references: everything below is discovered automatically
    // so nothing can be mis-wired by dragging the wrong object in the Inspector.
    private PlayerInventory playerInventory;
    private CombinationManager combinationManager;
    private Transform slotContainer;
    private GameObject slotUIPrefab;

    private int? selectedSlotIndex;
    private ItemData selectedItem;
    private bool refreshPending;

    private void Awake()
    {
        slotContainer = transform.Find("SlotContainer");
        slotUIPrefab = Resources.Load<GameObject>("UI/InventorySlotUI_Row");
    }

    private void Start()
    {
        playerInventory = FindObjectOfType<PlayerInventory>();
        combinationManager = FindObjectOfType<CombinationManager>();

        playerInventory.Inventory.OnInventoryChanged += QueueRefresh;
        Refresh();

        UIManager.Register("Inventory", gameObject);
    }

    private void OnDestroy()
    {
        if (playerInventory != null && playerInventory.Inventory != null)
            playerInventory.Inventory.OnInventoryChanged -= QueueRefresh;

        UIManager.Unregister("Inventory");
    }

    // Inventory changes (drop/combine) are fired from inside a slot row's own Button
    // click handler. Rebuilding the rows synchronously there would destroy the very
    // GameObject that handler is running on, which corrupts the UI EventSystem's
    // pointer state and throws on a later, unrelated click. Deferring one frame lets
    // the click finish first. The pending flag collapses multiple same-frame triggers
    // (e.g. two TryAddItem calls in a row) into a single rebuild.
    private void QueueRefresh()
    {
        if (refreshPending)
            return;

        refreshPending = true;

        // While the panel is closed (inactive GameObject), StartCoroutine can't run - it
        // fails silently. OnEnable() picks up the pending refresh once the panel reopens.
        if (gameObject.activeInHierarchy)
            StartCoroutine(RefreshNextFrame());
    }

    private void OnEnable()
    {
        if (refreshPending)
        {
            refreshPending = false;
            Refresh();
        }
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        refreshPending = false;
        Refresh();
    }

    private void Refresh()
    {
        selectedSlotIndex = null;
        selectedItem = null;

        for (int i = slotContainer.childCount - 1; i >= 0; i--)
            Destroy(slotContainer.GetChild(i).gameObject);

        var slots = playerInventory.Inventory.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty)
                continue;

            var rowGO = Instantiate(slotUIPrefab, slotContainer);
            var rowUI = rowGO.GetComponent<InventorySlotUI>();
            rowUI.Setup(i, slots[i], playerInventory, this);
        }
    }

    public void OnSlotCombineClicked(int slotIndex, ItemData item)
    {
        if (selectedSlotIndex == null)
        {
            selectedSlotIndex = slotIndex;
            selectedItem = item;
            Debug.Log($"[InventoryUI] Selected {item.itemName} for combination. Pick a second item.");
            return;
        }

        if (selectedSlotIndex == slotIndex)
        {
            // Clicked the same slot again - cancel selection.
            selectedSlotIndex = null;
            selectedItem = null;
            return;
        }

        var a = selectedItem;
        var b = item;
        selectedSlotIndex = null;
        selectedItem = null;

        combinationManager.TryCombine(playerInventory, a, b);
    }
}
