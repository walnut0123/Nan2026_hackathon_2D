using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>카드 획득 처리 결과. CardInventory.Acquire()의 반환값이자
/// CardDamageSystem.OfferPreview의 시뮬레이션 결과 태그로도 쓰인다.</summary>
public enum CardAcquireAction
{
    Added,      // 빈 슬롯에 신규 추가
    Upgraded,   // 이미 보유한 카드라 강화(+1)
    Replaced    // 5칸이 다른 카드로 꽉 차 있어 가장 약한 슬롯을 자동 교체
}

/// <summary>
/// 인벤토리 슬롯 하나의 데이터. 카드 참조와 그 슬롯의 강화 수치를 한 덩어리로 묶어서,
/// "이 슬롯에 뭐가 들었나"를 둘로 쪼개진 배열 두 개를 매번 같이 맞춰가며 관리할 필요가
/// 없게 한다(예전엔 ItemData[] slots와 int[] upgradeLevels를 나란히 두고 Acquire/SetSlot/
/// Clear 등 모든 변경 지점에서 인덱스를 수동으로 동기화해야 했다).
/// </summary>
[Serializable]
public struct CardSlot
{
    public ItemData card;
    public int upgradeLevel;
}

// Dedicated 5-slot inventory for cards only, separate from PlayerInventory.
// v4.1: 슬롯 데이터를 CardSlot[] 하나로 통합. CardDamageSystem의
// (레벨데미지+액면가보너스+강화)×배율 공식이 카드 참조와 강화 수치를 같이 필요로 하므로
// (CardDamageSystem.cs 1~2번 항목 참고), 슬롯 하나의 두 값이 항상 같은 인덱스를 가리키도록
// 묶어두는 편이 더 안전하다.
public class CardInventory : MonoBehaviour
{
    public static CardInventory Instance { get; private set; }

    public const int SlotCount = 5;

    // 카드 2회 획득당 플레이어 레벨 1 상승 (1회 획득=Lv1, 3회=Lv2, 5회=Lv3 ...).
    // CardDamageSystem.cs 2번 항목 참고.
    private const int AcquisitionsPerLevel = 2;

    // 새 게임 시작 시 채워지는 기본 카드. 카드가 0장이면 CardAutoAttack이 던질 게 없어 공격
    // 자체가 불가능해지므로(카드를 던져야 싸우는 설계라 몬스터/vase를 처치해서 카드를 얻는 것도
    // 막힘) 최소 1장은 항상 보장한다. GameManager.ApplyLoadedData가 기존 세이브를 불러올 때는
    // 이 값과 무관하게 Clear() 후 저장된 카드로 덮어쓴다.
    [SerializeField] private List<ItemData> starterCards = new List<ItemData>();

    [Header("발사 방식")]
    [Tooltip("true면 슬롯 순서대로 순환 발사(완전 결정론), false면 무작위 추첨. 기본값은 무작위 - " +
             "CardDamageSystem.cs 5번 항목 참고. 디버그 재현 등 결정론이 필요할 때만 켤 것.")]
    [SerializeField] private bool useSequentialFire = false;

    private readonly CardSlot[] slots = new CardSlot[SlotCount];
    private int fireIndex = 0;

    // Slots/UpgradeLevels의 실제 백킹 스토어는 slots(CardSlot[]) 하나뿐이다. 아래 두 뷰는
    // 그 배열을 인덱싱만 해서 보여주는 얇은 어댑터로, 외부에서 보는 IReadOnlyList<ItemData>/
    // IReadOnlyList<int> 계약은 리팩터링 이전과 완전히 동일하다 - CardProjectile,
    // CardAutoAttack, CardDamageSystem, 세이브 시스템, UI 등 어떤 호출부도 고칠 필요가 없다.
    // Awake에서 생성한다 - MonoBehaviour에는 명시적 생성자를 두지 않는 게 정석이다(Unity의
    // 객체 생성/직렬화 경로와 충돌할 수 있음).
    private ItemView itemView;
    private UpgradeView upgradeView;

