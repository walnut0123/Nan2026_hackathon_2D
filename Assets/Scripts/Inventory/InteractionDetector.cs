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
