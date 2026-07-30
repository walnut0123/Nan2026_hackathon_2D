using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  CardDamageSystem — NAN2026 카드 데미지 밸런스 설계서 & 구현 (v4.0)       ║
// ╚═══════════════════════════════════════════════════════════════════════════╝
//
// ──────────────────────────────────────────────────
//  1. 설계 원칙
// ──────────────────────────────────────────────────
//
//  ▸ 1발 데미지 = (레벨 데미지 + 카드 액면가 보너스 + 카드 강화) × 구성 배율 × 스테이지 계수
//
//  앞 세 항은 덧셈, 구성 배율·스테이지 계수만 곱셈이다.
//
// ──────────────────────────────────────────────────
//  2. 플레이어 레벨
// ──────────────────────────────────────────────────
//
//  카드를 2회 획득할 때마다 레벨이 1 오른다 (CardInventory.PlayerLevel).
//  레벨 데미지 = 2.0 + (레벨-1) × 1.6   (Lv1=2.0, Lv5=8.4, Lv10=16.4, Lv15=24.4)
//
// ──────────────────────────────────────────────────
//  3. 카드 액면가 보너스
// ──────────────────────────────────────────────────
//
//   카드   2    3    4    5    6    7    8    9   10    J    Q    K    A
//   보너스 0.0  0.5  1.0  1.5  2.0  2.5  3.0  3.5  4.0  4.5  5.0  5.5  6.0
//
// ──────────────────────────────────────────────────
//  4. 구성 배율 — 독립 트랙 (v4.0, 원본 설계서 그대로 전면 교체)
// ──────────────────────────────────────────────────
//
//  같은 숫자 / 같은 무늬 / 연속 숫자를 서로 독립적으로 판정해 그중 배율이 가장 높은
//  하나만 적용한다(중첩 곱셈 없음). 3장 단계부터 보상이 있어야 조합을 완성해가는
//  과정에서도 데미지가 계속 오르므로, "5장을 다 채워야만 보상"이었던 예전 방식보다
//  무늬·연속 루트를 실제로 선택하게 된다.
//
//  배율은 확률 순이 아니라 "몇 칸이 구속되는가"(난이도) 순으로 매겼다:
//    같은숫자4 : 4칸만 구속, 5번째 칸은 자유          → 가장 쉬움   → ×2.30
//    연속5     : 5칸 전부 구속                        → 어려움     → ×2.45
//    같은무늬5 : 5칸 전부 구속 + 무늬 제약             → 가장 어려움 → ×2.60
//  같은숫자4를 확률 기준대로 가장 비싸게(×2.70 이상) 두면 "같은 숫자만 모으기"가
//  지배 전략이 되어 다른 루트가 죽는다 - 반드시 이 순서를 유지할 것.
//
// ──────────────────────────────────────────────────
//  5. 발사 방식
// ──────────────────────────────────────────────────
//
//  기본은 랜덤 투척(CardInventory.GetRandomHeldCard) - 매 발 보유 슬롯 중 무작위로 하나.
//  CardInventory.useSequentialFire를 켜면 슬롯 순서대로 도는 완전 결정론 발사로 전환된다
//  (디버그/재현용).
//
// ──────────────────────────────────────────────────
//  6. 그림 카드 특수효과 — 자리만 마련 (미구현)
// ──────────────────────────────────────────────────
//
//  J=관통 / Q=유도 / K=폭발 / A=방어 무시. FaceEffect enum과 GetFaceEffect()만
//  준비해두고, 실제 투사체 관통·유도·스플래시 로직과 적 방어력 스탯은 아직 없다.
//  데미지 공식 자체에는 절대 개입하지 않는다.
//
// ──────────────────────────────────────────────────
//  7. 적 체력 밸런싱
// ──────────────────────────────────────────────────
//
//  이 프로젝트는 라운드/스테이지 구분이 없는 상시 월드라 고정된 회차별 HP표를
//  쓸 수 없다. EstimateEnemyHP(평균데미지, 목표타수)로 원하는 시점의 평균 데미지
//  기준 목표 타수(일반 1~3발 / 정예 6~10발 / 보스 40~60발)를 넣어 개별 적 프리팹의
//  maxHealth를 산출한다.
//
// ──────────────────────────────────────────────────
//  8. 변경 이력
// ──────────────────────────────────────────────────
//
//  v4.0 — 구성 배율 전면 교체 + 인벤토리/발사 옵션 보강
//    - PokerHandRank(9단계, 5장 완성해야 스트레이트/플러시 인정) → CompositionType
//      (같은숫자2/3/4, 투페어, 풀하우스, 같은무늬3/4/5, 연속3/4/5 독립 트랙)로 전면 교체.
//      3장/4장 단계 시너지가 부활함. HandTier(입문/도약/숙련/전설) enum은 신규 구성
//      체계와 맞지 않아 제거.
//    - EvaluateHand → EvaluateComposition. 5장 미만이어도 동일 로직으로 판정 가능해짐.
//    - GetHandMultiplier → GetMultiplier(CompositionType). GetCompositionName 신규.
//    - OfferPreview/PreviewOffer/PreviewOffers 신규 - "후보 카드 획득 시 변화율 미리보기"
//      백엔드 계산만 포팅. 후보 3장을 실제로 제시하는 UI/드랍 로직은 별도 설계가
//      필요해 이번엔 만들지 않았다(트리거 조건·패널 vs 필드 픽업 여부 등 결정 필요).
//    - SimulateVariance/BuildReport 신규 - 콘솔 디버그 리포트(CardInventory의
//      ContextMenu에서 호출).
//    - GetNextFireIndex 삭제 - CardInventory가 fireIndex를 자체적으로 들고 있음
//      (useSequentialFire 토글, CardInventory.cs 참고).
//
//  v3.0 — 덧셈 공식 전환 + 플레이어 레벨 + 카드 강화 + 랜덤 투척
//    - 공식 변경: 액면가 × 배율 → (레벨데미지 + 액면가보너스 + 강화) × 배율.
//    - CalculateShotDamage/GetAverageShotDamage/PreviewSwapDelta에
//      playerLevel, upgradeLevel(들) 파라미터 추가.
//    - GetLevelDamage, GetFaceBonus, EstimateEnemyHP 신규.
//    - FaceEffect enum·GetFaceEffect 신규 (미구현 스텁).
//    - 발사 방식: 순차(라운드로빈) → 랜덤 기본 전환.
//
//  v2.2 — BASE_SCALE 폐지, 데미지 float화 (이전 이력)
//    - 데미지를 정수로 반올림하지 않고 소수점 한 자리까지 그대로 계산·적용·표기.
//    - IDamageable.TakeDamage/Health.currentHealth·maxHealth/OnDamaged 이벤트도
//      전부 float로 변경.
//

