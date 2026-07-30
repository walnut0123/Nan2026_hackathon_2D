using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractionDetector : MonoBehaviour
{
    [SerializeField] private PlayerInventory playerInventory;

    private readonly List<IInteractable> interactablesInRange = new List<IInteractable>();
    private IInteractable currentPromptTarget;

    /// <summary>Fired when the nearest interactable changes. Null label = hide prompt.</summary>
    public event Action<string> OnPromptChanged;

    /// <summary>지금 프롬프트가 떠 있는 대상(없으면 null). 카드는 즉시 자동 획득되어 프롬프트를
    /// 거치지 않으므로 여기 잡히지 않는다 - 무기 등 버튼으로 줍는 일반 아이템에만 해당.</summary>
    public IInteractable CurrentTarget => currentPromptTarget;

    private void Awake()
    {
        if (playerInventory == null)
            playerInventory = GetComponentInParent<PlayerInventory>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var interactable = other.GetComponent<IInteractable>();
        if (interactable == null || interactablesInRange.Contains(interactable))
            return;

        if (interactable is ItemPickup pickup && pickup.itemData != null && pickup.itemData.itemType == ItemType.Card)
        {
            // 카드 투사체(CardAutoAttack.ShootSingleCard)는 필드/몬스터 드랍과 같은 프리팹을
            // 재사용하기 때문에 IInteractable 자체는 항상 붙어 있다 - 실제로 주울 수 있는
            // 상태인지는 ItemPickup.IsFieldDrop으로만 구분된다. 이 체크가 없으면 날아가는
            // 카드가 플레이어 트리거 범위를 스칠 때마다 잘못 반응한다.
            if (!pickup.IsFieldDrop)
                return;

            // 카드는 "줍기" 버튼을 기다리지 않고 범위에 들어오는 즉시 자동으로 줍는다
            // (ItemPickup.InteractAsCard가 CardAcquiredPopup을 알아서 띄운다). 다른 종류의
            // 아이템(무기 등)은 기존처럼 프롬프트+버튼 흐름을 그대로 쓴다.
            interactable.Interact(playerInventory);
            return;
        }

        interactablesInRange.Add(interactable);
        Debug.Log($"[InteractionDetector] In range: {other.name}");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var interactable = other.GetComponent<IInteractable>();
        if (interactable == null)
            return;

        if (interactablesInRange.Remove(interactable))
            Debug.Log($"[InteractionDetector] Out of range: {other.name}");
    }

    private void Update()
    {
        UpdatePrompt();
    }

    public void TryInteract()
    {
        interactablesInRange.RemoveAll(i => (i as MonoBehaviour) == null);

        var closest = GetClosest();
        if (closest == null)
        {
            Debug.Log("[InteractionDetector] No interactable in range.");
            return;
        }

        closest.Interact(playerInventory);
        interactablesInRange.Remove(closest);
        UpdatePrompt();
    }

    private void UpdatePrompt()
    {
        interactablesInRange.RemoveAll(i => (i as MonoBehaviour) == null);

        var closest = GetClosest();
        if (closest == currentPromptTarget)
            return;

        currentPromptTarget = closest;
        OnPromptChanged?.Invoke(closest == null ? null : GetLabel(closest));
    }

    private static string GetLabel(IInteractable interactable)
    {
        if (interactable is ItemPickup pickup && pickup.itemData != null)
            return pickup.itemData.itemName;

        var mb = interactable as MonoBehaviour;
        return mb != null ? mb.gameObject.name : "상호작용";
    }

    private IInteractable GetClosest()
    {
        IInteractable closest = null;
        float closestDist = float.MaxValue;

        foreach (var interactable in interactablesInRange)
        {
            var mb = interactable as MonoBehaviour;
            if (mb == null) continue;

            float dist = Vector3.Distance(transform.position, mb.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = interactable;
            }
        }

        return closest;
    }
}
