using System;
using UnityEngine;

// Always-visible display for the 5-slot CardInventory (no open/close toggle, unlike InventoryUI).
public class CardInventoryUI : MonoBehaviour
{
    public static CardInventoryUI Instance { get; private set; }

    [SerializeField] private CardSlotUI[] slots;

    private CardInventory cardInventory;
    private InteractionDetector detector;

    // 인벤토리가 꽉 찬 상태에서 새 카드를 주우려 할 때, 어느 슬롯을 내줄지 플레이어의 선택을
    // 기다리는 동안의 상태. 선택이 끝나면(또는 취소되면) 곧바로 비운다.
    private Action<int> pendingSwapCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

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

        // 교체 선택을 기다리는 도중 플레이어가 다른 곳으로 이동해 상호작용 대상이 바뀌면(그
        // 필드 카드에서 멀어졌다는 뜻) 선택 UI를 자동으로 취소한다 - 화면에 '변경' 버튼들만
        // 계속 떠 있는 채로 남는 걸 막는다.
        detector = FindObjectOfType<InteractionDetector>();
        if (detector != null)
            detector.OnPromptChanged += HandlePromptChanged;
    }

    private void OnDestroy()
    {
        if (cardInventory != null)
            cardInventory.OnChanged -= Refresh;

        if (detector != null)
            detector.OnPromptChanged -= HandlePromptChanged;
    }

    private void Refresh()
    {
        var cardSlots = cardInventory.Slots;
        for (int i = 0; i < slots.Length && i < cardSlots.Count; i++)
            slots[i].SetCard(cardSlots[i]);
    }

    private void HandlePromptChanged(string label)
    {
        if (pendingSwapCallback != null)
            CancelSwapSelection();
    }

    /// <summary>인벤토리 5칸이 이미 꽉 찬 상태에서 새 카드를 주우려 할 때 호출한다. 5개 슬롯
    /// 전부에 '변경' 오버레이를 띄우고, 플레이어가 그중 하나를 클릭하면 onSlotChosen(그 슬롯의
    /// 인덱스)을 호출한다.</summary>
    public void BeginSwapSelection(ItemData pendingCard, Action<int> onSlotChosen)
    {
        pendingSwapCallback = onSlotChosen;

        for (int i = 0; i < slots.Length; i++)
        {
            int slotIndex = i;
            slots[i].SetSwapMode(true, () => HandleSlotChosen(slotIndex));
        }
    }

    /// <summary>진행 중인 교체 선택을 취소한다(콜백 호출 없이 오버레이만 끈다).</summary>
    public void CancelSwapSelection()
    {
        pendingSwapCallback = null;
        for (int i = 0; i < slots.Length; i++)
            slots[i].SetSwapMode(false, null);
    }

    private void HandleSlotChosen(int slotIndex)
    {
        var callback = pendingSwapCallback;
        pendingSwapCallback = null;
        for (int i = 0; i < slots.Length; i++)
            slots[i].SetSwapMode(false, null);

        callback?.Invoke(slotIndex);
    }
}
