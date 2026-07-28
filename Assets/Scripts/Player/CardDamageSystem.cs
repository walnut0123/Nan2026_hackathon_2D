using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  CardDamageSystem — NAN2026 카드 데미지 밸런스 설계서 & 구현              ║
// ╚═══════════════════════════════════════════════════════════════════════════╝
//
// ──────────────────────────────────────────────────
//  1. 설계 원칙
// ──────────────────────────────────────────────────
//
//  이 게임의 데미지는 두 축으로 성장한다:
//    ① 개별 카드 가치  — 높은 숫자 카드를 주울수록 "한 발당" 데미지가 오른다.
//    ② 족보 배율       — 5장 조합이 좋을수록 "모든 발"에 곱연산이 적용된다.
//
//  플레이어는 바닥의 카드를 주울 때마다 두 판단을 해야 한다:
//    "이 카드가 내 족보를 올려줄까?"   → 배율 점프 (곱연산 성장)
//    "이 카드 숫자가 기존보다 높은가?" → 기본 데미지 상승 (합연산 성장)
//  두 판단이 교차하는 순간("족보는 깨지지만 숫자는 훨씬 높은 카드")이
//  카드 빌딩의 핵심 딜레마이자 재미다.
//
// ──────────────────────────────────────────────────
//  2. 데미지 공식
// ──────────────────────────────────────────────────
//
//  ▸ 1발 데미지 = 카드 액면가 × 족보 배율 × 스테이지 계수(k)
//
//  - 액면가: 2~10 = 숫자 그대로, J=11, Q=12, K=13, A=14
//  - 데미지는 정수로 반올림하지 않고 소수점 한 자리까지 그대로 계산/표기한다
//    (예: 카드 2 × 원페어(×1.15) = 2.3 데미지, 화면에도 "2.3"으로 표시).
//    예전에는 정수 반올림 손실을 피하려고 액면가에 10배(BASE_SCALE)를 곱하는
//    방식을 썼지만(v2.1), 데미지/체력 전체를 float로 바꿔서 폐지했다(v2.2) -
//    더 이상 정수 해상도를 확보할 필요 자체가 없어졌기 때문.
//
//  - 족보 배율: 보유 5장 전체를 상시 판정하여 결정 (아래 배율표 참조)
//  - 스테이지 계수(k): 기본 1.0. 난이도 미세 조정용 상수.
//    후반 스테이지에서 적 HP를 올리는 대신 k를 살짝 올려 성장감을 줄 수도 있고,
//    반대로 k를 줄여 "이 구간은 어렵게" 연출할 수도 있다.
//
// ──────────────────────────────────────────────────
//  3. 발사 방식: 순차 발사 (라운드로빈)
// ──────────────────────────────────────────────────
//
//  5장의 카드를 슬롯 순서대로 1→2→3→4→5→1→… 자동 발사한다.
//  한 발마다 "지금 던지는 카드"의 액면가가 기본 데미지가 되므로,
//  같은 족보라도 높은 카드 차례에는 강하고 낮은 카드 차례에는 약하다.
//  이 변동은 의도된 설계이며, 밸런싱은 5장의 "평균 1발 데미지" 기준으로 한다.
//
//  호출부 예시 (WeaponParent 등):
//    int fireIndex = 0;
//    void Fire() {
//        ItemData card = inventory[fireIndex];
//        float dmg = CardDamageSystem.CalculateShotDamage(card, inventory);
//        SpawnProjectile(dmg);
//        fireIndex = CardDamageSystem.GetNextFireIndex(fireIndex, inventory.Count);
//    }
//
// ──────────────────────────────────────────────────
//  4. 성장 곡선 — 4단계 티어
// ──────────────────────────────────────────────────
//
//  ┌─ 입문 티어 ─────────────────────────────────────────────────────┐
//  │  하이카드(×1.0) → 원페어(×1.15)                                │
//  │  성장폭: +15%. 체감은 약하지만, "페어를 맞추면 좋다"를 학습.    │
//  │  카드 1장만 맞으면 되므로 빠르게 벗어남. 튜토리얼 구간.         │
//  └─────────────────────────────────────────────────────────────────┘
//
//  ┌─ 도약 티어 ─────────────────────────────────────────────────────┐
//  │  투페어(×1.5) → 트리플(×1.8)                                   │
//  │  성장폭: +30%~+57% (하이카드 대비). 첫 번째 파워 스파이크.      │
//  │  "이 카드를 넣으면 투페어가 되겠는데?" — 카드 교체의 재미가      │
//  │  처음 느껴지는 구간. 트리플 도달 시 입문 대비 데미지 약 2배.     │
//  │                                                                 │
//  │  ★ 여기서 핵심 딜레마 발생:                                     │
//  │    예) 현재 [3,3,7,7,Q] 투페어(×1.5), 바닥에 K 등장.           │
//  │    K로 Q를 교체하면? → 투페어 유지, 평균값 +0.2 상승 (미미)     │
//  │    K로 3을 교체하면? → 원페어로 하락! 그러나 평균값 +2.0 상승    │
//  │    → "족보를 지킬까, 숫자를 올릴까?" 판단이 생긴다.              │
//  └─────────────────────────────────────────────────────────────────┘
//
//  ┌─ 숙련 티어 ─────────────────────────────────────────────────────┐
//  │  스트레이트(×2.2) → 플러시(×2.5) → 풀하우스(×2.8)              │
//  │  티어 내 성장폭은 작음 (+0.3씩). 그러나 도달 자체가 어렵다.     │
//  │  5장 전체를 의도적으로 맞춰야 하는 "빌드 완성" 구간.            │
//  │  도약 티어(트리플 ×1.8) → 숙련 티어(스트레이트 ×2.2) 전환 시    │
//  │  +22% 점프가 발생하여 진입 보상이 확실하다.                     │
//  │                                                                 │
//  │  ★ 이 구간에서 성장이 느리게 느껴지는 건 의도된 설계.           │
//  │    "이미 충분히 강하지만 더 극한까지 갈 수 있다"는 동기를 유지.  │
//  │    숙련 내에서는 카드 숫자 올리기가 주 성장 수단이 된다.         │
//  └─────────────────────────────────────────────────────────────────┘
//
//  ┌─ 전설 티어 ─────────────────────────────────────────────────────┐
//  │  포카드(×3.5) → 스트레이트 플러시(×5.0)                        │
//  │  달성 확률 극히 낮음. 도달하면 게임을 지배한다.                  │
//  │  풀하우스(×2.8) → 포카드(×3.5) 전환 시 +25% 폭등,              │
//  │  포카드(×3.5) → 스트레이트 플러시(×5.0) 전환 시 +43% 폭등.     │
//  │  "완벽한 빌드"에 대한 보상. 보스를 순삭하는 카타르시스.         │
//  └─────────────────────────────────────────────────────────────────┘
//
//  성장 체감 요약:
//    입문(느림) → 도약(빠름!) → 숙련(느림) → 전설(폭발!)
//    느린 구간: 입문 내부, 숙련 내부 (숫자 올리기로 보완)
//    빠른 구간: 입문→도약 전환, 숙련→전설 전환
//
// ──────────────────────────────────────────────────
//  5. 적 체력 밸런싱 기준표
// ──────────────────────────────────────────────────
//
//  목표: 일반몹 1~2타 / 정예 3~5타 / 보스 20~40타 (평균 1발 데미지 기준)
//  ※ v2.2부터 BASE_SCALE(10배) 폐지 - 아래 수치는 전부 그 이전 표를 10으로 나눈 값.
//    HP도 소수점을 허용하므로(Health.maxHealth가 float) 그대로 적용 가능.
//
//  스테이지  예상 보유패         족보       평균카드  1발평균   일반HP        정예HP         보스HP
//  ──────── ──────────────── ────────── ──────── ────────  ──────────  ────────────  ─────────────
//  1(초반)  [2,5,5,7,9]      원페어      5.6      6.4      6.4~12.8    19.2~32       128~256
//  2(중반초) [8,8,J,J,3]      투페어      8.2      12.3     12.3~24.6   36.9~61.5     246~492
//  3(중반)  [10,10,10,Q,5]   트리플      9.4      16.9     16.9~33.8   50.7~84.5     338~676
//  4(후반초) [9,10,J,Q,K]     스트레이트  11.0     24.2     24.2~48.4   72.6~121      484~968
//  5(후반)  [K,K,K,A,A]      풀하우스    12.6     35.3     35.3~70.6   105.9~176.5   706~1412
//  6(극후반) [K,K,K,K,A]      포카드      13.2     46.2     46.2~92.4   138.6~231     924~1848
//
//  ※ "1발평균" = 평균 카드값 × 족보 배율 (k=1.0 기준)
//  ※ 위 수치는 초기 밸런스 가이드라인. 반드시 플레이테스트로 검증할 것.
//  ※ 스테이지 계수(k)로 미세 조정 가능. 예: 3스테이지 보스를 더 어렵게 → k=0.9
//
// ──────────────────────────────────────────────────
//  6. 변경 이력
// ──────────────────────────────────────────────────
//
//  v2.2 — BASE_SCALE 폐지, 데미지 float화
//    - BASE_SCALE(10배) 상수 삭제. 데미지를 정수로 반올림하지 않고 소수점
//      한 자리까지 그대로 계산·적용·표기 (예: 2 대신 2.0) - float 자체가
//      반올림 손실 문제를 해결하므로 정수 해상도를 억지로 늘릴 필요가 없어졌다.
//    - CalculateShotDamage 반환 타입 int → float.
//    - IDamageable.TakeDamage/Health.currentHealth·maxHealth/OnDamaged 이벤트도
//      전부 float로 변경 - 데미지 정밀도가 실제로 체력에 반영되게 하기 위함.
//    - 적 HP 기준표를 BASE_SCALE 없는 수치로 재계산(전부 ÷10).
//    - 하위 호환용이던 CalculateDamage(int baseDamage, ...)는 BASE_SCALE
//      전제로 짜여 있던 구식 공식이라 삭제 (호출부 없음, CardProjectile은
//      이미 CalculateShotDamage만 사용).
//
//  v2.1 — 소수점 해상도 수정
//    - BASE_SCALE(=10) 도입. 카드값 × 10으로 정수 해상도 확보.
//      낮은 카드(2~4)에서 족보 배율 보너스가 반올림에 흡수되는 문제 해결.
//    - 적 HP 기준표를 BASE_SCALE 반영 수치로 재계산.
//    - CalculateShotDamage, GetAverageShotDamage, CalculateDamage(하위호환)
//      모두 BASE_SCALE 적용.
//
//  v2.0 — 전면 재설계
//    - 공식 변경: (baseDamage + rankSum) × 배율 → 개별카드값 × 배율 (순차 발사)
//    - 배율표: 임의 수치 → 확률 기반 로그 스케일 + 게임 체감 보정
//    - CalculateShotDamage 신규 추가 (1발 단위 데미지)
//    - GetNextFireIndex 신규 추가 (순차 발사 인덱스)
//    - GetAverageShotDamage 신규 추가 (밸런스 테스트용)
//    - GetHandTier 신규 추가 (UI 표시용 성장 티어)
//    - 기존 CalculateDamage는 하위 호환용으로 유지 (배율표만 신규 적용)
//

