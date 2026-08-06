using System;
using System.Collections;
using UnityEngine;

// 몬스터 스폰 예고용 원형 링 VFX. LineRenderer로 원 둘레의 정점을 매 프레임 직접 계산해서
// 그린다(텍스처 불필요, PPU 정렬만 신경 쓰면 됨). 반지름이 큰 값에서 작은 값으로 EaseIn
// 곡선을 따라 수축하면서 알파가 0->1로 올라가고, 수축이 끝나면 onComplete로 알린다.
// 재생이 끝나면 스스로 파괴되므로 호출부(MonsterSpawnSequencer)가 정리할 필요가 없다.
[RequireComponent(typeof(LineRenderer))]
public class SpawnTelegraphRing : MonoBehaviour
{
    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
    }

    /// <summary>예고 링 재생을 시작한다. radiusStart -> radiusEnd로 EaseIn 수축, 알파는 0 -> 1로 상승한다.</summary>
    public void Play(float radiusStart, float radiusEnd, float duration, Color color, float lineWidth,
        int segments = 32, Action onComplete = null)
    {
        lineRenderer.widthMultiplier = lineWidth;
        StartCoroutine(PlayRoutine(radiusStart, radiusEnd, duration, color, segments, onComplete));
    }

    private IEnumerator PlayRoutine(float radiusStart, float radiusEnd, float duration, Color color,
        int segments, Action onComplete)
    {
        Vector3 center = transform.position;
        lineRenderer.positionCount = segments;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

            // EaseIn(t^2) - 초반엔 느리게, 후반엔 빠르게 수축해서 긴장감을 준다.
            float easedT = t * t;
            float radius = Mathf.Lerp(radiusStart, radiusEnd, easedT);
            float alpha = t;

            DrawCircle(center, radius);

            Color c = color;
            c.a = color.a * alpha;
            lineRenderer.startColor = c;
            lineRenderer.endColor = c;

            yield return null;
        }

        onComplete?.Invoke();
        Destroy(gameObject);
    }

    private void DrawCircle(Vector3 center, float radius)
    {
        int segments = lineRenderer.positionCount;
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            Vector3 point = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            lineRenderer.SetPosition(i, point);
        }
    }
}
