using UnityEngine;

// Always-visible display for the 5-slot CardInventory (no open/close toggle, unlike InventoryUI).
public class CardInventoryUI : MonoBehaviour
{
    [SerializeField] private CardSlotUI[] slots;

    private CardInventory cardInventory;

    private void Start()
    {
        cardInventory = CardInventory.Instance;
        if (cardInventory == null)
        {
            Debug.LogWarning("[CardInventoryUI] No CardInventory found in the scene.");
            return;
        }

        cardInventory.OnChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (cardInventory != null)
            cardInventory.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        var cardSlots = cardInventory.Slots;
        for (int i = 0; i < slots.Length && i < cardSlots.Count; i++)
            slots[i].SetCard(cardSlots[i]);
    }
}