/// <summary>
/// 5칸 카드 조합(포커 족보) 등급.
/// HighCard가 가장 약하고 StraightFlush가 가장 강하다.
/// </summary>
public enum PokerHandRank
{
    HighCard,       // 하이카드       ×1.0
    OnePair,        // 원페어         ×1.15
    TwoPair,        // 투페어         ×1.5
    ThreeOfAKind,   // 트리플         ×1.8
    Straight,       // 스트레이트     ×2.2
    Flush,          // 플러시         ×2.5
    FullHouse,      // 풀하우스       ×2.8
    FourOfAKind,    // 포카드         ×3.5
    StraightFlush   // 스트레이트플러시 ×5.0
}

/// <summary>
/// 성장 구간 4단계. UI에서 현재 빌드 티어를 표시하거나,
/// 티어 전환 시 연출(이펙트, 사운드)을 트리거하는 데 사용.
/// </summary>
public enum HandTier
{
    Beginner,   // 입문: 하이카드, 원페어
    Breakout,   // 도약: 투페어, 트리플
    Expert,     // 숙련: 스트레이트, 플러시, 풀하우스
    Legendary   // 전설: 포카드, 스트레이트 플러시
}

/// <summary>
/// 카드 데미지 밸런스를 담당하는 단 하나의 스크립트.
/// 족보 판정 · 배율표 · 데미지 공식이 모두 이 파일 안에만 있으므로,
/// 밸런스를 바꿀 일이 생기면 이 파일만 수정하면 된다.
/// </summary>
public static class CardDamageSystem
{
    // ═══════════════════════════════════════════════════════════
    //  족보별 배율표
    // ═══════════════════════════════════════════════════════════
    //
    //  산출 근거: 52장 5-card 포커 확률의 역수를 로그 스케일링한 뒤,
    //  게임 체감에 맞게 보정. 공식: base = 1 + 0.5 × log10(P_하이카드 / P_족보)
    //
    //  족보             확률        로그기반   게임보정   보정 사유
    //  ──────────────  ──────────  ────────  ────────  ──────────────────
    //  하이카드         50.12%      1.00      1.0       기준점
    //  원페어           42.26%      1.04      1.15      +4%는 체감 불가 → +15%로 학습 보상
    //  투페어            4.75%      1.51      1.5       로그값 그대로. 첫 파워 스파이크
    //  트리플            2.11%      1.69      1.8       도약 티어 상한, 약간 상향
    //  스트레이트        0.39%      2.05      2.2       5장 완성 진입 보상 체감 상향
    //  플러시            0.20%      2.20      2.5       무늬 통일 난이도 반영
    //  풀하우스          0.14%      2.27      2.8       트리플+페어 복합 난이도 반영
    //  포카드            0.024%     2.66      3.5       전설 진입. 큰 점프 의도
    //  스트레이트플러시  0.0014%    3.28      5.0       최종 목표. 게임 지배 보상
    //
    private static readonly Dictionary<PokerHandRank, float> Multipliers =
        new Dictionary<PokerHandRank, float>
    {
        { PokerHandRank.HighCard,       1.0f  },
        { PokerHandRank.OnePair,        1.15f },
        { PokerHandRank.TwoPair,        1.5f  },
        { PokerHandRank.ThreeOfAKind,   1.8f  },
        { PokerHandRank.Straight,       2.2f  },
        { PokerHandRank.Flush,          2.5f  },
        { PokerHandRank.FullHouse,      2.8f  },
        { PokerHandRank.FourOfAKind,    3.5f  },
        { PokerHandRank.StraightFlush,  5.0f  },
    };

