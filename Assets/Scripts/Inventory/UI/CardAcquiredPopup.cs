using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드를 획득했을 때 화면 중앙에 잠깐 떴다 사라지는 팝업. DamageTextDisplay와 같은
/// static Show() 진입점 패턴을 따른다 - 호출부(ItemPickup)는 이 컴포넌트를 직접 몰라도 된다.
/// </summary>
public class CardAcquiredPopup : MonoBehaviour
{
    public static CardAcquiredPopup Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image cardIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI effectText;
    [SerializeField] private TextMeshProUGUI synergyText;

    [Tooltip("팝업이 완전히 보이는 상태로 유지되는 시간(초). 이후 페이드아웃.")]
    [SerializeField] private float holdDuration = 1.6f;
    [SerializeField] private float fadeDuration = 0.35f;

    private Coroutine activeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 카드 획득 팝업을 띄운다. compositionBefore/After를 비교해서 이번 획득으로 새로
    /// 발동(또는 상위 단계로 갱신)한 구성이 있으면 시너지 줄을 같이 보여준다.
    /// </summary>
    /// <param name="replacedCard">action이 Replaced일 때, 자동교체로 밀려난 카드(획득 전 스냅샷).
    /// 그 외 액션에서는 무시된다.</param>
    public static void Show(
        ItemData card, CardAcquireAction action,
        CompositionType compositionBefore, CompositionType compositionAfter,
        ItemData replacedCard = null)
    {
        if (card == null)
            return;

        // 씬에는 기본적으로 비활성 상태로 저장돼 있다(디자인 편집 편의를 위한 기본값 - 하이어라키에서
        // 켜져 있으면 항상 화면에 떠 있게 되므로). 비활성 오브젝트는 Awake()가 아직 안 돌아서
        // Instance가 비어 있을 수 있으니, 처음 호출될 때 직접 찾아서 켜준다 - 이 SetActive(true)는
        // 외부(ItemPickup)에서 거는 것이라 "자기 자신을 끈 오브젝트가 다시 스스로 못 켜는" 문제와는
        // 다르다(그 문제는 Update()/코루틴처럼 켜져 있어야만 도는 로직이 스스로를 끌 때만 발생).
        if (Instance == null)
        {
            var found = FindObjectOfType<CardAcquiredPopup>(true);
            if (found == null) return;
            found.gameObject.SetActive(true);
        }

        Instance?.Display(card, action, compositionBefore, compositionAfter, replacedCard);
    }

    private void Display(
        ItemData card, CardAcquireAction action,
        CompositionType compositionBefore, CompositionType compositionAfter,
        ItemData replacedCard)
    {
        if (cardIcon != null)
        {
            cardIcon.sprite = card.icon;
            cardIcon.enabled = card.icon != null;
        }

        if (nameText != null)
            nameText.text = BuildNameText(card, action, replacedCard);

        if (effectText != null)
            effectText.text = BuildEffectText(card);

        bool synergyActivated = compositionAfter != CompositionType.None && compositionAfter != compositionBefore;
        if (synergyText != null)
        {
            synergyText.gameObject.SetActive(synergyActivated);
            if (synergyActivated)
            {
                synergyText.text = $"시너지 발동: {CardDamageSystem.GetCompositionName(compositionAfter)} " +
                                    $"(×{CardDamageSystem.GetMultiplier(compositionAfter):F2})";
            }
        }

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        // MonoBehaviour.StartCoroutine silently no-ops (with a console warning) on an inactive
        // GameObject, and Awake() leaves this object inactive - so it must be reactivated here,
        // synchronously, BEFORE starting the coroutine. Doing the SetActive(true) inside
        // ShowRoutine itself doesn't work: the coroutine would never get a chance to run its
        // first line in the first place.
        gameObject.SetActive(true);
        activeRoutine = StartCoroutine(ShowRoutine());
    }

    /// <summary>일반 획득/강화는 카드 이름만, 인벤토리가 꽉 차 플레이어가 직접 슬롯을 골라
    /// 교체했을 때는 "{밀려난 카드} > {새 카드}"로 무엇이 무엇으로 바뀌었는지 보여준다.</summary>
    private static string BuildNameText(ItemData card, CardAcquireAction action, ItemData replacedCard)
    {
        switch (action)
        {
            case CardAcquireAction.Upgraded:
                return $"{card.itemName} (강화)";
            case CardAcquireAction.Replaced:
                string replacedName = replacedCard != null ? replacedCard.itemName : "?";
                return $"카드 교체: {replacedName} > {card.itemName}";
            default:
                return card.itemName;
        }
    }

    /// <summary>숫자 카드는 액면가 보너스, 그림 카드는 특수효과(미구현 표시 포함)를 보여준다.</summary>
    private string BuildEffectText(ItemData card)
    {
        var faceEffect = CardDamageSystem.GetFaceEffect(card.cardValue);
        if (faceEffect != FaceEffect.None)
            return $"특수효과: {CardDamageSystem.GetFaceEffectName(faceEffect)} (구현 예정)";

        return $"액면가 보너스 +{CardDamageSystem.GetFaceBonus(card.cardValue):F1}";
    }

    private IEnumerator ShowRoutine()
    {
        yield return Fade(0f, 1f, fadeDuration);
        yield return new WaitForSeconds(holdDuration);
        yield return Fade(1f, 0f, fadeDuration);
        gameObject.SetActive(false);
        activeRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null)
            yield break;

        float t = 0f;
        canvasGroup.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
