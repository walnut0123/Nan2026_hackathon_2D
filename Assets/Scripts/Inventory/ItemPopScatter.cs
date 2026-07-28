using System.Collections;
using UnityEngine;

/// <summary>Runtime-only (AddComponent'd, not on prefabs) arc-lerp motion for a freshly dropped
/// item: eases from the drop origin out to a scattered landing point with a small pop arc.
/// Disables ItemBob for the duration - ItemBob.Update() unconditionally overwrites
/// localPosition every frame, which fought CardProjectile's own motion the same way once before
/// (see CardProjectile.Initialize), so the same guard is needed here.</summary>
public class ItemPopScatter : MonoBehaviour
{
    public void Begin(Vector3 start, Vector3 end, float popHeight, float duration)
    {
        var bob = GetComponent<ItemBob>();
        if (bob != null)
            bob.enabled = false;

        StartCoroutine(Animate(start, end, popHeight, duration, bob));
    }

    private IEnumerator Animate(Vector3 start, Vector3 end, float popHeight, float duration, ItemBob bob)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - p, 3f);

            Vector3 pos = Vector3.Lerp(start, end, eased);
            pos.y += Mathf.Sin(p * Mathf.PI) * popHeight;
            transform.position = pos;

            yield return null;
        }

        transform.position = end;

        if (bob != null)
            bob.enabled = true;

        Destroy(this);
    }
}
