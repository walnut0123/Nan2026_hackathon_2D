using System.Collections.Generic;
using UnityEngine;

/// <summary>Central "pop and scatter" drop presentation, shared by anything that drops field
/// items (breakables, and eventually MonsterDropper). Physics-impulse scatter doesn't work here
/// since item pickup prefabs have no Rigidbody2D (trigger-only colliders for pickup range) - so
/// this animates position directly via ItemPopScatter instead of relying on AddForce.</summary>
public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance { get; private set; }

    [SerializeField] private float scatterRadius = 1.2f;
    [SerializeField] private float popHeight = 0.6f;
    [SerializeField] private float scatterDuration = 0.35f;

    private void Awake() => Instance = this;

    public GameObject SpawnDrop(ItemData item, int count, Vector3 origin)
    {
        if (item == null)
            return null;

        if (item.worldPrefab == null)
        {
            Debug.LogWarning($"[ItemSpawner] {item.itemName} has no worldPrefab; skipping drop.");
            return null;
        }

        var dropped = Instantiate(item.worldPrefab, origin, Quaternion.identity);
        dropped.AddComponent<DroppedItemMarker>();

        var pickup = dropped.GetComponent<ItemPickup>();
        if (pickup != null)
        {
            pickup.itemData = item;
            pickup.count = count;
            pickup.MarkAsFieldDrop();
        }

        Vector2 dir = Random.insideUnitCircle.normalized;
        float distance = Random.Range(scatterRadius * 0.4f, scatterRadius);
        Vector3 landingPoint = origin + (Vector3)(dir * distance);

        dropped.AddComponent<ItemPopScatter>().Begin(origin, landingPoint, popHeight, scatterDuration);

        return dropped;
    }

    public void SpawnDrops(IEnumerable<(ItemData item, int count)> drops, Vector3 origin)
    {
        foreach (var (item, count) in drops)
            SpawnDrop(item, count, origin);
    }
}