    public IReadOnlyList<ItemData> Slots => itemView;

    /// <summary>Slots와 같은 인덱스의 슬롯별 강화 수치. 같은 카드를 중복 획득할 때마다 +1.</summary>
    public IReadOnlyList<int> UpgradeLevels => upgradeView;

    /// <summary>지금까지 카드를 획득한 총 횟수(신규 추가 + 중복 강화 모두 포함). 플레이어 레벨 산정에 쓰인다.</summary>
    public int TotalAcquired { get; private set; }

    /// <summary>CardDamageSystem.GetLevelDamage에 넘길 플레이어 레벨. 카드 2회 획득당 +1레벨.</summary>
    public int PlayerLevel => 1 + TotalAcquired / AcquisitionsPerLevel;

    public bool UseSequentialFire => useSequentialFire;

    public event Action OnChanged;

    private void Awake()
    {
        itemView = new ItemView(slots);
        upgradeView = new UpgradeView(slots);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        for (int i = 0; i < starterCards.Count && i < SlotCount; i++)
        {
            if (starterCards[i] == null) continue;
            slots[i].card = starterCards[i];
            TotalAcquired++;
        }
    }

    /// <summary>
    /// 카드 획득. 이미 보유한 카드(같은 itemId)면 그 슬롯을 강화(upgradeLevel+1)하고,
    /// 빈 칸이 있으면 새로 추가하고, 다른 카드로 5칸이 이미 꽉 찬 상태면 가장 약한 슬롯
    /// (FindWeakestSlot)을 자동으로 교체한다 - 항상 성공한다.
    /// </summary>
    public CardAcquireAction Acquire(ItemData card, out int targetSlot)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].card != null && slots[i].card.itemId == card.itemId)
            {
                slots[i].upgradeLevel++;
                TotalAcquired++;
                targetSlot = i;
                OnChanged?.Invoke();
                return CardAcquireAction.Upgraded;
            }
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].card == null)
            {
                slots[i] = new CardSlot { card = card, upgradeLevel = 0 };
                TotalAcquired++;
                targetSlot = i;
                OnChanged?.Invoke();
                return CardAcquireAction.Added;
            }
        }

        int weakest = FindWeakestSlot();
        Debug.Log($"[CardInventory] 인벤토리가 꽉 차 있어 가장 약한 슬롯 [{weakest}]({slots[weakest].card.itemName})를 {card.itemName}(으)로 교체합니다.");
        slots[weakest] = new CardSlot { card = card, upgradeLevel = 0 };
        TotalAcquired++;
        targetSlot = weakest;
        OnChanged?.Invoke();
        return CardAcquireAction.Replaced;
    }

    /// <summary>Acquire()의 bool 오버로드. 기존 호출부(ItemPickup 등) 호환용 - card가 null이 아닌 한
    /// 항상 true를 반환한다(자동 교체로 실패 케이스가 없어졌다).</summary>
    public bool TryAddCard(ItemData card)
    {
        if (card == null) return false;
        Acquire(card, out _);
        return true;
    }

    /// <summary>
    /// 보유 슬롯 중 가장 약한 슬롯의 인덱스(액면가 보너스 + 강화 수치가 최소인 칸).
    /// 빈 슬롯이 있으면 그 슬롯을 즉시 반환한다. Acquire()가 꽉 찼을 때 교체 대상을
    /// 고르는 기준이자, PreviewOffer가 "꽉 찼을 때" 시뮬레이션에 쓰는 기준이기도 하다.
    /// </summary>
    public int FindWeakestSlot()
    {
        int best = 0;
        float bestScore = float.MaxValue;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].card == null) return i;

            float score = CardDamageSystem.GetFaceBonus(slots[i].card.cardValue) + slots[i].upgradeLevel;
            if (score < bestScore) { bestScore = score; best = i; }
        }
        return best;
    }

    /// <summary>Directly places a card into a slot by index, bypassing the "first empty
    /// slot" rule of TryAddCard. Used by the save system to restore exact slot layout
    /// (including its saved upgrade level).</summary>
    public void SetSlot(int index, ItemData card, int upgradeLevel = 0)
    {
        if (index < 0 || index >= slots.Length)
            return;

        slots[index] = new CardSlot { card = card, upgradeLevel = card != null ? upgradeLevel : 0 };
    }

    /// <summary>세이브 로드 시 총 획득 횟수(=플레이어 레벨의 근거)를 직접 복원한다.
    /// 슬롯 강화 수치의 합만으로는 과거에 교체되어 사라진 카드의 획득 이력을 알 수 없어
    /// 별도로 저장/복원해야 한다.</summary>
    public void SetTotalAcquired(int count) => TotalAcquired = Mathf.Max(0, count);

    public void Clear()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i] = default;

        TotalAcquired = 0;
        fireIndex = 0;
    }

    public void NotifyChanged() => OnChanged?.Invoke();

    // ── 발사 ──

    /// <summary>
    /// 카드 1장을 뽑는다. useSequentialFire에 따라 순차/랜덤을 자동으로 분기한다
    /// (CardDamageSystem.cs 5번 항목 참고). 보유 카드가 하나도 없으면 slotIndex=-1과
    /// null을 반환한다.
    /// </summary>
    public ItemData DrawCard(out int slotIndex)
        => useSequentialFire ? DrawSequentialCard(out slotIndex) : GetRandomHeldCard(out slotIndex);

    /// <summary>보유 중인(비어있지 않은) 슬롯 중 하나를 무작위로 뽑는다.</summary>
    public ItemData GetRandomHeldCard(out int slotIndex)
    {
        int heldCount = 0;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].card != null) heldCount++;

        if (heldCount == 0)
        {
            slotIndex = -1;
            return null;
        }

        int pick = UnityEngine.Random.Range(0, heldCount);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].card == null) continue;

            if (pick == 0)
            {
                slotIndex = i;
                return slots[i].card;
            }
            pick--;
        }

        slotIndex = -1;
        return null;
    }

    /// <summary>fireIndex부터 슬롯을 한 바퀴 돌며 비어있지 않은 다음 카드를 찾는다(빈 슬롯은 건너뜀).
    /// 5칸이 전부 비어있으면 null을 반환한다. useSequentialFire=true일 때만 쓰인다.</summary>
    private ItemData DrawSequentialCard(out int slotIndex)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            int idx = fireIndex;
            fireIndex = (fireIndex + 1) % slots.Length;

            if (slots[idx].card != null)
            {
                slotIndex = idx;
                return slots[idx].card;
            }
        }

        slotIndex = -1;
        return null;
    }

    /// <summary>순차 발사 인덱스 초기화. 방 입장·부활 시 호출.</summary>
    public void ResetFireIndex() => fireIndex = 0;

    // ── 디버그 ──

    [ContextMenu("Debug: 카드 상태 리포트")]
    private void DebugPrintReport()
        => Debug.Log(CardDamageSystem.BuildReport(this, PlayerLevel));

    [ContextMenu("Debug: 랜덤 투척 편차 시뮬레이션")]
    private void DebugPrintVarianceSimulation()
        => Debug.Log(CardDamageSystem.SimulateVariance(this, PlayerLevel));

    // ── CardSlot[]을 읽기 전용으로 투영하는 얇은 어댑터 (할당 없이 인덱싱만 함) ──

    private sealed class ItemView : IReadOnlyList<ItemData>
    {
        private readonly CardSlot[] source;
        public ItemView(CardSlot[] source) => this.source = source;
        public ItemData this[int index] => source[index].card;
        public int Count => source.Length;
        public IEnumerator<ItemData> GetEnumerator()
        {
            foreach (var slot in source) yield return slot.card;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class UpgradeView : IReadOnlyList<int>
    {
        private readonly CardSlot[] source;
        public UpgradeView(CardSlot[] source) => this.source = source;
        public int this[int index] => source[index].upgradeLevel;
        public int Count => source.Length;
        public IEnumerator<int> GetEnumerator()
        {
            foreach (var slot in source) yield return slot.upgradeLevel;
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
