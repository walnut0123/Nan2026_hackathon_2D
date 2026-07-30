using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 디버그 전용 UI. 정식 UI가 아니다 - 밸런스 확인을 위해 카드를 주웠을 때 인벤토리 5칸의
/// "줍기 전 > 줍은 후" 데미지를 잠깐 텍스트로 보여준다.
///
/// 카드가 InteractionDetector 범위에 들어오는 즉시 자동으로 줍히도록 바뀌면서(더 이상 "줍기"
/// 버튼을 기다리는 대기 상태가 없다), 예전처럼 "지금 주울 수 있는 카드가 있는가"를 매 프레임
/// 폴링하는 방식은 더 이상 성립하지 않는다 - 그 순간이 곧 실제로 주운 순간이라, ItemPickup이
/// 실제 획득 시점에 스냅샷을 찍어 이 패널에 직접 넘겨주는 이벤트 방식으로 바꿨다
/// (CardAcquiredPopup과 같은 타이밍에, 같은 정보 소스로 뜬다).
/// </summary>
public class CardDamagePreviewDebugUI : MonoBehaviour
{
    public static CardDamagePreviewDebugUI Instance { get; private set; }

    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI text;

    [Tooltip("텍스트가 화면에 유지되는 시간(초).")]
    [SerializeField] private float displayDuration = 3f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start() => SetVisible(false);

    /// <summary>
    /// 카드를 실제로 주운 직후 호출한다. beforeCards/beforeDamages는 Acquire() 호출 전,
    /// afterCards/afterDamages는 호출 후의 슬롯별 스냅샷 - 두 배열 다 같은 길이(슬롯 수)여야 한다.
    /// </summary>
    public static void ShowSnapshot(
        ItemData candidate,
        ItemData[] beforeCards, float[] beforeDamages,
        ItemData[] afterCards, float[] afterDamages)
    {
        if (candidate == null)
            return;

        // 씬에는 기본적으로 비활성 상태로 저장돼 있다(디자인 편집 편의를 위한 기본값). 비활성
        // 오브젝트는 Awake()가 안 돌아서 Instance가 비어 있을 수 있으니 처음 호출될 때 찾아서
        // 켜준다. 이후로는 SetVisible()이 배경/텍스트만 껐다 켰다 하고 루트는 계속 켜진 채로
        // 둔다 - 루트 자신을 다시 끄면 Update/코루틴이 다시 돌 기회조차 없어지기 때문
        // (CardAcquiredPopup은 외부에서 껐다 켰다 하니 무관하지만 여기는 그 패턴을 안 쓴다).
        if (Instance == null)
        {
            var found = FindObjectOfType<CardDamagePreviewDebugUI>(true);
            if (found == null) return;
            found.gameObject.SetActive(true);
        }

        Instance?.Display(candidate, beforeCards, beforeDamages, afterCards, afterDamages);
    }

    private void Display(
        ItemData candidate,
        ItemData[] beforeCards, float[] beforeDamages,
        ItemData[] afterCards, float[] afterDamages)
    {
        if (text != null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{candidate.itemName}] 획득 - 데미지 변화");

            int count = Mathf.Min(beforeCards.Length, afterCards.Length);
            for (int i = 0; i < count; i++)
            {
                string beforeName = beforeCards[i] != null ? beforeCards[i].itemName : "(빈 슬롯)";
                sb.AppendLine($"[{i}] {beforeName}  {beforeDamages[i]:F1} > {afterDamages[i]:F1}");
            }

            text.text = sb.ToString();
        }

        SetVisible(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        SetVisible(false);
        hideRoutine = null;
    }

    private void SetVisible(bool visible)
    {
        if (background != null) background.enabled = visible;
        if (text != null && text.gameObject.activeSelf != visible)
            text.gameObject.SetActive(visible);
    }
}