/// <summary>
/// 5칸 카드 구성 등급. 같은 숫자 / 같은 무늬 / 연속 숫자를 독립적으로 판정해
/// 그중 배율이 가장 높은 하나만 적용한다(중첩 곱셈 없음). CardDamageSystem.cs 4번 항목 참고.
/// </summary>
public enum CompositionType
{
    None = 0,
    SameRank2,      // 같은 숫자 2장   ×1.25
    SameRank3,      // 같은 숫자 3장   ×1.55
    SameRank4,      // 같은 숫자 4장   ×2.30
    TwoPair,        // 투페어          ×1.45
    FullHouse,      // 풀하우스        ×2.10
    SameSuit3,      // 같은 무늬 3장   ×1.15
    SameSuit4,      // 같은 무늬 4장   ×1.60
    SameSuit5,      // 같은 무늬 5장   ×2.60
    Sequence3,      // 연속 숫자 3장   ×1.20
    Sequence4,      // 연속 숫자 4장   ×1.65
    Sequence5       // 연속 숫자 5장   ×2.45
}

/// <summary>
/// 그림 카드 특수 효과. 데미지 공식에는 절대 개입하지 않고(값은 그대로) 투사체 거동에만
/// 영향을 줄 예정이다. 현재는 매핑만 존재하고 실제 관통/유도/폭발/방어무시 로직은 미구현.
/// </summary>
public enum FaceEffect
{
    None = 0,       // 2~10
    Pierce,         // J : 적 관통 (미구현)
    Homing,         // Q : 유도 (미구현)
    Explosion,      // K : 착탄 시 소폭발 (미구현)
    IgnoreDefense   // A : 적 방어력 무시 (미구현, 적 방어력 스탯 자체가 아직 없음)
}

