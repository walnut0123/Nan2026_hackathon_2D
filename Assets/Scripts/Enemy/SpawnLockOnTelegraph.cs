using System;
using System.Collections;
using UnityEngine;

// 몬스터 스폰 1단계(예고) VFX - Enter the Gungeon 스타일로, 플레이어가 적을 수동 타겟팅할 때
// 쓰는 것과 동일한 락온 브라켓 이미지(TargetLockVFX.prefab이 쓰는 custom_Target 스프라이트)를
// 그대로 재사용한다. 큰 브라켓이 스폰 지점 위에 나타나 안쪽으로 좁혀지며 "여기에 뭔가 잠기고
// 있다"는 락온 연출을 만든 뒤, 완전히 닫힌 채로 잠깐 멈췄다가(holdDuration) 완료를 알린다.
// TargetLockVFX.cs 자체를 재사용하지 않고 별도 스크립트로 둔 이유는, 그쪽은 플레이어 UX용으로
// 고정된 타이밍(1.6배 축소 후 1초 유지)을 갖고 있어 스폰 시퀀스가 필요로 하는 완료 콜백이 없고,
// 이 타이밍을 스폰 연출에 맞게 바꾸면 실제 플레이어 락온 연출에도 영향을 줄 위험이 있기 때문이다 -
// 이미지(스프라이트) 자산만 동일하게 재사용하고 재생 로직은 완전히 독립적으로 둔다.
[RequireComponent(typeof(SpriteRenderer))]
public class SpawnLockOnTelegraph : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    /// <summary>락온 브라켓 연출을 재생한다. startScale/endScale은 프리팹 기본 스케일에 곱해지는
    /// 배율이다 - 크게 벌어진 상태에서 endScale(보통 1)까지 좁혀지며 "잠기는" 느낌을 낸다.</summary>
    public void Play(float startScale, float endScale, float shrinkDuration, float holdDuration,
        Color color, float rotationDegrees, Action onComplete = null)
    {
        spriteRenderer.color = color;
        StartCoroutine(PlayRoutine(startScale, endScale, shrinkDuration, holdDuration, rotationDegrees, onComplete));
    }

    private IEnumerator PlayRoutine(float startScale, float endScale, float shrinkDuration,
        float holdDuration, float rotationDegrees, Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = shrinkDuration > 0f ? Mathf.Clamp01(elapsed / shrinkDuration) : 1f;

            // EaseIn(t^2) - 처음엔 느리게 벌어져 있다가 후반부에 빠르게 좁혀지며 "탁" 하고
            // 잠기는 느낌을 준다(기존 예고 링이 쓰던 것과 동일한 곡선 계열).
            float easedT = t * t;
            float scale = Mathf.Lerp(startScale, endScale, easedT);
            transform.localScale = baseScale * scale;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, rotationDegrees, easedT));

            yield return null;
        }

        transform.localScale = baseScale * endScale;
        transform.rotation = Quaternion.Euler(0f, 0f, rotationDegrees);

        if (holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        onComplete?.Invoke();
        Destroy(gameObject);
    }
}
