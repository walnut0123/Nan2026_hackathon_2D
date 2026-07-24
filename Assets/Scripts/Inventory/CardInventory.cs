using System;
using System.Collections.Generic;
using UnityEngine;

// Dedicated 5-slot inventory for cards only, separate from PlayerInventory.
// Which card sits in which slot will later drive a combat-buff algorithm - not implemented yet,
// this just holds the 5 slots and notifies the UI when they change.
public class CardInventory : MonoBehaviour
{
    public static CardInventory Instance { get; private set; }

    public const int SlotCount = 5;

    private readonly ItemData[] slots = new ItemData[SlotCount];
    public IReadOnlyList<ItemData> Slots => slots;

    public event Action OnChanged;

    // Sum of the held cards' rank values, used by CardProjectile's damage formula.
    public int TotalCardValue
    {
        get
        {
            int total = 0;
            foreach (var card in slots)
            {
                if (card != null)
                    total += card.cardValue;
            }
            return total;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool TryAddCard(ItemData card)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = card;
                OnChanged?.Invoke();
                return true;
            }
        }

        return false;
    }
}
