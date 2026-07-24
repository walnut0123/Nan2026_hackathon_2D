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

        if (CardInventory.Instance == null || !CardInventory.Instance.TryAddCard(itemData))
        {
            Debug.Log($"[ItemPickup] Card inventory full, could not pick up {itemData.itemName}");
            return;
        }

        Debug.Log($"[ItemPickup] Picked up card {itemData.itemName}");

        if (destroyOnFullPickup)
        {
            var entity = GetComponent<PersistentWorldEntity>();
            if (entity != null)
                GameManager.Instance?.MarkWorldObjectRemoved(entity.Id);

            Destroy(gameObject);
        }
    }
}
