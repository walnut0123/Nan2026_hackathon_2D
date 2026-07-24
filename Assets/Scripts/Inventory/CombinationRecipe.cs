using UnityEngine;

[CreateAssetMenu(fileName = "NewCombinationRecipe", menuName = "Inventory/Combination Recipe")]
public class CombinationRecipe : ScriptableObject
{
    public ItemData ingredientA;
    public ItemData ingredientB;
    public ItemData resultItem;
    public int resultCount = 1;

    public bool Matches(ItemData a, ItemData b)
    {
        return (ingredientA == a && ingredientB == b) || (ingredientA == b && ingredientB == a);
    }
}
