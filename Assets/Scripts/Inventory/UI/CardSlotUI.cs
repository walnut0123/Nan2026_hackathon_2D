using UnityEngine;
using UnityEngine.UI;

public class CardSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;

    public void SetCard(ItemData card)
    {
        if (icon == null) return;

        icon.sprite = card != null ? card.icon : null;
        icon.enabled = card != null && card.icon != null;
    }
}
