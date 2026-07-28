using System.Collections;
using UnityEngine;

// Health의 OnDamaged/OnDeath 이벤트를 구독해서 적 머리 위에 체력바를 표시한다.
// 배경/체력 게이지 SpriteRenderer를 코드에서 직접 생성해 붙이므로, 프리팹에는 이 컴포넌트
// 하나만 추가하면 된다. 부모(적) 오브젝트의 스케일과 무관하게 항상 barWidth/barHeight에서
// 지정한 월드 단위 크기로 보이도록, 부모의 lossyScale 역수를 곱해 상쇄한다.
[RequireComponent(typeof(Health))]
public class EnemyHealthBar : MonoBehaviour
{
    [Header("크기/위치 (월드 단위)")]
    [SerializeField] private float barWidth = 0.6f;
    [SerializeField] private float barHeight = 0.08f;
    [SerializeField] private float gapAboveSprite = 0.12f;

    [Header("색상")]
    [SerializeField] private Color backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);
    [SerializeField] private Color fillColor = new Color(0.2f, 0.85f, 0.25f, 1f);

    [Tooltip("체력바(배경/게이지)에 쓸 스프라이트. Inspector에서 반드시 연결 (예: Assets/Sprites/UI/WhitePixel.png)")]
    [SerializeField] private Sprite barSprite;

    [Header("피격 연출 (번호로 구분 - 특정 기능만 나중에 빼기 쉽게)")]
    [Tooltip("기능 1: 피격 시 체력바 전체가 순간 커졌다가 원래 크기로 돌아오는 펀치 스케일 효과")]
    [SerializeField] private bool enablePunchScale = true;
    [SerializeField] private float punchScaleMultiplier = 1.3f;
    [SerializeField] private float punchScaleDuration = 0.15f;
    [Tooltip("연속 히트(예: 카드 버스트) 중 너무 자주 재발동해서 정신없어 보이는 것을 막기 위한 최소 " +
        "재발동 간격(초). 이 시간 안에 또 맞아도 펀치는 재시작하지 않는다.")]
    [SerializeField] private float punchCooldown = 1f;

    [Tooltip("기능 2: 피격 시 방금 잃은 체력만큼 흰색 잔여 체력(트레일)이 남아있다가, 잠시 후 " +
        "서서히 줄어들며 사라지는 효과. 초록 게이지는 항상 즉시 갱신되고, 흰색 트레일이 그 뒤를 " +
        "따라가듯 지연되어 줄어들면서 '방금 이만큼 잃었다'를 눈으로 보여준다.")]
    [SerializeField] private bool enableWhiteTrail = true;
    [Tooltip("흰색 잔여 체력이 줄어들기 시작하기까지의 대기 시간(초). 연속 히트가 들어오는 동안은 " +
        "히트마다 이 대기가 다시 시작되어, 버스트가 끝날 때까지 흰색이 계속 쌓인 채로 유지된다.")]
    [SerializeField] private float whiteTrailHoldDelay = 0.4f;
    [Tooltip("대기가 끝난 뒤 흰색 잔여 체력이 실제 체력 위치까지 줄어드는 데 걸리는 시간(초)")]
    [SerializeField] private float whiteTrailDrainDuration = 0.25f;
    [Tooltip("히트가 whiteTrailHoldDelay보다 촘촘하게 계속 들어오면(여러 적에게 동시에 맞는 등) " +
        "대기가 끝없이 밀려서 흰색이 영원히 안 사라지는 것을 막는 상한(초) - 이 그룹의 첫 히트로부터 " +
        "이 시간이 지나면, 더 맞고 있어도 강제로 드레인을 시작한다.")]
    [SerializeField] private float whiteTrailMaxHoldWindow = 1.5f;

    private Health health;
    private Transform barRoot;
    private Vector3 barRestScale;
    private Transform fillAnchor;
    private SpriteRenderer fillRenderer;
    private SpriteRenderer whiteTrailRenderer;
    private Coroutine punchScaleRoutine;
    private Coroutine whiteTrailRoutine;
    private float lastPunchTime = float.NegativeInfinity;
    private float lastWhiteTrailHitTime;
    private float firstWhiteTrailHitTimeInGroup = float.NegativeInfinity;

    private void Awake()
    {
        health = GetComponent<Health>();
        BuildBar();
    }

    private void Start()
    {
        // Health.Awake()가 currentHealth를 초기화하는 시점과 이 컴포넌트의 Awake() 실행 순서가
        // 보장되지 않아서(같은 오브젝트 내 컴포넌트 간 Awake 순서는 정해져 있지 않음), 여기서 다시
        // 채워준다 - 모든 오브젝트의 Awake()가 끝난 뒤에만 실행되는 Start()는 항상 안전하다.
        UpdateFill();
        SetWhiteTrailWidth(fillRenderer.transform.localScale.x);
    }

    private void OnEnable()
    {
        health.OnDamaged += HandleDamaged;
        health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        health.OnDamaged -= HandleDamaged;
        health.OnDeath -= HandleDeath;
    }

    private void BuildBar()
    {
        if (barSprite == null)
            Debug.LogWarning("[EnemyHealthBar] barSprite가 지정되지 않았습니다. Inspector에서 연결해주세요.");

        Vector3 parentScale = transform.lossyScale;
        float invX = parentScale.x != 0f ? 1f / parentScale.x : 1f;
        float invY = parentScale.y != 0f ? 1f / parentScale.y : 1f;

        var rootGO = new GameObject("HealthBar");
        barRoot = rootGO.transform;
        barRoot.SetParent(transform, false);
        barRoot.localScale = new Vector3(invX, invY, 1f);
        barRestScale = barRoot.localScale;

        var spriteRenderer = GetComponent<SpriteRenderer>();
        float spriteTopWorldY = spriteRenderer != null ? spriteRenderer.bounds.extents.y : 0f;
        barRoot.localPosition = new Vector3(0f, (spriteTopWorldY + gapAboveSprite) * invY, 0f);

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(barRoot, false);
        var bgRenderer = bgGO.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = barSprite;
        bgRenderer.color = backgroundColor;
        bgRenderer.sortingOrder = 20;
        bgGO.transform.localScale = new Vector3(barWidth, barHeight, 1f);

        var anchorGO = new GameObject("FillAnchor");
        fillAnchor = anchorGO.transform;
        fillAnchor.SetParent(barRoot, false);
        fillAnchor.localPosition = new Vector3(-barWidth / 2f, 0f, 0f);

        // 기능 2: Fill보다 먼저(아래) 그려지는 흰색 잔여 체력(트레일) 바. Fill과 같은 앵커/좌표계를
        // 공유해서 폭 계산 방식이 완전히 동일하다 - Fill이 그 위를 덮어서, 트레일 폭이 Fill 폭보다
        // 큰 구간(=최근에 잃은 체력)만 흰색으로 보인다.
        var whiteTrailGO = new GameObject("WhiteTrail");
        whiteTrailGO.transform.SetParent(fillAnchor, false);
        whiteTrailRenderer = whiteTrailGO.AddComponent<SpriteRenderer>();
        whiteTrailRenderer.sprite = barSprite;
        whiteTrailRenderer.color = Color.white;
        whiteTrailRenderer.sortingOrder = 21;

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAnchor, false);
        fillRenderer = fillGO.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = barSprite;
        fillRenderer.color = fillColor;
        fillRenderer.sortingOrder = 22;
    }

    private void HandleDamaged(float amount)
    {
        float oldFillWidth = fillRenderer.transform.localScale.x;

        // 초록 게이지는 항상 즉시(지연 없이) 실제 체력을 반영한다.
        UpdateFill();

        // 기능 1: 펀치 스케일 - 쿨다운 안에 또 맞으면 재발동하지 않고 넘어간다(연속 히트 시 계속
        // 들썩여서 시선을 뺏는 것 방지).
        if (enablePunchScale && Time.time - lastPunchTime >= punchCooldown)
        {
            lastPunchTime = Time.time;
            if (punchScaleRoutine != null)
                StopCoroutine(punchScaleRoutine);
            punchScaleRoutine = StartCoroutine(PunchScaleEffect());
        }

        // 기능 2: 흰색 잔여 체력 트레일 - 방금 잃은 만큼(oldFillWidth까지)은 흰색으로 남겨둔다.
        // 이미 그보다 더 넓게 남아있으면(연속 히트 도중이라 아직 못 줄어든 경우) 그대로 유지한다.
        if (enableWhiteTrail)
        {
            float currentWhiteWidth = whiteTrailRenderer.transform.localScale.x;
            SetWhiteTrailWidth(Mathf.Max(currentWhiteWidth, oldFillWidth));

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

    // 기능 1: 펀치 스케일 - barRoot를 punchScaleMultiplier배로 순간 키웠다가 punchScaleDuration에
    // 걸쳐 원래 크기(barRestScale)로 되돌린다.
    private IEnumerator PunchScaleEffect()
    {
        Vector3 punchedScale = barRestScale * punchScaleMultiplier;
        float elapsed = 0f;

        while (elapsed < punchScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / punchScaleDuration);
            barRoot.localScale = Vector3.Lerp(punchedScale, barRestScale, t);
            yield return null;
        }

        barRoot.localScale = barRestScale;
        punchScaleRoutine = null;
    }

    // 기능 2: 마지막 히트로부터 whiteTrailHoldDelay만큼 조용해지면(또는 이 그룹의 첫 히트로부터
    // whiteTrailMaxHoldWindow가 지나면 강제로), 흰색 잔여 체력을 그 시점의 실제 체력(Fill) 폭까지
    // whiteTrailDrainDuration에 걸쳐 서서히 줄인다. HandleDamaged가 코루틴을 재시작하지 않고
    // lastWhiteTrailHitTime만 갱신하므로, 이 while 루프가 알아서 대기를 연장한다 - 단, 상한을
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

        float startWidth = whiteTrailRenderer.transform.localScale.x;
        float targetWidth = fillRenderer.transform.localScale.x;
        float elapsed = 0f;

        while (elapsed < whiteTrailDrainDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / whiteTrailDrainDuration);
            SetWhiteTrailWidth(Mathf.Lerp(startWidth, targetWidth, t));
            yield return null;
        }

        SetWhiteTrailWidth(targetWidth);
        whiteTrailRoutine = null;
    }

    private void SetWhiteTrailWidth(float width)
    {
        whiteTrailRenderer.transform.localScale = new Vector3(width, barHeight * 0.7f, 1f);
        whiteTrailRenderer.transform.localPosition = new Vector3(width / 2f, 0f, 0f);
    }

    private void HandleDeath()
    {
        if (barRoot != null)
            barRoot.gameObject.SetActive(false);
    }

    private void UpdateFill()
    {
        if (fillRenderer == null || health.MaxHealth <= 0)
            return;

        float pct = Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth);
        float width = barWidth * pct;

        fillRenderer.transform.localScale = new Vector3(width, barHeight * 0.7f, 1f);
        fillRenderer.transform.localPosition = new Vector3(width / 2f, 0f, 0f);
    }
}
