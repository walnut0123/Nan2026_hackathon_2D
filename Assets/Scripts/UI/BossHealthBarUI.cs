using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// PlayerHealthBarUI와 완전히 동일한 방식(초록 Bar는 즉시 갱신, 흰색 트레일은 잠시 남아있다가
// 서서히 줄어듦)이지만 대상이 플레이어가 아니라 씬의 BossAttackController를 가진 보스다.
public class BossHealthBarUI : MonoBehaviour
{
    [SerializeField] private Image barImage;
    [SerializeField] private Image whiteTrailImage;

    [Tooltip("흰색 트레일이 줄어들기 시작하기까지의 대기 시간(초). 연속 히트 중에는 히트마다 이 " +
        "대기가 다시 시작되어, 버스트가 끝날 때까지 흰색이 계속 쌓인 채로 유지된다.")]
    [SerializeField] private float whiteTrailHoldDelay = 0.4f;

    [Tooltip("대기가 끝난 뒤 흰색 트레일이 실제 체력 위치까지 줄어드는 데 걸리는 시간(초)")]
    [SerializeField] private float whiteTrailDrainDuration = 0.25f;

    [Tooltip("히트가 whiteTrailHoldDelay보다 촘촘하게 계속 들어오면 대기가 끝없이 밀려서 흰색이 " +
        "영원히 안 사라지는 것을 막는 상한(초) - 이 그룹의 첫 히트로부터 이 시간이 지나면, " +
        "더 맞고 있어도 강제로 드레인을 시작한다.")]
    [SerializeField] private float whiteTrailMaxHoldWindow = 1.5f;

    private Health bossHealth;
    private Coroutine whiteTrailRoutine;
    private float lastWhiteTrailHitTime;
    private float firstWhiteTrailHitTimeInGroup = float.NegativeInfinity;

    private void Start()
    {
        var boss = FindObjectOfType<BossAttackController>(true);
        if (boss != null)
            bossHealth = boss.GetComponent<Health>();

        if (bossHealth != null)
        {
            bossHealth.OnDamaged += HandleDamaged;
            bossHealth.OnDeath += HandleDeath;
        }
        else
        {
            Debug.LogWarning("[BossHealthBarUI] 보스의 Health를 찾지 못했습니다.");
        }

        UpdateFill();
        if (whiteTrailImage != null && barImage != null)
            whiteTrailImage.fillAmount = barImage.fillAmount;
    }

    private void OnDestroy()
    {
        if (bossHealth != null)
        {
            bossHealth.OnDamaged -= HandleDamaged;
            bossHealth.OnDeath -= HandleDeath;
        }
    }

    private void HandleDamaged(float amount)
    {
        float oldFillAmount = barImage.fillAmount;

        UpdateFill();

        if (whiteTrailImage != null)
        {
            whiteTrailImage.fillAmount = Mathf.Max(whiteTrailImage.fillAmount, oldFillAmount);

            if (Time.time - firstWhiteTrailHitTimeInGroup >= whiteTrailMaxHoldWindow)
                firstWhiteTrailHitTimeInGroup = Time.time;

            lastWhiteTrailHitTime = Time.time;

            if (whiteTrailRoutine != null)
                StopCoroutine(whiteTrailRoutine);
            whiteTrailRoutine = StartCoroutine(WhiteTrailDrainEffect());
        }
    }

    // 보스가 죽으면 체력바 전체(프레임/바/이름표 등 이 오브젝트의 모든 자식)를 통째로 숨긴다.
    private void HandleDeath()
    {
        UpdateFill();
        gameObject.SetActive(false);
    }

    private void UpdateFill()
    {
        if (bossHealth == null || bossHealth.MaxHealth <= 0 || barImage == null)
            return;

        barImage.fillAmount = Mathf.Clamp01((float)bossHealth.CurrentHealth / bossHealth.MaxHealth);
    }

    private IEnumerator WhiteTrailDrainEffect()
    {
        while (Time.time - lastWhiteTrailHitTime < whiteTrailHoldDelay
            && Time.time - firstWhiteTrailHitTimeInGroup < whiteTrailMaxHoldWindow)
        {
            float quietRemaining = whiteTrailHoldDelay - (Time.time - lastWhiteTrailHitTime);
            float capRemaining = whiteTrailMaxHoldWindow - (Time.time - firstWhiteTrailHitTimeInGroup);
            yield return new WaitForSeconds(Mathf.Min(quietRemaining, capRemaining));
        }

        float startFill = whiteTrailImage.fillAmount;
        float targetFill = barImage.fillAmount;
        float elapsed = 0f;

        while (elapsed < whiteTrailDrainDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / whiteTrailDrainDuration);
            whiteTrailImage.fillAmount = Mathf.Lerp(startFill, targetFill, t);
            yield return null;
        }

        whiteTrailImage.fillAmount = targetFill;
        whiteTrailRoutine = null;
    }
}
