using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    public ItemData itemData;
    public int count = 1;

    // Final behavior, not a dev-only stub: a fully picked-up item is removed from the
    // field. Kept as a toggle (default on) so a specific pickup can be debugged without
    // disappearing, e.g. to repeatedly test Interact() without re-dropping it each time.
    public bool destroyOnFullPickup = true;

    // Cards only: whether this instance is actually pickable. Thrown card projectiles
    // (CardAutoAttack.ShootCard) reuse the same card prefabs as field/monster drops, so this
    // defaults to false to keep an in-flight thrown card from being interactable; field/monster
    // drop spawns must explicitly flip it to true (done by hand for now on scene-placed cards).
    [SerializeField] private bool isFieldDrop = false;

    // Called by spawners (MonsterDropper, etc.) right after instantiating a genuine field/monster
    // drop, since those spawn from the same prefabs a thrown card projectile uses.
    public void MarkAsFieldDrop()
    {
        isFieldDrop = true;
    }

    /// <summary>InteractionDetector에서 "지금 실제로 주울 수 있는 상태인가"를 판단하는 데 쓴다
    /// (날아가는 카드 투사체는 이 값이 false).</summary>
    public bool IsFieldDrop => isFieldDrop;

    public void Interact(PlayerInventory inventory)
    {
        if (itemData == null)
            return;

        // Cards live in their own dedicated 5-slot CardInventory, not the general
        // PlayerInventory - route them there instead of falling through below.
        if (itemData.itemType == ItemType.Card)
        {
            InteractAsCard();
            return;
        }

        if (inventory == null)
            return;

        int leftover = inventory.TryAddItem(itemData, count);
        int picked = count - leftover;

        if (picked <= 0)
        {
            Debug.Log($"[ItemPickup] Inventory full, could not pick up {itemData.itemName}");
            return;
        }

        Debug.Log($"[ItemPickup] Picked up {itemData.itemName} x{picked}");

        if (leftover > 0)
        {
            count = leftover;
        }
        else if (destroyOnFullPickup)
        {
            var entity = GetComponent<PersistentWorldEntity>();
            if (entity != null)
                GameManager.Instance?.MarkWorldObjectRemoved(entity.Id);

            Destroy(gameObject);
        }
    }

    private void InteractAsCard()
    {
        if (!isFieldDrop)
            return;

        var cardInventory = CardInventory.Instance;
        if (cardInventory == null)
            return;

        int playerLevel = cardInventory.PlayerLevel;

        // Acquire() 전 스냅샷 - 자동교체(Replaced)일 때 "무엇이 밀려났는지"와, 획득 전/후
        // 데미지 비교 패널(CardDamagePreviewDebugUI) 양쪽 다 이 시점 상태가 필요하다.
        var beforeCards = ToArray(cardInventory.Slots);
        var beforeDamages = CardDamageSystem.GetSlotDamages(cardInventory.Slots, cardInventory.UpgradeLevels, playerLevel);
        var compositionBefore = CardDamageSystem.EvaluateComposition(cardInventory.Slots);

        var action = cardInventory.Acquire(itemData, out int targetSlot);
        ItemData replacedCard = action == CardAcquireAction.Replaced ? beforeCards[targetSlot] : null;

        var afterCards = ToArray(cardInventory.Slots);
        var afterDamages = CardDamageSystem.GetSlotDamages(cardInventory.Slots, cardInventory.UpgradeLevels, playerLevel);
        var compositionAfter = CardDamageSystem.EvaluateComposition(cardInventory.Slots);

        Debug.Log($"[ItemPickup] Picked up card {itemData.itemName}");
        CardAcquiredPopup.Show(itemData, action, compositionBefore, compositionAfter, replacedCard);
        CardDamagePreviewDebugUI.ShowSnapshot(itemData, beforeCards, beforeDamages, afterCards, afterDamages);

        if (destroyOnFullPickup)
        {
            var entity = GetComponent<PersistentWorldEntity>();
            if (entity != null)
                GameManager.Instance?.MarkWorldObjectRemoved(entity.Id);

            Destroy(gameObject);
        }
    }

    private static ItemData[] ToArray(System.Collections.Generic.IReadOnlyList<ItemData> cards)
    {
        var result = new ItemData[cards.Count];
        for (int i = 0; i < cards.Count; i++)
            result[i] = cards[i];
        return result;
    }
}