    // ═══════════════════════════════════════════════════════════
    //  핵심 API: 순차 발사 시스템
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 1발 데미지 계산 (순차 발사 시스템의 핵심 메서드).
    ///
    /// 공식: 던지는 카드 액면가 × 족보 배율 × 스테이지 계수
    ///
    /// 왜 "던지는 카드"를 분리하는가?
    /// → 5장 합산으로 퉁치면 카드 개개의 가치가 사라진다.
    ///   [2,2,2,2,K] 포카드와 [K,K,K,K,2] 포카드는 같은 족보이지만
    ///   전자는 K 차례에만 강하고, 후자는 거의 모든 발이 강하다.
    ///   개별 카드값을 살려야 "높은 숫자 카드를 모을 동기"가 유지된다.
    ///
    /// 왜 순차 발사(라운드로빈)인가?
    /// → 자동공격 기반이므로 플레이어가 매번 고를 수 없다.
    ///   랜덤 발사는 DPS 예측이 어려워 적 HP 밸런싱이 흔들린다.
    ///   순차 발사는 5발 주기가 항상 동일하여 평균 DPS가 확정적이다.
    /// </summary>
    /// <param name="thrownCard">이번 순번에 던지는 카드 1장</param>
    /// <param name="allCards">보유 중인 전체 카드 목록 (최대 5장, null 허용)</param>
    /// <param name="hand">판정된 족보 (out)</param>
    /// <param name="stageCoefficient">스테이지 난이도 계수 (기본 1.0)</param>
    /// <returns>최종 데미지. 소수점 한 자리로 고정(정수 반올림 없음) - 화면 표기(DamageTextDisplay)와
    /// 실제 적용(Health.TakeDamage) 양쪽 모두 이 값을 그대로 쓰므로 항상 서로 일치한다.</returns>
    public static float CalculateShotDamage(
        ItemData thrownCard,
        IReadOnlyList<ItemData> allCards,
        out PokerHandRank hand,
        float stageCoefficient = 1f)
    {
        var held = FilterNull(allCards);
        hand = EvaluateHand(held);
        float multiplier = Multipliers[hand];

        float value = thrownCard != null ? thrownCard.cardValue : 0f;
        float raw = Mathf.Max(0.1f, value * multiplier * stageCoefficient);
        return Mathf.Round(raw * 10f) / 10f;
    }

