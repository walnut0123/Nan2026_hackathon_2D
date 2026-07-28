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

    // 새 게임 시작 시 채워지는 기본 카드. 카드가 0장이면 CardAutoAttack이 던질 게 없어 공격
    // 자체가 불가능해지므로(카드를 던져야 싸우는 설계라 몬스터/vase를 처치해서 카드를 얻는 것도
    // 막힘) 최소 1장은 항상 보장한다. GameManager.ApplyLoadedData가 기존 세이브를 불러올 때는
    // 이 값과 무관하게 Clear() 후 저장된 카드로 덮어쓴다.
    [SerializeField] private List<ItemData> starterCards = new List<ItemData>();

    private readonly ItemData[] slots = new ItemData[SlotCount];
    public IReadOnlyList<ItemData> Slots => slots;

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        for (int i = 0; i < starterCards.Count && i < SlotCount; i++)
            slots[i] = starterCards[i];
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

    /// <summary>Directly places a card into a slot by index, bypassing the "first empty
    /// slot" rule of TryAddCard. Used by the save system to restore exact slot layout.</summary>
    public void SetSlot(int index, ItemData card)
    {
        if (index < 0 || index >= slots.Length)
            return;

        slots[index] = card;
    }

    public void Clear()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i] = null;
    }

    public void NotifyChanged() => OnChanged?.Invoke();
}
