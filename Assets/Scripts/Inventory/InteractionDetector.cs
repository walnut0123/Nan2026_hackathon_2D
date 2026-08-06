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

    /// <summary>지금 프롬프트가 떠 있는 대상(없으면 null). 카드도 무기 등 다른 아이템과 동일하게
    /// 프롬프트+버튼을 거쳐야 줍는다.</summary>
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

        // 실제로 지금 상호작용 가능한지(CanInteract)는 여기서 걸러내지 않는다 - 날아가는 카드나
        // 아직 클리어 전인 보물상자처럼 "범위 안에 있지만 아직은 안 되는" 상태가 나중에(예: 방
        // 클리어) 바뀔 수 있으므로, 필터링은 매 프레임 다시 평가하는 GetClosest()에서 한다.
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
            if (!interactable.CanInteract)
                continue;

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
