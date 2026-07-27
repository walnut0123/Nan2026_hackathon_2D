using System.Collections;
using UnityEngine;

// 플레이어가 적을 클릭해서 우선 타겟으로 지정하는 순간, 그 적 위에 잠깐 나타나는 락온 연출.
// 배치해 둔 크기(= "1배")의 startScaleMultiplier배(기본 1.4배)로 시작해서 shrinkDuration에 걸쳐
// 원래 크기로 줄어든다. 다 줄어든 뒤에는 holdAfterShrink만큼 그 크기로 잠깐 더 보이다가 사라진다 -
// 줄어들자마자 바로 사라지면 너무 빨리 없어져서 눈에 잘 안 들어오기 때문.
public class TargetLockVFX : MonoBehaviour
{
    [SerializeField] private float startScaleMultiplier = 1.4f;
    [SerializeField] private float shrinkDuration = 0.2f;
    [Tooltip("1배로 다 줄어든 뒤, 그 크기 그대로 유지하다가 사라지기까지의 시간(초)")]
    [SerializeField] private float holdAfterShrink = 1f;

    private void Start()
    {
        StartCoroutine(ShrinkThenDisappear());
    }

    private IEnumerator ShrinkThenDisappear()
    {
        Vector3 restScale = transform.localScale;
        Vector3 startScale = restScale * startScaleMultiplier;
        transform.localScale = startScale;

        float elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkDuration);
            transform.localScale = Vector3.Lerp(startScale, restScale, t);
            yield return null;
        }

        transform.localScale = restScale;

        if (holdAfterShrink > 0f)
            yield return new WaitForSeconds(holdAfterShrink);

        Destroy(gameObject);
    }
}
