using System;
using System.Collections;
using UnityEngine;

// 몬스터 스폰 2단계(플래시)용 화이트 원형 VFX. BossIndicatorUtil이 이미 절차적으로 생성해두는
// 원형 스프라이트를 그대로 재사용한다(별도 텍스처/에셋 임포트 없이 SpriteRenderer만으로 표현).
// 작은 크기에서 큰 크기로 팽창하며 페이드아웃하고, 끝나면 스스로 파괴된다.
[RequireComponent(typeof(SpriteRenderer))]
public class SpawnFlashBurst : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = BossIndicatorUtil.GetFilledCircleSprite();
    }

    /// <summary>플래시 재생을 시작한다. startScale -> endScale로 팽창하며 알파가 1 -> 0으로 페이드아웃한다.</summary>
    public void Play(float duration, float startScale, float endScale, Color color, Action onComplete = null)
    {
        StartCoroutine(PlayRoutine(duration, startScale, endScale, color, onComplete));
    }

    private IEnumerator PlayRoutine(float duration, float startScale, float endScale, Color color, Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

            float scale = Mathf.Lerp(startScale, endScale, t);
            transform.localScale = Vector3.one * scale;

            Color c = color;
            c.a = Mathf.Lerp(color.a, 0f, t);
            spriteRenderer.color = c;

            yield return null;
        }

        onComplete?.Invoke();
        Destroy(gameObject);
    }
}
