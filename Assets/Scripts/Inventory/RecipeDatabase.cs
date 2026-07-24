using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipeDatabase", menuName = "Inventory/Recipe Database")]
public class RecipeDatabase : ScriptableObject
{
    public List<CombinationRecipe> recipes = new List<CombinationRecipe>();

    public CombinationRecipe FindRecipe(ItemData a, ItemData b)
    {
        foreach (var recipe in recipes)
        {
            if (recipe != null && recipe.Matches(a, b))
                return recipe;
        }

        return null;
    }
}