/// <summary>
/// 카드 데미지 밸런스를 담당하는 단 하나의 스크립트.
/// 구성 판정 · 배율표 · 데미지 공식이 모두 이 파일 안에만 있으므로,
/// 밸런스를 바꿀 일이 생기면 이 파일만 수정하면 된다.
/// </summary>
public static class CardDamageSystem
{
    // ═══════════════════════════════════════════════════════════
    //  ① 레벨 데미지
    // ═══════════════════════════════════════════════════════════

    public const float LEVEL_BASE = 2f;    // Lv1 기본값
    public const float LEVEL_STEP = 1.6f;  // 레벨당 상승폭

    /// <summary>Lv1=2.0, Lv5=8.4, Lv10=16.4, Lv15=24.4</summary>
    public static float GetLevelDamage(int playerLevel)
        => LEVEL_BASE + (Mathf.Max(1, playerLevel) - 1) * LEVEL_STEP;

    // ═══════════════════════════════════════════════════════════
    //  ② 카드 액면가 보너스
    // ═══════════════════════════════════════════════════════════

    public const float FACE_STEP = 0.5f;

    /// <summary>액면가 보너스. (액면가 - 2) × 0.5. ItemData.cardValue는 2~14(A=14) 범위.</summary>
    public static float GetFaceBonus(int cardValue)
        => Mathf.Clamp(cardValue, 2, 14) * FACE_STEP - 2f * FACE_STEP;

    // ═══════════════════════════════════════════════════════════
    //  ③ 구성 배율표
    // ═══════════════════════════════════════════════════════════

    private static readonly Dictionary<CompositionType, float> Multipliers =
        new Dictionary<CompositionType, float>
    {
        { CompositionType.None,      1.00f },
        { CompositionType.SameRank2, 1.25f },
        { CompositionType.SameRank3, 1.55f },
        { CompositionType.SameRank4, 2.30f },
        { CompositionType.TwoPair,   1.45f },
        { CompositionType.FullHouse, 2.10f },
        { CompositionType.SameSuit3, 1.15f },
        { CompositionType.SameSuit4, 1.60f },
        { CompositionType.SameSuit5, 2.60f },
        { CompositionType.Sequence3, 1.20f },
        { CompositionType.Sequence4, 1.65f },
        { CompositionType.Sequence5, 2.45f },
    };

    public static float GetMultiplier(CompositionType type) => Multipliers[type];

