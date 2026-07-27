using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// EnemyHealthBar의 흰색 트레일과 동일한 알고리즘: 초록 Bar는 피격 즉시 갱신되고, 방금 잃은
// 체력만큼은 흰색 트레일(Bar 바로 아래 레이어)이 그 자리에 남아있다가, 잠시 후 Lerp로 서서히
// Bar의 현재 위치까지 줄어든다. whiteTrailImage는 barImage와 같은 위치/크기의 Filled Image이고,
// Bar가 그 위에 그려지므로 실제로 눈에 보이는 흰색은 "Bar와 트레일의 fillAmount 차이" 구간,
// 즉 방금 깎인 부분뿐이다(바 전체가 하얗게 되지 않는다).
public class PlayerHealthBarUI : MonoBehaviour
{
    [SerializeField] private Image barImage;
    [SerializeField] private Image whiteTrailImage;

    [Tooltip("흰색 트레일이 줄어들기 시작하기까지의 대기 시간(초). 연속 히트 중에는 히트마다 이 " +
        "대기가 다시 시작되어, 버스트가 끝날 때까지 흰색이 계속 쌓인 채로 유지된다.")]
    [SerializeField] private float whiteTrailHoldDelay = 0.4f;

    [Tooltip("대기가 끝난 뒤 흰색 트레일이 실제 체력 위치까지 줄어드는 데 걸리는 시간(초)")]
    [SerializeField] private float whiteTrailDrainDuration = 0.25f;

    [Tooltip("히트가 whiteTrailHoldDelay보다 촘촘하게 계속 들어오면(여러 적에게 동시에 맞는 등) " +
        "대기가 끝없이 밀려서 흰색이 영원히 안 사라지는 것을 막는 상한(초) - 이 그룹의 첫 히트로부터 " +
        "이 시간이 지나면, 더 맞고 있어도 강제로 드레인을 시작한다.")]
    [SerializeField] private float whiteTrailMaxHoldWindow = 1.5f;

    private Health playerHealth;
    private Coroutine whiteTrailRoutine;
    private float lastWhiteTrailHitTime;
    private float firstWhiteTrailHitTimeInGroup = float.NegativeInfinity;

    private void Start()
    {
        var playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
            playerHealth = playerInventory.GetComponent<Health>();

        if (playerHealth != null)
        {
            playerHealth.OnDamaged += HandleDamaged;
            playerHealth.OnDeath += HandleDeath;
        }
        else
        {
            Debug.LogWarning("[PlayerHealthBarUI] 플레이어의 Health를 찾지 못했습니다.");
        }

        UpdateFill();
        if (whiteTrailImage != null && barImage != null)
            whiteTrailImage.fillAmount = barImage.fillAmount;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged -= HandleDamaged;
            playerHealth.OnDeath -= HandleDeath;
        }
    }

    private void HandleDamaged(int amount)
    {
        float oldFillAmount = barImage.fillAmount;

        // 초록 Bar는 항상 즉시(지연 없이) 실제 체력을 반영한다.
        UpdateFill();

        if (whiteTrailImage != null)
        {
            // 방금 잃은 만큼(oldFillAmount까지)은 흰색으로 남겨둔다. 이미 그보다 더 남아있으면
            // (연속 히트 도중이라 아직 못 줄어든 경우) 그대로 유지한다.
            whiteTrailImage.fillAmount = Mathf.Max(whiteTrailImage.fillAmount, oldFillAmount);

            // 이전 그룹이 이미 상한을 넘겨서 끝난 상태였다면(또는 그룹이 아예 없었다면) 새 그룹 시작.
            // bool 플래그 대신 경과 시간으로만 판단하므로, 코루틴이 중간에 죽는 예외적인 상황에서도
            // "영원히 true로 고착"되는 상태 자체가 존재하지 않는다.
            if (Time.time - firstWhiteTrailHitTimeInGroup >= whiteTrailMaxHoldWindow)
                firstWhiteTrailHitTimeInGroup = Time.time;

            lastWhiteTrailHitTime = Time.time;

            // 매 히트마다 항상 재시작 - 이전 코루틴이 어떤 이유로든 이미 죽어있었어도(핸들이 낡았어도)
            // StopCoroutine은 안전하게 무시되고, 새 코루틴이 확실히 스케줄된다.
            if (whiteTrailRoutine != null)
                StopCoroutine(whiteTrailRoutine);
            whiteTrailRoutine = StartCoroutine(WhiteTrailDrainEffect());
        }
    }

    private void HandleDeath() => UpdateFill();

    private void UpdateFill()
    {
        if (playerHealth == null || playerHealth.MaxHealth <= 0 || barImage == null)
            return;

        barImage.fillAmount = Mathf.Clamp01((float)playerHealth.CurrentHealth / playerHealth.MaxHealth);
    }

    // 마지막 히트로부터 whiteTrailHoldDelay만큼 조용해지면(또는 이 그룹의 첫 히트로부터
    // whiteTrailMaxHoldWindow가 지나면 강제로), 흰색 트레일을 그 시점의 실제 체력(Bar) 위치까지
    // whiteTrailDrainDuration에 걸쳐 서서히 줄인다. HandleDamaged는 코루틴을 재시작하지 않고
    // lastWhiteTrailHitTime만 갱신하므로 이 while 루프가 알아서 대기를 연장한다 - 단, 상한을
    // 넘으면 히트가 계속 들어와도 무시하고 드레인을 시작해서 "영원히 안 사라지는" 상황을 막는다.
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
