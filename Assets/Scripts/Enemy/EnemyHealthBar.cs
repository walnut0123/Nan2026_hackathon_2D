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

    private Health health;
    private Transform barRoot;
    private Transform fillAnchor;
    private SpriteRenderer fillRenderer;

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

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillAnchor, false);
        fillRenderer = fillGO.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = barSprite;
        fillRenderer.color = fillColor;
        fillRenderer.sortingOrder = 21;
    }

    private void HandleDamaged(int amount)
    {
        UpdateFill();
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
