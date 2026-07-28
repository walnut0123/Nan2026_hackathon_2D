using System.Collections;
using UnityEngine;

// Health.OnDamaged를 구독해서 피격 시 플레이어 스프라이트를 순간적으로 빨갛게 물들였다가
// 원래 색으로 되돌린다. 실제 그리는 대상은 Player 루트가 아니라 자식(Visual)의 SpriteRenderer이므로
// 비워두면 자식에서 자동으로 찾는다.
[RequireComponent(typeof(Health))]
public class PlayerHitFlash : MonoBehaviour
{
    [Tooltip("피격 시 색을 바꿀 SpriteRenderer. 비워두면 자식에서 자동으로 찾음")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.15f;

    private Health health;
    private Color originalColor;
    private Coroutine flashRoutine;

    private void Awake()
    {
        health = GetComponent<Health>();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();

        if (targetRenderer != null)
            originalColor = targetRenderer.color;
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= HandleDamaged;
    }

    private void HandleDamaged(float amount)
    {
        if (targetRenderer == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        targetRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        targetRenderer.color = originalColor;
        flashRoutine = null;
    }
}