    /// <summary>족보 out 파라미터가 필요 없을 때 사용하는 오버로드.</summary>
    public static float CalculateShotDamage(
        ItemData thrownCard,
        IReadOnlyList<ItemData> allCards,
        float stageCoefficient = 1f)
        => CalculateShotDamage(thrownCard, allCards, out _, stageCoefficient);

    /// <summary>
    /// 순차 발사 인덱스 갱신. 호출부에서 fireIndex를 보관하고,
    /// 매 발사 후 이 메서드로 다음 인덱스를 받는다.
    /// </summary>
    public static int GetNextFireIndex(int currentIndex, int handSize)
    {
        if (handSize <= 0) return 0;
        return (currentIndex + 1) % handSize;
    }

    // ═══════════════════════════════════════════════════════════
    //  밸런스 테스트 & UI 헬퍼
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 현재 핸드의 평균 1발 데미지. 밸런스 테스트, 디버그 UI,
    /// 카드 교체 시 "데미지 변화 프리뷰"에 사용.
    /// </summary>
    public static float GetAverageShotDamage(IReadOnlyList<ItemData> cards, float stageCoefficient = 1f)
    {
        var held = FilterNull(cards);
        if (held.Count == 0) return 0f;

        PokerHandRank hand = EvaluateHand(held);
        float multiplier = Multipliers[hand];
        float avgValue = (float)held.Sum(c => c.cardValue) / held.Count;

        return avgValue * multiplier * stageCoefficient;
    }

