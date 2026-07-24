using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    // Wired automatically from the fixed prefab hierarchy (Icon/Label/DropButton/
    // CombineButton) instead of serialized fields, so nothing here can be dragged
    // onto the wrong reference by accident in the Inspector.
    private Image icon;
    private TextMeshProUGUI label;
    private Button dropButton;
    private Button combineButton;
    private Image background;

    private int slotIndex;
    private ItemData item;
    private PlayerInventory playerInventory;
    private InventoryUI inventoryUI;

    private void Awake()
    {
        background = GetComponent<Image>();
        icon = transform.Find("Icon").GetComponent<Image>();
        label = transform.Find("Label").GetComponent<TextMeshProUGUI>();
        dropButton = transform.Find("DropButton").GetComponent<Button>();
        combineButton = transform.Find("CombineButton").GetComponent<Button>();
    }

    public void Setup(int index, InventorySlot slotData, PlayerInventory inventory, InventoryUI owner)
    {
        slotIndex = index;
        item = slotData.item;
        playerInventory = inventory;
        inventoryUI = owner;

        label.text = $"{item.itemName} x{slotData.count}";
        icon.sprite = item.icon;
        icon.enabled = item.icon != null;

        dropButton.onClick.RemoveAllListeners();
        dropButton.onClick.AddListener(OnDropClicked);

        combineButton.onClick.RemoveAllListeners();
        combineButton.onClick.AddListener(OnCombineClicked);

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        background.color = selected ? new Color(1f, 0.85f, 0.3f, 0.6f) : new Color(0f, 0f, 0f, 0.35f);
    }

    private void OnDropClicked()
    {
        playerInventory.TryDropItem(slotIndex, 1);
    }

    private void OnCombineClicked()
    {
        inventoryUI.OnSlotCombineClicked(slotIndex, item);
    }
}
