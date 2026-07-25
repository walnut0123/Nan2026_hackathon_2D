using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelBattleText;

// Thin bridge between gameplay code and the PixelBattleText plugin, so callers (CardProjectile,
// etc.) only need a world position and an amount - no per-caller TextAnimation wiring required.
public class DamageTextDisplay : MonoBehaviour
{
    public static DamageTextDisplay Instance { get; private set; }

    [SerializeField] private TextAnimation damageAnimation;

    // Extra breathing room above the target's own sprite top, so the number floats clearly
    // above the head instead of hugging the sprite edge.
    [SerializeField] private float headroomMargin = 0.3f;

    // Used only when the target has no SpriteRenderer to measure a real height from.
    [SerializeField] private float fallbackOffset = 1f;

    // Option 1 (active): random per-spawn spread so a burst of near-simultaneous hits doesn't
    // stack every number in the exact same spot.
    [SerializeField] private Vector2 positionJitter = new Vector2(0.3f, 0.15f);

    // Option 3 (kept for later, superseded above by option 1 - jitter):
    // hits landing on the same target are summed and shown as a single number instead of one
    // number per hit. Sliding window - every new hit pushes the flush time back out by
    // mergeWindow again, so a whole burst (e.g. 5 staggered cards) merges into one number as
    // long as no two consecutive hits are more than mergeWindow apart, regardless of how long
    // the burst runs in total.
    // [Tooltip("같은 대상에게 마지막 데미지가 들어온 뒤 이 시간(초)만큼 추가 데미지가 없으면 합산된 숫자를 표시합니다.")]
    // [SerializeField] private float mergeWindow = 0.3f;

    // Bug fix: a pure sliding window never closes if hits keep landing less than mergeWindow
    // apart forever (e.g. continuous auto-attack with cooldown/cardThrowDelay both smaller than
    // mergeWindow) - damage would silently accumulate and never actually display. This caps the
    // TOTAL time a group can stay open, regardless of how recently the last hit landed.
    // [Tooltip("연속 히트가 계속 이어져도, 첫 히트로부터 이 시간(초)이 지나면 강제로 표시합니다.")]
    // [SerializeField] private float maxMergeWindow = 1f;

    // private readonly Dictionary<Transform, int> pendingDamage = new Dictionary<Transform, int>();
    // private readonly Dictionary<Transform, float> lastHitTime = new Dictionary<Transform, float>();
    // private readonly Dictionary<Transform, float> firstHitTime = new Dictionary<Transform, float>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    ///<summary>Shows a damage number above the target's head immediately, offset by a small random jitter so overlapping hits don't stack exactly on top of each other.</summary>
    public static void ShowDamage(int amount, Transform target)
    {
        if (Instance == null || Instance.damageAnimation == null || target == null)
            return;

        Instance.DisplayImmediate(amount, target);
    }

    private void DisplayImmediate(int amount, Transform target)
    {
        var cam = Camera.main;
        if (cam == null)
            return;

        Vector3 worldPosition = target.position + GetHeadOffset(target) + GetJitter();
        Vector3 viewportPos = cam.WorldToViewportPoint(worldPosition);
        PixelBattleTextController.DisplayText(amount.ToString(), damageAnimation, viewportPos);
    }

    // Option 3's accumulate/flush methods (kept for later, see field comments above):
    // private void AccumulateDamage(int amount, Transform target)
    // {
    //     bool alreadyPending = pendingDamage.ContainsKey(target);
    //     pendingDamage[target] = alreadyPending ? pendingDamage[target] + amount : amount;
    //     lastHitTime[target] = Time.time;
    //
    //     if (!alreadyPending)
    //     {
    //         firstHitTime[target] = Time.time;
    //         StartCoroutine(FlushWhenQuiet(target));
    //     }
    // }
    //
    // private IEnumerator FlushWhenQuiet(Transform target)
    // {
    //     while (Time.time - lastHitTime[target] < mergeWindow
    //         && Time.time - firstHitTime[target] < maxMergeWindow)
    //     {
    //         float quietRemaining = mergeWindow - (Time.time - lastHitTime[target]);
    //         float capRemaining = maxMergeWindow - (Time.time - firstHitTime[target]);
    //         yield return new WaitForSeconds(Mathf.Min(quietRemaining, capRemaining));
    //     }
    //
    //     int total = pendingDamage[target];
    //     pendingDamage.Remove(target);
    //     lastHitTime.Remove(target);
    //     firstHitTime.Remove(target);
    //
    //     if (target == null)
    //         yield break;
    //
    //     var cam = Camera.main;
    //     if (cam == null)
    //         yield break;
    //
    //     Vector3 worldPosition = target.position + GetHeadOffset(target);
    //     Vector3 viewportPos = cam.WorldToViewportPoint(worldPosition);
    //     PixelBattleTextController.DisplayText(total.ToString(), damageAnimation, viewportPos);
    // }

    private Vector3 GetHeadOffset(Transform target)
    {
        var renderer = target.GetComponentInChildren<SpriteRenderer>();
        float height = renderer != null ? renderer.bounds.extents.y : fallbackOffset;
        return new Vector3(0f, height + headroomMargin, 0f);
    }

    // Option 1's jitter helper (active):
    private Vector3 GetJitter()
    {
        return new Vector3(
            Random.Range(-positionJitter.x, positionJitter.x),
            Random.Range(-positionJitter.y, positionJitter.y),
            0f);
    }
}
