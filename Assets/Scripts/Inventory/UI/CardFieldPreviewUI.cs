using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 필드에 놓인 카드에 가까이 다가가면(InteractionDetector가 그 카드를 가장 가까운 상호작용
// 대상으로 잡으면) 카드를 줍기 전에 효과를 미리 보여주는 상시 대기형 패널. 획득 직후 잠깐
// 떴다 사라지는 CardAcquiredPopup과 달리, 범위 안에 머무는 동안 계속 떠 있다가 범위를
// 벗어나면(또는 다른 대상으로 프롬프트가 바뀌면) 사라진다.
public class CardFieldPreviewUI : MonoBehaviour
{
    [SerializeField] private Image cardIcon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI effectText;

    private InteractionDetector detector;

    private void Start()
    {
        detector = FindObjectOfType<InteractionDetector>();
        if (detector != null)
            detector.OnPromptChanged += HandlePromptChanged;

        gameObject.SetActive(false);
        UIManager.Register("Card Field Preview", gameObject);
    }

    private void OnDestroy()
    {
        if (detector != null)
            detector.OnPromptChanged -= HandlePromptChanged;

        UIManager.Unregister("Card Field Preview");
    }

    private void HandlePromptChanged(string label)
    {
        var pickup = detector.CurrentTarget as ItemPickup;
        if (pickup == null || pickup.itemData == null || pickup.itemData.itemType != ItemType.Card)
        {
            gameObject.SetActive(false);
            return;
        }

        Display(pickup.itemData);
    }

    private void Display(ItemData card)
    {
        if (cardIcon != null)
        {
            cardIcon.sprite = card.icon;
            cardIcon.enabled = card.icon != null;
        }

        if (nameText != null)
            nameText.text = card.itemName;

        if (effectText != null)
            effectText.text = BuildEffectText(card);

        gameObject.SetActive(true);
    }

    /// <summary>CardAcquiredPopup.BuildEffectText와 동일한 규칙 - 숫자 카드는 액면가 보너스,
    /// 그림 카드는 특수효과(미구현 표시 포함)를 보여준다.</summary>
    private static string BuildEffectText(ItemData card)
    {
        var faceEffect = CardDamageSystem.GetFaceEffect(card.cardValue);
        if (faceEffect != FaceEffect.None)
            return $"특수효과: {CardDamageSystem.GetFaceEffectName(faceEffect)} (구현 예정)";

        return $"액면가 보너스 +{CardDamageSystem.GetFaceBonus(card.cardValue):F1}";
    }
}