    /// <summary>
    /// 카드 교체 시뮬레이션: 슬롯의 카드를 newCard로 바꿨을 때
    /// 평균 데미지 변화량을 미리 계산. UI에서 "+3.2 ▲" 같은 표시에 사용.
    /// </summary>
    /// <param name="cards">현재 보유 카드</param>
    /// <param name="slotIndex">교체할 슬롯 인덱스</param>
    /// <param name="newCard">새로 넣을 카드</param>
    /// <returns>평균 데미지 변화량 (양수=강해짐, 음수=약해짐)</returns>
    public static float PreviewSwapDelta(
        IReadOnlyList<ItemData> cards,
        int slotIndex,
        ItemData newCard)
    {
        float currentAvg = GetAverageShotDamage(cards);

        // 임시로 교체하여 계산
        var simulated = new List<ItemData>(cards);
        simulated[slotIndex] = newCard;
        float newAvg = GetAverageShotDamage(simulated);

        return newAvg - currentAvg;
    }

    /// <summary>현재 족보의 배율을 반환.</summary>
    public static float GetHandMultiplier(PokerHandRank hand) => Multipliers[hand];

    /// <summary>현재 족보가 속한 성장 티어를 반환. UI 연출용.</summary>
    public static HandTier GetHandTier(PokerHandRank hand)
    {
        switch (hand)
        {
            case PokerHandRank.HighCard:
            case PokerHandRank.OnePair:
                return HandTier.Beginner;

            case PokerHandRank.TwoPair:
            case PokerHandRank.ThreeOfAKind:
                return HandTier.Breakout;

            case PokerHandRank.Straight:
            case PokerHandRank.Flush:
            case PokerHandRank.FullHouse:
                return HandTier.Expert;

            case PokerHandRank.FourOfAKind:
            case PokerHandRank.StraightFlush:
                return HandTier.Legendary;

            default:
                return HandTier.Beginner;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  족보 판정 (Hand Evaluation)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 보유 중인 카드들(빈 슬롯 제외)에서 가장 높은 족보를 판정한다.
    /// 스트레이트·플러시·풀하우스 등 5장 완성 족보는 5장이 모두 채워졌을 때만 성립.
    /// 5장 미만이면 페어류(원페어, 투페어, 트리플, 포카드)만 인정.
    /// </summary>
    public static PokerHandRank EvaluateHand(IReadOnlyList<ItemData> heldCards)
    {
        if (heldCards.Count == 0)
            return PokerHandRank.HighCard;

        var rankCounts = heldCards
            .GroupBy(c => c.cardValue)
            .Select(g => g.Count())
            .OrderByDescending(n => n)
            .ToList();

        bool isFullHand = heldCards.Count == 5;
        bool isFlush    = isFullHand && heldCards.All(c => c.suit == heldCards[0].suit);
        bool isStraight = isFullHand && IsStraight(heldCards.Select(c => c.cardValue).ToList());

        // 판정 우선순위: 높은 족보부터 내려간다
        if (isStraight && isFlush)                                         return PokerHandRank.StraightFlush;
        if (rankCounts[0] == 4)                                            return PokerHandRank.FourOfAKind;
        if (rankCounts[0] == 3 && rankCounts.Count > 1 && rankCounts[1] == 2) return PokerHandRank.FullHouse;
        if (isFlush)                                                       return PokerHandRank.Flush;
        if (isStraight)                                                    return PokerHandRank.Straight;
        if (rankCounts[0] == 3)                                            return PokerHandRank.ThreeOfAKind;
        if (rankCounts[0] == 2 && rankCounts.Count > 1 && rankCounts[1] == 2) return PokerHandRank.TwoPair;
        if (rankCounts[0] == 2)                                            return PokerHandRank.OnePair;

        return PokerHandRank.HighCard;
    }

    // ═══════════════════════════════════════════════════════════
    //  내부 유틸리티
    // ═══════════════════════════════════════════════════════════

    /// <summary>null 슬롯을 제거한 카드 목록 반환.</summary>
    private static List<ItemData> FilterNull(IReadOnlyList<ItemData> cards)
        => cards.Where(c => c != null).ToList();

    /// <summary>
    /// 5장의 랭크가 연속인지 판정.
    /// 에이스는 로우 스트레이트(A,2,3,4,5)와 하이 스트레이트(10,J,Q,K,A) 양쪽 허용.
    /// </summary>
    private static bool IsStraight(List<int> values)
    {
        var distinct = values.Distinct().OrderBy(v => v).ToList();
        if (distinct.Count != 5)
            return false;

        if (IsSequential(distinct))
            return true;

        // ItemData.cardValue는 에이스를 14(하이)로 저장하므로, 10-J-Q-K-A 하이 스트레이트는
        // 위 IsSequential(distinct)에서 이미 잡힌다. 여기서는 반대로 A-2-3-4-5 로우 스트레이트를
        // 잡기 위해 14를 1로 되돌려서 다시 검사한다.
        var aceLow = distinct.Select(v => v == 14 ? 1 : v).OrderBy(v => v).ToList();
        return IsSequential(aceLow);
    }

    private static bool IsSequential(List<int> sortedValues)
    {
        for (int i = 1; i < sortedValues.Count; i++)
        {
            if (sortedValues[i] != sortedValues[i - 1] + 1)
                return false;
        }
        return true;
    }
}