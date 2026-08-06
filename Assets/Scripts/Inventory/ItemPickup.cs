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

    // 카드 투사체(CardAutoAttack.ShootSingleCard)는 필드/몬스터 드랍과 같은 프리팹을 재사용하기
    // 때문에 IInteractable 자체는 항상 붙어 있다 - 실제로 주울 수 있는 상태인지는 isFieldDrop으로만
    // 구분된다. 카드가 아닌 일반 아이템은 항상 상호작용 가능.
    public bool CanInteract => itemData == null || itemData.itemType != ItemType.Card || isFieldDrop;

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

    // "획득" 버튼(InteractionDetector.TryInteract -> Interact)을 눌렀을 때 호출된다. 더 이상
    // 범위에 들어오는 즉시 자동으로 줍지 않는다 - InteractionDetector가 카드도 일반 아이템처럼
    // interactablesInRange에 넣어 프롬프트/버튼을 거치게 한다.
    private void InteractAsCard()
    {
        if (!isFieldDrop)
            return;

        var cardInventory = CardInventory.Instance;
        if (cardInventory == null)
            return;

        var beforeCards = ToArray(cardInventory.Slots);
        var beforeDamages = CardDamageSystem.GetSlotDamages(cardInventory.Slots, cardInventory.UpgradeLevels, cardInventory.PlayerLevel);
        var compositionBefore = CardDamageSystem.EvaluateComposition(cardInventory.Slots);

        var action = cardInventory.Acquire(itemData, out _);

        if (action == CardAcquireAction.NeedsSwapChoice)
        {
            // 인벤토리 5칸이 이미 다른 카드로 꽉 차 있다 - 자동으로 아무 슬롯이나 밀어내지 않고,
            // 어느 슬롯을 내줄지 플레이어가 직접 고르게 한다. 필드의 이 카드는 선택이 끝날 때까지
            // 그대로 남아 있는다(아직 아무것도 소모되지 않았으므로 Destroy하지 않고 return).
            var ui = CardInventoryUI.Instance;
            if (ui != null)
                ui.BeginSwapSelection(itemData, HandleSwapSlotChosen);
            else
                Debug.LogWarning("[ItemPickup] CardInventoryUI를 찾을 수 없어 카드 교체 UI를 띄우지 못했습니다.");
            return;
        }

        FinishCardAcquire(action, null, beforeCards, beforeDamages, compositionBefore);
    }

    // CardInventoryUI의 슬롯별 "변경" 버튼 클릭 콜백. 플레이어가 인덱스를 고른 시점에야 실제로
    // 슬롯을 교체하고 이 필드 카드를 소모한다.
    private void HandleSwapSlotChosen(int slotIndex)
    {
        var cardInventory = CardInventory.Instance;
        if (cardInventory == null || itemData == null)
            return;

        var beforeCards = ToArray(cardInventory.Slots);
        var beforeDamages = CardDamageSystem.GetSlotDamages(cardInventory.Slots, cardInventory.UpgradeLevels, cardInventory.PlayerLevel);
        var compositionBefore = CardDamageSystem.EvaluateComposition(cardInventory.Slots);

        ItemData replacedCard = cardInventory.SwapSlot(slotIndex, itemData);

        FinishCardAcquire(CardAcquireAction.Replaced, replacedCard, beforeCards, beforeDamages, compositionBefore);
    }

    private void FinishCardAcquire(
        CardAcquireAction action, ItemData replacedCard,
        ItemData[] beforeCards, float[] beforeDamages, CompositionType compositionBefore)
    {
        var cardInventory = CardInventory.Instance;
        var afterCards = ToArray(cardInventory.Slots);
        var afterDamages = CardDamageSystem.GetSlotDamages(cardInventory.Slots, cardInventory.UpgradeLevels, cardInventory.PlayerLevel);
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
