using System;
using System.Collections.Generic;

public class Inventory
{
    public IReadOnlyList<InventorySlot> Slots => slots;
    public int Capacity => capacity;

    public event Action OnInventoryChanged;

    private readonly List<InventorySlot> slots;
    private readonly int capacity;

    public Inventory(int capacity)
    {
        this.capacity = capacity;
        slots = new List<InventorySlot>(capacity);
        for (int i = 0; i < capacity; i++)
            slots.Add(new InventorySlot());
    }

    /// <summary>Returns the leftover count that couldn't be added (0 = fully added).</summary>
    public int AddItem(ItemData item, int count)
    {
        if (item == null || count <= 0)
            return count;

        int remaining = count;

        foreach (var slot in slots)
        {
            if (remaining <= 0) break;
            if (slot.IsEmpty || slot.item != item) continue;

            int space = item.maxStackCount - slot.count;
            if (space <= 0) continue;

            int add = Math.Min(space, remaining);
            slot.count += add;
            remaining -= add;
        }

        foreach (var slot in slots)
        {
            if (remaining <= 0) break;
            if (!slot.IsEmpty) continue;

            int add = Math.Min(item.maxStackCount, remaining);
            slot.item = item;
            slot.count = add;
            remaining -= add;
        }

        if (remaining != count)
            OnInventoryChanged?.Invoke();

        return remaining;
    }

    public bool RemoveItem(ItemData item, int count)
    {
        if (item == null || count <= 0)
            return false;

        if (!HasItem(item, count))
            return false;

        int remaining = count;
        foreach (var slot in slots)
        {
            if (remaining <= 0) break;
            if (slot.IsEmpty || slot.item != item) continue;

            int remove = Math.Min(slot.count, remaining);
            slot.count -= remove;
            remaining -= remove;

            if (slot.count <= 0)
                slot.Clear();
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(ItemData item, int count)
    {
        if (item == null || count <= 0)
            return false;

        int total = 0;
        foreach (var slot in slots)
        {
            if (slot.IsEmpty || slot.item != item) continue;
            total += slot.count;
        }

        return total >= count;
    }

    /// <summary>Directly places an item into a slot by index, bypassing stacking rules. Used by the save system to restore exact slot layout.</summary>
    public void SetSlot(int index, ItemData item, int count)
    {
        if (index < 0 || index >= slots.Count)
            return;

        slots[index].item = item;
        slots[index].count = count;
    }

    public void Clear()
    {
        foreach (var slot in slots)
            slot.Clear();
    }

    public void NotifyChanged() => OnInventoryChanged?.Invoke();
}
