using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CardSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;

    [Tooltip("카드 인벤토리가 꽉 찼을 때, 이 슬롯을 새 카드로 교체하도록 제안하는 '변경' 오버레이" +
        "(반투명 배경 박스 + 버튼). 평소엔 비활성 상태.")]
    [SerializeField] private GameObject swapOverlayRoot;
    [SerializeField] private Button swapButton;

    public void SetCard(ItemData card)
    {
        if (icon == null) return;

        icon.sprite = card != null ? card.icon : null;
        icon.enabled = card != null && card.icon != null;
    }

    /// <summary>'변경' 오버레이를 켜고/끄고, 켤 때는 onClick을 버튼에 새로 연결한다.</summary>
    public void SetSwapMode(bool active, UnityAction onClick)
    {
        if (swapOverlayRoot != null)
            swapOverlayRoot.SetActive(active);

        if (swapButton == null)
            return;

        swapButton.onClick.RemoveAllListeners();
        if (active && onClick != null)
            swapButton.onClick.AddListener(onClick);
    }
}