    /// <summary>UI 표시용 한글 이름.</summary>
    public static string GetCompositionName(CompositionType t)
    {
        switch (t)
        {
            case CompositionType.SameRank2: return "같은 숫자 2장";
            case CompositionType.SameRank3: return "같은 숫자 3장";
            case CompositionType.SameRank4: return "포카드";
            case CompositionType.TwoPair:   return "투페어";
            case CompositionType.FullHouse: return "풀하우스";
            case CompositionType.SameSuit3: return "같은 무늬 3장";
            case CompositionType.SameSuit4: return "같은 무늬 4장";
            case CompositionType.SameSuit5: return "플러시";
            case CompositionType.Sequence3: return "연속 3장";
            case CompositionType.Sequence4: return "연속 4장";
            case CompositionType.Sequence5: return "스트레이트";
            default: return "구성 없음";
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  ④ 그림 카드 효과 매핑 (미구현 스텁 — 어디서도 호출하지 않음)
    // ═══════════════════════════════════════════════════════════

    public const int PIERCE_TARGETS = 2;         // J - 차후 구현 시 사용할 상수
    public const float EXPLOSION_RATIO = 0.55f;   // K - 차후 구현 시 사용할 상수

    /// <summary>카드 액면가로부터 그림 카드 특수효과를 매핑한다. 값은 J=11,Q=12,K=13,A=14 기준.
    /// 실제 투사체 거동은 아직 구현되어 있지 않다 - 매핑만 준비된 상태.</summary>
    public static FaceEffect GetFaceEffect(int cardValue)
    {
        switch (cardValue)
        {
            case 11: return FaceEffect.Pierce;
            case 12: return FaceEffect.Homing;
            case 13: return FaceEffect.Explosion;
            case 14: return FaceEffect.IgnoreDefense;
            default: return FaceEffect.None;
        }
    }

    /// <summary>UI 표시용 한글 이름. FaceEffect.None이면 빈 문자열.</summary>
    public static string GetFaceEffectName(FaceEffect effect)
    {
        switch (effect)
        {
            case FaceEffect.Pierce: return "관통";
            case FaceEffect.Homing: return "유도";
            case FaceEffect.Explosion: return "폭발";
            case FaceEffect.IgnoreDefense: return "방어 무시";
            default: return "";
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  핵심 API: 데미지 계산
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 1발 데미지 계산.
    /// 공식: (레벨 데미지 + 던지는 카드의 액면가 보너스 + 그 카드의 강화 수치) × 구성 배율 × 스테이지 계수
    /// </summary>
    /// <param name="thrownCard">이번에 던지는 카드 1장</param>
    /// <param name="thrownCardUpgradeLevel">그 카드가 놓인 슬롯의 강화 수치(CardInventory.UpgradeLevels)</param>
    /// <param name="allCards">보유 중인 전체 카드 목록 (최대 5장, null 허용)</param>
    /// <param name="playerLevel">현재 플레이어 레벨(CardInventory.PlayerLevel)</param>
    /// <param name="composition">판정된 구성 (out)</param>
    /// <param name="stageCoefficient">스테이지 난이도 계수 (기본 1.0)</param>
    /// <returns>최종 데미지. 소수점 한 자리로 고정.</returns>
    public static float CalculateShotDamage(
        ItemData thrownCard,
        int thrownCardUpgradeLevel,
        IReadOnlyList<ItemData> allCards,
        int playerLevel,
        out CompositionType composition,
        float stageCoefficient = 1f)
    {
        composition = EvaluateComposition(allCards);
        float multiplier = Multipliers[composition];

        int cardValue = thrownCard != null ? thrownCard.cardValue : 0;
        float flat = GetLevelDamage(playerLevel) + GetFaceBonus(cardValue) + thrownCardUpgradeLevel;

        float raw = Mathf.Max(0.1f, flat * multiplier * stageCoefficient);
        return Mathf.Round(raw * 10f) / 10f;
    }

    /// <summary>구성 out 파라미터가 필요 없을 때 사용하는 오버로드.</summary>
    public static float CalculateShotDamage(
        ItemData thrownCard,
        int thrownCardUpgradeLevel,
        IReadOnlyList<ItemData> allCards,
        int playerLevel,
        float stageCoefficient = 1f)
        => CalculateShotDamage(thrownCard, thrownCardUpgradeLevel, allCards, playerLevel, out _, stageCoefficient);

    // ═══════════════════════════════════════════════════════════
    //  밸런스 테스트 & UI 헬퍼
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 현재 핸드의 평균 1발 데미지. 밸런스 테스트, 디버그 UI,
    /// 카드 교체 시 "데미지 변화 프리뷰"에 사용.
    /// </summary>
    /// <param name="cards">보유 카드 (슬롯 순서, null 허용)</param>
    /// <param name="upgradeLevels">cards와 같은 순서의 슬롯별 강화 수치. null이면 전부 0으로 취급.</param>
    /// <param name="playerLevel">현재 플레이어 레벨</param>
    public static float GetAverageShotDamage(
        IReadOnlyList<ItemData> cards,
        IReadOnlyList<int> upgradeLevels,
        int playerLevel,
        float stageCoefficient = 1f)
    {
        var held = FilterNull(cards);
        if (held.Count == 0) return 0f;

        CompositionType composition = EvaluateComposition(cards);
        float multiplier = Multipliers[composition];
        float levelDamage = GetLevelDamage(playerLevel);

        float sum = 0f;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == null) continue;
            int upgrade = (upgradeLevels != null && i < upgradeLevels.Count) ? upgradeLevels[i] : 0;
            float flat = levelDamage + GetFaceBonus(cards[i].cardValue) + upgrade;
            sum += flat * multiplier * stageCoefficient;
        }

        return sum / held.Count;
    }

    /// <summary>
    /// 카드 교체 시뮬레이션: 슬롯의 카드를 newCard로 바꿨을 때
    /// 평균 데미지 변화량을 미리 계산. UI에서 "+3.2 ▲" 같은 표시에 사용.
    /// 새로 들어오는 카드는 강화 0(신규 획득)으로 취급한다.
    /// </summary>
    public static float PreviewSwapDelta(
        IReadOnlyList<ItemData> cards,
        IReadOnlyList<int> upgradeLevels,
        int slotIndex,
        ItemData newCard,
        int playerLevel)
    {
        float currentAvg = GetAverageShotDamage(cards, upgradeLevels, playerLevel);

        var simCards = new List<ItemData>(cards);
        var simUpgrades = upgradeLevels != null ? new List<int>(upgradeLevels) : new List<int>();
        while (simUpgrades.Count < simCards.Count) simUpgrades.Add(0);

        simCards[slotIndex] = newCard;
        simUpgrades[slotIndex] = 0;

        float newAvg = GetAverageShotDamage(simCards, simUpgrades, playerLevel);
        return newAvg - currentAvg;
    }

    /// <summary>슬롯별 데미지 배열. 획득 전/후 스냅샷을 찍어 비교하는 디버그 UI
    /// (CardDamagePreviewDebugUI)가 쓴다 - 인벤토리를 실제로 바꾸지 않는 PreviewOffer류와 달리,
    /// 이미 확정된 카드 목록(현재든 시뮬레이션이든)을 그대로 넣어서 쓰는 순수 계산 헬퍼다.</summary>
    public static float[] GetSlotDamages(IReadOnlyList<ItemData> cards, IReadOnlyList<int> upgradeLevels, int playerLevel)
    {
        var result = new float[cards.Count];
        for (int i = 0; i < cards.Count; i++)
        {
            result[i] = cards[i] != null
                ? CalculateShotDamage(cards[i], upgradeLevels[i], cards, playerLevel)
                : 0f;
        }
        return result;
    }

    /// <summary>평균 1발 데미지로부터 목표 타수를 만족하는 적 HP를 역산한다.
    /// 일반몹 1~3발 / 정예 6~10발 / 보스 40~60발 기준으로 쓸 것 (7번 항목 참고).</summary>
    public static int EstimateEnemyHP(float averageDamage, int targetHits)
        => Mathf.Max(1, Mathf.RoundToInt(averageDamage * targetHits));

    // ═══════════════════════════════════════════════════════════
    //  카드 획득 미리보기 (3장 중 1택 UI 백엔드)
    // ═══════════════════════════════════════════════════════════
    //
    //  ★ 이 절은 계산 로직만 제공한다. "후보 3장을 실제로 제시하는" UI/드랍 트리거는
    //    별도 설계가 필요해(패널로 띄울지, 필드에 3장을 깔고 하나를 주우면 나머지가
    //    사라지는 방식으로 할지, 모든 몬스터 처치마다 발동할지 등) 이번 포팅에는
    //    포함하지 않았다. PreviewOffers()가 반환하는 목록을 그대로 UI에 바인딩하면 된다.

    /// <summary>후보 카드 하나를 획득했을 때의 변화 미리보기 결과.</summary>
    public struct OfferPreview
    {
        public ItemData card;
        public int targetSlot;                     // 강화/추가/교체될 슬롯
        public CardAcquireAction action;
        public float currentAverage;
        public float newAverage;
        public CompositionType newComposition;
        public FaceEffect effect;

        public float DeltaPercent => currentAverage <= 0f ? 0f
            : (newAverage / currentAverage - 1f) * 100f;

        /// <summary>UI 문구. 예: "♦K · 플러시 · +18%"</summary>
        public string Describe()
        {
            string tag = action == CardAcquireAction.Upgraded
                ? "강화" : GetCompositionName(newComposition);
            string sign = DeltaPercent >= 0f ? "+" : "";
            return $"{(card != null ? card.itemName : "?")} · {tag} · {sign}{DeltaPercent:F0}%";
        }
    }

    /// <summary>
    /// 후보 카드를 획득했을 때의 변화를 미리 계산한다. 인벤토리는 변경하지 않는다.
    /// 꽉 찬 상태에서는 CardInventory.FindWeakestSlot()이 교체 대상이 된다
    /// (CardInventory.TryAddCard의 실제 자동교체 동작과 동일한 기준).
    /// </summary>
    public static OfferPreview PreviewOffer(CardInventory inventory, ItemData card, int playerLevel)
    {
        float currentAvg = GetAverageShotDamage(inventory.Slots, inventory.UpgradeLevels, playerLevel);
        SimulateAcquire(inventory, card, out var simCards, out var simUpgrades, out var action, out var target);

        return new OfferPreview
        {
            card = card,
            targetSlot = target,
            action = action,
            currentAverage = currentAvg,
            newAverage = GetAverageShotDamage(simCards, simUpgrades, playerLevel),
            newComposition = EvaluateComposition(simCards),
            effect = card != null ? GetFaceEffect(card.cardValue) : FaceEffect.None
        };
    }

    /// <summary>후보 여러 장을 평가해 변화율 내림차순으로 정렬한다.</summary>
    public static List<OfferPreview> PreviewOffers(
        CardInventory inventory, IEnumerable<ItemData> candidates, int playerLevel)
        => candidates.Select(c => PreviewOffer(inventory, c, playerLevel))
                     .OrderByDescending(p => p.DeltaPercent)
                     .ToList();

    /// <summary>슬롯 하나의 "줍기 전 > 줍은 후" 데미지 한 쌍. 디버그 UI(CardDamagePreviewDebugUI)가
    /// 인벤토리 5칸 전부를 비교 표시하는 데 쓴다. beforeCard/afterCard가 다르면 그 슬롯이
    /// 추가/교체된 슬롯이고, 카드가 같아도 구성 배율이 전역으로 바뀌면 데미지 값 자체는 달라질 수 있다.</summary>
    public struct SlotDamagePreview
    {
        public ItemData beforeCard;
        public float beforeDamage;
        public ItemData afterCard;
        public float afterDamage;
    }

    /// <summary>
    /// 후보 카드를 획득했다고 가정했을 때, 인벤토리 5칸 각각의 데미지가 "줍기 전 → 줍은 후"로
    /// 어떻게 바뀌는지 슬롯별로 계산한다. 인벤토리는 변경하지 않는다.
    /// candidate가 null이면 before=after로 채워진다(비교 대상 없음).
    /// </summary>
    public static SlotDamagePreview[] PreviewSlotDamages(CardInventory inventory, ItemData candidate, int playerLevel)
    {
        var beforeCards = inventory.Slots;
        var beforeUpgrades = inventory.UpgradeLevels;
        int count = beforeCards.Count;
        var result = new SlotDamagePreview[count];

        for (int i = 0; i < count; i++)
        {
            result[i].beforeCard = beforeCards[i];
            result[i].beforeDamage = beforeCards[i] != null
                ? CalculateShotDamage(beforeCards[i], beforeUpgrades[i], beforeCards, playerLevel)
                : 0f;
        }

        if (candidate == null)
        {
            for (int i = 0; i < count; i++)
            {
                result[i].afterCard = result[i].beforeCard;
                result[i].afterDamage = result[i].beforeDamage;
            }
            return result;
        }

        SimulateAcquire(inventory, candidate, out var simCards, out var simUpgrades, out _, out _);

        for (int i = 0; i < count; i++)
        {
            result[i].afterCard = simCards[i];
            result[i].afterDamage = simCards[i] != null
                ? CalculateShotDamage(simCards[i], simUpgrades[i], simCards, playerLevel)
                : 0f;
        }

        return result;
    }

    /// <summary>CardInventory.Acquire()와 동일한 규칙(중복이면 강화, 빈 칸이면 추가, 꽉 찼으면
    /// FindWeakestSlot 교체)으로 카드 획득을 시뮬레이션한다. 실제 인벤토리는 변경하지 않는다.
    /// PreviewOffer/PreviewSlotDamages가 공유하는 내부 헬퍼.</summary>
    private static void SimulateAcquire(
        CardInventory inventory, ItemData card,
        out List<ItemData> simCards, out List<int> simUpgrades,
        out CardAcquireAction action, out int target)
    {
        simCards = new List<ItemData>(inventory.Slots);
        simUpgrades = new List<int>(inventory.UpgradeLevels);

        int existing = simCards.FindIndex(c => c != null && card != null && c.itemId == card.itemId);
        if (existing >= 0)
        {
            action = CardAcquireAction.Upgraded;
            target = existing;
            simUpgrades[existing] = simUpgrades[existing] + 1;
            return;
        }

        int emptyIndex = simCards.FindIndex(c => c == null);
        if (emptyIndex >= 0)
        {
            action = CardAcquireAction.Added;
            target = emptyIndex;
            simCards[emptyIndex] = card;
            simUpgrades[emptyIndex] = 0;
            return;
        }

        action = CardAcquireAction.Replaced;
        target = inventory.FindWeakestSlot();
        simCards[target] = card;
        simUpgrades[target] = 0;
    }

    // ═══════════════════════════════════════════════════════════
    //  구성 판정 (Composition Evaluation)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 보유 중인 카드들(빈 슬롯 제외)에서 같은 숫자 / 같은 무늬 / 연속 숫자를 독립적으로
    /// 판정해 배율이 가장 높은 구성 하나를 반환한다. 5장 미만이어도 판정 가능
    /// (3장부터 시너지가 있어야 하는 설계 의도 - CardDamageSystem.cs 4번 항목 참고).
    /// </summary>
    public static CompositionType EvaluateComposition(IReadOnlyList<ItemData> cards)
    {
        var held = FilterNull(cards);
        if (held.Count == 0) return CompositionType.None;

        var rankGroups = held.GroupBy(c => c.cardValue).Select(g => g.Count())
                              .OrderByDescending(n => n).ToList();
        int maxRank = rankGroups[0];
        int maxSuit = held.GroupBy(c => c.suit).Max(g => g.Count());
        int maxSeq = LongestSequence(held.Select(c => c.cardValue).ToList());

        CompositionType best = CompositionType.None;
        float bestMult = Multipliers[CompositionType.None];

        void Consider(CompositionType t)
        {
            if (Multipliers[t] > bestMult) { bestMult = Multipliers[t]; best = t; }
        }

        if (maxRank >= 4) Consider(CompositionType.SameRank4);
        else if (maxRank == 3) Consider(CompositionType.SameRank3);
        else if (maxRank == 2) Consider(CompositionType.SameRank2);

        if (maxRank == 2 && rankGroups.Count > 1 && rankGroups[1] == 2)
            Consider(CompositionType.TwoPair);

        if (maxRank == 3 && rankGroups.Count > 1 && rankGroups[1] == 2)
            Consider(CompositionType.FullHouse);

        if (maxSuit >= 5) Consider(CompositionType.SameSuit5);
        else if (maxSuit == 4) Consider(CompositionType.SameSuit4);
        else if (maxSuit == 3) Consider(CompositionType.SameSuit3);

        if (maxSeq >= 5) Consider(CompositionType.Sequence5);
        else if (maxSeq == 4) Consider(CompositionType.Sequence4);
        else if (maxSeq == 3) Consider(CompositionType.Sequence3);

        return best;
    }

    /// <summary>
    /// 가장 긴 연속 숫자 구간의 길이.
    /// 에이스는 14(10-J-Q-K-A)와 1(A-2-3-4-5) 양쪽으로 계산해 유리한 쪽을 취한다.
    /// </summary>
    private static int LongestSequence(List<int> values)
    {
        int best = RunLength(values);
        if (values.Contains(14))
            best = Mathf.Max(best, RunLength(values.Select(v => v == 14 ? 1 : v).ToList()));
        return best;
    }

    private static int RunLength(List<int> src)
    {
        var d = src.Distinct().OrderBy(v => v).ToList();
        int longest = 1, cur = 1;
        for (int i = 1; i < d.Count; i++)
        {
            if (d[i] == d[i - 1] + 1) { cur++; longest = Mathf.Max(longest, cur); }
            else cur = 1;
        }
        return longest;
    }

    // ═══════════════════════════════════════════════════════════
    //  밸런싱 도구 (에디터 디버그용 - CardInventory의 ContextMenu에서 호출)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 랜덤 투척의 편차를 실측한다. 5발 누적 변동계수가 5%를 넘으면 액면가 기울기가
    /// 과한 신호다.
    /// </summary>
    public static string SimulateVariance(CardInventory inventory, int playerLevel, int windows = 20000)
    {
        var slots = inventory.Slots;
        var upgrades = inventory.UpgradeLevels;

        var heldIndices = new List<int>();
        for (int i = 0; i < slots.Count; i++)
            if (slots[i] != null) heldIndices.Add(i);

        if (heldIndices.Count == 0) return "인벤토리가 비어 있음";

        var dmgs = new float[heldIndices.Count];
        for (int i = 0; i < heldIndices.Count; i++)
        {
            int slot = heldIndices[i];
            dmgs[i] = CalculateShotDamage(slots[slot], upgrades[slot], slots, playerLevel);
        }

        var rng = new System.Random(12345);
        float sum = 0f, sumSq = 0f, min = float.MaxValue, max = float.MinValue;
        for (int w = 0; w < windows; w++)
        {
            float acc = 0f;
            for (int i = 0; i < 5; i++) acc += dmgs[rng.Next(dmgs.Length)];
            sum += acc; sumSq += acc * acc;
            if (acc < min) min = acc;
            if (acc > max) max = acc;
        }

        float mean = sum / windows;
        float sd = Mathf.Sqrt(Mathf.Max(0f, sumSq / windows - mean * mean));

        var sb = new StringBuilder();
        sb.AppendLine($"=== 랜덤 투척 편차 ({windows} 윈도우 x 5발) ===");
        sb.AppendLine($"슬롯별 데미지 : {string.Join(" / ", dmgs.Select(d => d.ToString("F1")))}");
        sb.AppendLine($"발당 최대/최소: x{dmgs.Max() / Mathf.Max(0.1f, dmgs.Min()):F2}");
        sb.AppendLine($"5발 누적 평균 : {mean:F1}");
        sb.AppendLine($"5발 누적 범위 : {min:F1} ~ {max:F1}");
        sb.AppendLine($"변동계수      : {sd / mean * 100f:F2}%  (5% 이하 권장)");
        sb.AppendLine($"최악 5발      : 평균의 {min / mean * 100f:F1}%");
        return sb.ToString();
    }

    /// <summary>현재 인벤토리 상태 리포트. 에디터에서 Debug.Log로 확인용.</summary>
    public static string BuildReport(CardInventory inventory, int playerLevel)
    {
        var slots = inventory.Slots;
        var upgrades = inventory.UpgradeLevels;

        CompositionType composition = EvaluateComposition(slots);
        float avg = GetAverageShotDamage(slots, upgrades, playerLevel);

        var sb = new StringBuilder();
        sb.AppendLine($"=== 카드 상태 (Lv{playerLevel}) ===");
        sb.AppendLine($"구성       : {GetCompositionName(composition)} x{Multipliers[composition]:F2}");
        sb.AppendLine($"레벨 데미지 : {GetLevelDamage(playerLevel):F1}");
        sb.AppendLine($"평타 평균   : {avg:F1}");
        sb.AppendLine($"투척 방식   : {(inventory.UseSequentialFire ? "순차" : "랜덤")}");

        sb.AppendLine("--- 슬롯 ---");
        for (int i = 0; i < slots.Count; i++)
        {
            var c = slots[i];
            if (c == null) { sb.AppendLine($"  [{i}] (비어있음)"); continue; }

            var effect = GetFaceEffect(c.cardValue);
            float dmg = CalculateShotDamage(c, upgrades[i], slots, playerLevel);
            sb.AppendLine($"  [{i}] {c.itemName,-8}"
                        + $" 액면 +{GetFaceBonus(c.cardValue):F1}"
                        + $" 강화 +{upgrades[i]}"
                        + $"  데미지 {dmg:F1}"
                        + (effect == FaceEffect.None ? "" : $"  ({effect})"));
        }

        sb.AppendLine("--- 권장 적 HP ---");
        sb.AppendLine($"  일반몹(1~3발) : {EstimateEnemyHP(avg, 1)} ~ {EstimateEnemyHP(avg, 3)}");
        sb.AppendLine($"  정예 (6~10발) : {EstimateEnemyHP(avg, 6)} ~ {EstimateEnemyHP(avg, 10)}");
        sb.AppendLine($"  보스 (40~60발): {EstimateEnemyHP(avg, 40)} ~ {EstimateEnemyHP(avg, 60)}");
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════
    //  내부 유틸리티
    // ═══════════════════════════════════════════════════════════

    /// <summary>null 슬롯을 제거한 카드 목록 반환.</summary>
    private static List<ItemData> FilterNull(IReadOnlyList<ItemData> cards)
        => cards.Where(c => c != null).ToList();
}
