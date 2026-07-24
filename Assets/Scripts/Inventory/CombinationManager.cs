using System;
using UnityEngine;

public class CombinationManager : MonoBehaviour
{
    [SerializeField] private RecipeDatabase recipeDatabase;

    public event Action<CombinationRecipe> OnCombineSuccess;
    public event Action OnCombineFail;

    public bool TryCombine(PlayerInventory inventory, ItemData a, ItemData b)
    {
        if (inventory == null || a == null || b == null || recipeDatabase == null)
        {
            OnCombineFail?.Invoke();
            return false;
        }

        var recipe = recipeDatabase.FindRecipe(a, b);
        if (recipe == null)
        {
            Debug.Log($"[CombinationManager] No recipe for {a.itemName} + {b.itemName}");
            OnCombineFail?.Invoke();
            return false;
        }

        if (!inventory.TryRemoveItem(a, 1))
        {
            OnCombineFail?.Invoke();
            return false;
        }

        if (!inventory.TryRemoveItem(b, 1))
        {
            inventory.TryAddItem(a, 1);
            OnCombineFail?.Invoke();
            return false;
        }

        int leftover = inventory.TryAddItem(recipe.resultItem, recipe.resultCount);
        Debug.Log($"[CombinationManager] Combined {a.itemName} + {b.itemName} -> {recipe.resultItem.itemName} x{recipe.resultCount - leftover}");

        OnCombineSuccess?.Invoke(recipe);
        return true;
    }
}
