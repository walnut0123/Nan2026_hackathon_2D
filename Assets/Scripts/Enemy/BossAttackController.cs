using System.Collections;
using UnityEngine;

// 보스 임시 캐릭터의 모든 공격 패턴을 한 스크립트에 모아 관리한다.
// 패턴마다 별도 스크립트(BossLineAttack/BossAoeAttack/BossMeteorAttack 등)로 나뉘어 있으면
// Inspector에서 여러 컴포넌트를 오가야 하는 불편함이 있어, 패턴별 설정을
// [System.Serializable] 중첩 클래스로 모듈화했다. 각 모듈이 자신의 enablePattern/쿨타임/시전
// 상태를 들고 있으므로, 예를 들어 일직선 공격만 끄고 싶으면 Line Pattern 모듈의
// Enable Pattern 체크박스 하나만 끄면 된다. 새 패턴을 추가할 때도 이 파일 안에 중첩 클래스 +
// CanStartX/CastRoutine 메서드만 추가하고 TryStartRandomPattern의 후보 목록에 끼워 넣으면 된다.
//
// 패턴 선택은 매 프레임 고정된 순서(Line→Aoe→Meteor→Square)로 시도하지 않는다 - 그러면 여러
// 패턴이 동시에 준비됐을 때 항상 먼저 검사되는 패턴이 이겨서 "항상 같은 순서로 나가는" 것처럼
// 보인다. 대신 쿨타임이 다 된 패턴들을 모아두고 그중 하나를 무작위로 골라 시전한다
// (TryStartRandomPattern). 또한 한 패턴이 끝난 직후 바로 다음 패턴이 나가지 않도록,
// 공유 쿨타임인 interAttackDelay(공격 텀)를 모든 패턴이 공통으로 거친다.
public class BossAttackController : MonoBehaviour
{
    [System.Serializable]
    public class LinePattern
    {
        [Tooltip("꺼두면 이 패턴은 발동하지 않는다. 일직선 패턴만 따로 테스트/디벨롭할 때 사용.")]
        public bool enablePattern = true;

        [Header("공통 설정")]
        [Tooltip("공격 1회 종료 후 다음 시전까지의 쿨타임(초)")]
        public float attackCooldown = 5f;
        [Tooltip("라인 위에 있을 때 즉발로 주는 데미지")]
        public int attackDamage = 1;

        [Header("시작점 설정")]
        [Tooltip("보스를 중심으로 이 반지름의 아주 작은 원 위에서 무작위 지점 2개(A, B)를 뽑는다. " +
            "이 원은 공격 범위가 아니라 라인이 시작되는 위치를 살짝 흩뿌리기 위한 값이다.")]
        public float originSpreadRadius = 1f;

        [Tooltip("보스/A/B 각 시작점에서 타겟(플레이어) 방향으로 라인이 뻗어나가는 거리")]
        public float attackRange = 6f;

        [Header("Indicator 설정")]
        [Tooltip("시전 시작부터 실제 피격 판정까지의 전체 예열 시간(초). 이 시간이 끝나는 순간 " +
            "라인 위에 있으면 즉발 데미지 - 라인 색상이 이 시간에 맞춰 연하게→진하게 변하며 " +
            "'언제 맞는지'를 알려주는 시계 역할을 한다.")]
        public float indicatorWarmupTime = 1f;
        [Tooltip("라인 길이가 0에서 사거리 끝까지 그려지는 연출 시간(초). 예열 시간과는 별개로, " +
            "빠르게(0.2~0.5초 권장) 한 번 그려지고 나면 예열이 끝날 때까지 그 길이로 유지된다.")]
        public float drawInDuration = 0.3f;
        [Tooltip("라인(및 피격 판정) 두께(월드 단위)")]
        public float indicatorWidth = 0.6f;
        [Tooltip("예열 시작 시 라인 색상(연하게)")]
        public Color indicatorStartColor = new Color(1f, 0.3f, 0.3f, 0.15f);
        [Tooltip("예열 완료(=피격 판정) 시점 라인 색상(진하게)")]
        public Color indicatorEndColor = new Color(1f, 0f, 0f, 0.75f);

        [Header("피격 순간 연출")]
        [Tooltip("데미지가 들어가는 순간 라인이 잠깐 번쩍이는 색상 - 타격감을 위한 짧은 플래시")]
        public Color hitFlashColor = new Color(1f, 1f, 1f, 0.95f);
        [Tooltip("플래시가 유지되는 시간(초). 이 시간이 끝나면 라인이 사라진다")]
        public float hitFlashDuration = 0.08f;

        [System.NonSerialized] public float cooldownRemaining;
        [System.NonSerialized] public bool isCasting;
    }

    [System.Serializable]
    public class AoePattern
    {
        [Tooltip("꺼두면 이 패턴은 발동하지 않는다. 장판 패턴만 따로 테스트/디벨롭할 때 사용.")]
        public bool enablePattern = true;

        [Header("공통 설정")]
        public float attackCooldown = 4f;
        [Tooltip("장판이 터질 때 범위 안의 대상에게 주는 데미지")]
        public int attackDamage = 3;

        [Header("범위/차징 설정")]
        [Tooltip("실제 피격 범위(큰 원)의 반지름")]
        public float bigRadius = 3f;
        [Tooltip("차징(작은 원)이 시작될 때의 반지름")]
        public float smallRadiusStart = 0.3f;
        [Tooltip("작은 원이 커져서 큰 원과 맞닿기까지(=실제 공격 발동까지) 걸리는 소요 시간(초)")]
        public float chargeDuration = 1.5f;
        [Tooltip("큰 원(실제 피격 범위)이 0에서 실제 크기까지 그려지는 연출 시간(초). chargeDuration과는 " +
            "별개이며, 다른 패턴의 draw-in보다 1.5배 느리게(천천히 생기도록) 잡았다.")]
        public float bigCircleDrawInDuration = 0.45f;

        [Header("Indicator 색상 - 일직선 패턴과 톤을 맞춤")]
        public Color bigCircleColor = new Color(1f, 0.1840f, 0.1840f, 0.55f);
        public Color smallCircleStartColor = new Color(1f, 0.3f, 0.3f, 0.15f);
        public Color smallCircleEndColor = new Color(1f, 0.1840f, 0.1840f, 0.75f);

        [Header("피격 순간 연출")]
        [Tooltip("데미지가 들어가는 순간 원이 잠깐 번쩍이는 색상 - 타격감을 위한 짧은 플래시")]
        public Color hitFlashColor = new Color(1f, 1f, 1f, 0.95f);
        [Tooltip("플래시가 유지되는 시간(초). 이 시간이 끝나면 원이 사라진다")]
        public float hitFlashDuration = 0.08f;

        [System.NonSerialized] public float cooldownRemaining;
        [System.NonSerialized] public bool isCasting;
    }

    [System.Serializable]
    public class MeteorPattern
    {
        [Tooltip("꺼두면 이 패턴은 발동하지 않는다. 낙하형 장판 패턴만 따로 테스트/디벨롭할 때 사용.")]
        public bool enablePattern = true;

        [Header("공통 설정")]
        public float attackCooldown = 4f;
        [Tooltip("장판이 터질 때 범위 안의 대상에게 주는 데미지")]
        public int attackDamage = 3;

        [Header("타겟팅 설정")]
        [Tooltip("시전 시점 타겟(씬에 PriorityTarget/허수아비가 있으면 그쪽 우선, 없으면 플레이어) " +
            "위치를 중심으로 이 반경 내에서 첫 번째 착탄 지점이 무작위로 정해진다")]
        public float targetRandomRadius = 3f;

        [Header("두 번째 낙하 설정 - 장판이 한 번에 2개씩 떨어진다")]
        [Tooltip("첫 번째 장판이 시작된 뒤, 두 번째 장판이 추가로 떨어지기 시작하기까지의 시간차(초)")]
        public float secondDropDelay = 0.8f;
        [Tooltip("두 번째 장판의 착탄 지점이 첫 번째 착탄 지점으로부터 벗어날 수 있는 최소 거리 " +
            "(월드 단위) - 두 장판이 너무 달라붙지 않도록 제한한다")]
        public float secondDropSpreadMin = 0.25f;
        [Tooltip("두 번째 장판의 착탄 지점이 첫 번째 착탄 지점으로부터 벗어날 수 있는 최대 거리 " +
            "(월드 단위) - 두 장판이 서로 너무 멀어지지 않도록 제한한다")]
        public float secondDropSpreadMax = 0.6f;

        [Header("범위/차징 설정")]
        public float bigRadius = 2f;
        public float smallRadiusStart = 0.3f;
        public float chargeDuration = 1.5f;
        [Tooltip("큰 원(실제 피격 범위)이 0에서 실제 크기까지 빠르게 그려지는 연출 시간(초). " +
            "chargeDuration과는 별개로, 짧게(0.2~0.5초 권장) 그려지고 나면 그 크기로 유지된다.")]
        public float bigCircleDrawInDuration = 0.3f;

        [Header("Indicator 색상 - 일직선 패턴과 톤을 맞춤")]
        public Color bigCircleColor = new Color(1f, 0.1840f, 0.1840f, 0.55f);
        public Color smallCircleStartColor = new Color(1f, 0.3f, 0.3f, 0.15f);
        public Color smallCircleEndColor = new Color(1f, 0.1840f, 0.1840f, 0.75f);

        [Header("피격 순간 연출")]
        [Tooltip("데미지가 들어가는 순간 원이 잠깐 번쩍이는 색상 - 타격감을 위한 짧은 플래시")]
        public Color hitFlashColor = new Color(1f, 1f, 1f, 0.95f);
        [Tooltip("플래시가 유지되는 시간(초). 이 시간이 끝나면 원이 사라진다")]
        public float hitFlashDuration = 0.08f;

        [System.NonSerialized] public float cooldownRemaining;
        [System.NonSerialized] public bool isCasting;
    }

    [System.Serializable]
    public class SquarePattern
    {
        [Tooltip("꺼두면 이 패턴은 발동하지 않는다.")]
        public bool enablePattern = true;

        [Header("공통 설정")]
        [Tooltip("공격 1회 종료 후 다음 시전까지의 쿨타임(초)")]
        public float attackCooldown = 6f;
        [Tooltip("투사체 하나가 명중했을 때 주는 데미지")]
        public int attackDamage = 1;

        [Header("대형 설정")]
        [Tooltip("다이아몬드 중심에서 꼭짓점까지의 거리(월드 단위). 대형 전체 크기를 결정한다.")]
        public float diamondRadius = 2f;
        [Tooltip("다이아몬드 한 변에 놓이는 투사체 개수(꼭짓점 포함, 총 개수 = 4 * orbsPerSide)")]
        public int orbsPerSide = 6;

        [Header("투사체(오브) 비주얼 - 속 원(core) + 테두리 원(ring) 2겹으로 표현")]
        [Tooltip("속 원(core)의 지름(월드 단위)")]
        public float orbSize = 0.44f;
        [Tooltip("속 원(core) 색상 - 진하고 선명한 빨강")]
        public Color orbColor = new Color(0.95f, 0.05f, 0.05f, 1f);
        [Tooltip("테두리 원(ring)의 지름(월드 단위). core보다 크게 잡아야 테두리처럼 보인다.")]
        public float ringSize = 0.62f;
        [Tooltip("테두리 원(ring) 색상 - core 뒤에 깔리는 은은한 붉은 광채")]
        public Color ringColor = new Color(1f, 0.25f, 0.15f, 0.45f);

        [Header("이동/회전 설정 (대형 전체 기준 - 개별 오브가 아니라 정사각형 모양 자체가 통째로 움직인다)")]
        [Tooltip("범위 표시(인디케이터) 없이 즉발로 나가는 대신, 대형 전체가 이 속도로 아주 느리게 " +
            "이동해서 플레이어가 보고 피할 시간을 준다")]
        public float moveSpeed = 1.2f;
        [Tooltip("다이아몬드 대형 전체가 자신의 중심(시전 위치)을 축으로 회전하는 속도(도/초). " +
            "개별 오브가 아니라 정사각형 모양 자체가 팽이처럼 통째로 돈다.")]
        public float rotationSpeed = 90f;
        [Tooltip("아무것도 맞추지 못했을 때 대형 전체가 자동으로 사라지기까지의 생존 시간(초)")]
        public float lifetime = 8f;

        [Header("이중 발사 설정 - 대형이 한 번에 2개씩 나간다")]
        [Tooltip("첫 번째 대형이 나간 뒤, 두 번째 대형이 추가로 나가기까지의 시간차(초) - " +
            "두 대형이 완전히 동시에 나가지 않도록 살짝 텀을 둔다")]
        public float secondFormationDelay = 0.4f;
        [Tooltip("두 번째 대형의 중심이 첫 번째 대형 중심으로부터 벗어날 수 있는 최대 거리(월드 단위) - " +
            "두 대형이 서로 너무 멀어지지 않도록 제한한다. 나머지 동작(이동/회전/수명 등)은 " +
            "두 대형 모두 동일하다.")]
        public float secondFormationSpread = 0.6f;

        [System.NonSerialized] public float cooldownRemaining;
        [System.NonSerialized] public bool isCasting;
    }

    [Header("패턴 1: 일직선 공격")]
    [SerializeField] private LinePattern linePattern = new LinePattern();

    [Header("패턴 2: 광역기 장판")]
    [SerializeField] private AoePattern aoePattern = new AoePattern();

    [Header("패턴 3: 낙하형 장판 공격")]
    [SerializeField] private MeteorPattern meteorPattern = new MeteorPattern();

    [Header("패턴 4: 정사각형(다이아몬드) 투사체 공격")]
    [SerializeField] private SquarePattern squarePattern = new SquarePattern();

    // 모든 패턴(일직선/낙하형/정사각형)이 공유하는 타겟 선택 방식. Auto는 기존 규칙(씬에
    // PriorityTarget=허수아비가 있으면 최우선, 없으면 플레이어) 그대로이고, Player/Scarecrow로
    // 강제하면 요청 없이도 Inspector에서 바로 테스트 대상을 바꿀 수 있다.
    private enum TargetMode
    {
        Auto,
        Player,
        Scarecrow,
    }

    [Header("타겟팅 (모든 패턴 공유)")]
    [Tooltip("Auto: PriorityTarget(허수아비)이 씬에 있으면 최우선, 없으면 플레이어.\n" +
        "Player: 항상 플레이어를 타겟으로 삼는다(허수아비가 있어도 무시).\n" +
        "Scarecrow: 항상 PriorityTarget(허수아비)을 타겟으로 삼는다(없으면 타겟 없음).")]
    [SerializeField] private TargetMode targetMode = TargetMode.Player;

    [Header("공격 텀 (모든 패턴 공유)")]
    [Tooltip("한 패턴이 끝난 직후 다음 패턴이 시작되기까지의 공통 대기 시간(초). 각 패턴 자체의 " +
        "쿨타임과는 별개로, 매 공격 사이에 항상 이만큼의 텀을 강제한다.")]
    [SerializeField] private float interAttackDelay = 1f;

    private Transform targetTransform;

    // 패턴들이 동시에 튀어나오지 않도록 하는 공유 잠금. 쿨타임과는 별개로
    // "한 번에 하나의 패턴만" 규칙을 강제한다.
    private bool isAnyPatternCasting;
    private float interAttackCooldownRemaining;

    private void Start()
    {
        FindTarget();
    }

    private void Update()
    {
        // targetMode를 Inspector에서 바꿨을 때 바로 반영되도록 매 프레임 다시 찾는다.
        FindTarget();

        // 쿨타임은 잠금 상태와 무관하게 항상 흘러가야 각 패턴이 독립적으로 준비된다 -
        // 예를 들어 A 패턴이 시전 중이어도 B/C/D의 쿨타임은 계속 감소한다.
        TickCooldown(linePattern);
        TickCooldown(aoePattern);
        TickCooldown(meteorPattern);
        TickCooldown(squarePattern);

        if (interAttackCooldownRemaining > 0f)
            interAttackCooldownRemaining -= Time.deltaTime;

        if (isAnyPatternCasting || interAttackCooldownRemaining > 0f)
            return;

        TryStartRandomPattern();
    }

    private void TickCooldown(LinePattern p)
    {
        if (p.cooldownRemaining > 0f) p.cooldownRemaining -= Time.deltaTime;
    }

    private void TickCooldown(AoePattern p)
    {
        if (p.cooldownRemaining > 0f) p.cooldownRemaining -= Time.deltaTime;
    }

    private void TickCooldown(MeteorPattern p)
    {
        if (p.cooldownRemaining > 0f) p.cooldownRemaining -= Time.deltaTime;
    }

    private void TickCooldown(SquarePattern p)
    {
        if (p.cooldownRemaining > 0f) p.cooldownRemaining -= Time.deltaTime;
    }

    // 쿨타임이 다 된 패턴들을 모아 그중 하나를 무작위로 골라 시전한다 - 항상 같은 순서로
    // 나가지 않고 매번 랜덤하게 패턴이 나가도록 하기 위함.
    private void TryStartRandomPattern()
    {
        var ready = new System.Collections.Generic.List<int>(4);
        if (CanStartLine()) ready.Add(0);
        if (CanStartAoe()) ready.Add(1);
        if (CanStartMeteor()) ready.Add(2);
        if (CanStartSquare()) ready.Add(3);

        if (ready.Count == 0)
            return;

        switch (ready[Random.Range(0, ready.Count)])
        {
            case 0: StartCoroutine(LineCastRoutine(linePattern)); break;
            case 1: StartCoroutine(AoeCastRoutine(aoePattern)); break;
            case 2: StartCoroutine(MeteorCastRoutine(meteorPattern)); break;
            case 3: StartCoroutine(SquareCastRoutine(squarePattern)); break;
        }
    }

    private bool CanStartLine() =>
        linePattern.enablePattern && !linePattern.isCasting && linePattern.cooldownRemaining <= 0f && targetTransform != null;

    private bool CanStartAoe() =>
        aoePattern.enablePattern && !aoePattern.isCasting && aoePattern.cooldownRemaining <= 0f;

    private bool CanStartMeteor() =>
        meteorPattern.enablePattern && !meteorPattern.isCasting && meteorPattern.cooldownRemaining <= 0f && targetTransform != null;

    private bool CanStartSquare() =>
        squarePattern.enablePattern && !squarePattern.isCasting && squarePattern.cooldownRemaining <= 0f && targetTransform != null;

    // 패턴 하나의 시전이 끝날 때 공통으로 호출한다 - 공유 잠금을 풀어주는 동시에, 다음 패턴이
    // 곧바로 이어서 나가지 않도록 공격 텀(interAttackDelay)을 시작시킨다.
    private void EndCast()
    {
        isAnyPatternCasting = false;
        interAttackCooldownRemaining = interAttackDelay;
    }

    // EnemyChaser/EnemyAttacker/RangedAttacker와 동일한 기본 규칙(Auto)에 targetMode 강제
    // 옵션을 얹었다. 일직선/낙하형/정사각형 패턴 모두 이 targetTransform 하나를 공유한다.
    private void FindTarget()
    {
        if (targetMode == TargetMode.Player)
        {
            var playerInventory = FindObjectOfType<PlayerInventory>();
            targetTransform = playerInventory != null ? playerInventory.transform : null;
            return;
        }

        if (targetMode == TargetMode.Scarecrow)
        {
            var scarecrow = FindObjectOfType<PriorityTarget>();
            targetTransform = scarecrow != null ? scarecrow.transform : null;
            return;
        }

        var priorityTarget = FindObjectOfType<PriorityTarget>();
        if (priorityTarget != null)
        {
            targetTransform = priorityTarget.transform;
            return;
        }

        var fallbackPlayer = FindObjectOfType<PlayerInventory>();
        if (fallbackPlayer != null)
            targetTransform = fallbackPlayer.transform;
    }

    private Vector2 DirectionToTarget()
    {
        Vector2 direction = (Vector2)(targetTransform.position - transform.position);
        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;
        return direction.normalized;
    }

    // ===== 패턴 1: 일직선 공격 =====

    private IEnumerator LineCastRoutine(LinePattern p)
    {
        p.isCasting = true;
        isAnyPatternCasting = true;

        // 보스를 중심으로 한 아주 작은 원 위에서 시작점 2개(A, B)를 무작위로 뽑는다 - 이 원은
        // 공격 범위가 아니라 라인이 시작되는 위치만 살짝 흩뿌리는 용도다. 보스 자신 + A + B,
        // 총 3개의 시작점에서 각각 기존의 "타겟 방향으로 사거리만큼 뻗는" 일직선 공격을 그대로 쏜다.
        Vector3[] origins =
        {
            transform.position,
            transform.position + (Vector3)RandomPointOnCircle(p.originSpreadRadius),
            transform.position + (Vector3)RandomPointOnCircle(p.originSpreadRadius),
        };

        var endPoints = new Vector3[origins.Length];
        var indicators = new GameObject[origins.Length];
        var indicatorRenderers = new SpriteRenderer[origins.Length];

        // 방향은 시전 시작 시점에 고정한다 - 인디케이터가 보여준 그대로 공격이 나가야
        // 플레이어가 인디케이터를 보고 피할 수 있는 "텔레그래프"로서 의미가 있다.
        for (int i = 0; i < origins.Length; i++)
        {
            Vector2 direction = (Vector2)(targetTransform.position - origins[i]);
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.right;
            direction.Normalize();

            endPoints[i] = origins[i] + (Vector3)(direction * p.attackRange);
            indicators[i] = CreateLineIndicator(origins[i], direction, p.indicatorWidth, p.indicatorStartColor);
            indicatorRenderers[i] = indicators[i].GetComponent<SpriteRenderer>();
        }

        float elapsed = 0f;
        while (elapsed < p.indicatorWarmupTime)
        {
            elapsed += Time.deltaTime;

            // 길이는 drawInDuration 동안만 빠르게 0→사거리로 그려지고 그 이후엔 고정된다 -
            // 예열 시간 전체에 걸쳐 늘어나는 게 아니라 짧은 "그려지는 연출"일 뿐이다.
            float drawT = p.drawInDuration > 0f ? Mathf.Clamp01(elapsed / p.drawInDuration) : 1f;

            // 색상은 예열 시간 전체에 걸쳐 연하게→진하게 변하며 "언제 맞는지"를 알려주는 역할.
            float colorT = p.indicatorWarmupTime > 0f ? Mathf.Clamp01(elapsed / p.indicatorWarmupTime) : 1f;

            for (int i = 0; i < origins.Length; i++)
            {
                indicators[i].transform.localScale = new Vector3(p.attackRange * drawT, p.indicatorWidth, 1f);
                indicatorRenderers[i].color = Color.Lerp(p.indicatorStartColor, p.indicatorEndColor, colorT);
            }

            yield return null;
        }

        // 투사체 없이, 예열이 끝나 라인이 다 뻗은 순간 라인 위에 있는 대상에게만 즉발로 데미지를 준다.
        // 사라지기 전에 잠깐 하얗게 번쩍여서 "맞았다"는 타격감을 준다.
        for (int i = 0; i < origins.Length; i++)
        {
            indicatorRenderers[i].color = p.hitFlashColor;
            DealLineDamage(origins[i], endPoints[i], p.indicatorWidth, p.attackDamage);
        }

        yield return new WaitForSeconds(p.hitFlashDuration);

        for (int i = 0; i < origins.Length; i++)
            Destroy(indicators[i]);

        p.cooldownRemaining = p.attackCooldown;
        p.isCasting = false;
        EndCast();
    }

    private Vector2 RandomPointOnCircle(float radius)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    // minDist~maxDist 범위의 거리로, 무작위 방향의 오프셋을 만든다(도넛 모양 범위) - 두 지점이
    // 너무 가깝지도(달라붙지도) 너무 멀지도 않도록 동시에 제한할 때 사용.
    private Vector2 RandomOffsetInAnnulus(float minDist, float maxDist)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = Random.Range(minDist, Mathf.Max(minDist, maxDist));
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    }

    // origin에서 direction 방향으로 뻗어나갈 직사각형 라인 인디케이터를 길이 0으로 생성한다.
    // 실제 길이는 예열 애니메이션 도중 localScale.x를 갱신해서 서서히 늘린다.
    private GameObject CreateLineIndicator(Vector3 origin, Vector2 direction, float width, Color color)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        var indicator = new GameObject("LineAttackIndicator");
        indicator.transform.position = origin;
        indicator.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        indicator.transform.localScale = new Vector3(0f, width, 1f);

        var renderer = indicator.AddComponent<SpriteRenderer>();
        renderer.sprite = BossIndicatorUtil.GetRectangleSprite();
        renderer.color = color;
        renderer.sortingOrder = 2;

        return indicator;
    }

    // a-b 라인 위(두께 width의 직사각형 판정)에 있는 대상에게 즉발 데미지를 준다.
    private void DealLineDamage(Vector3 a, Vector3 b, float width, int damage)
    {
        Vector2 diff = b - a;
        float length = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        Vector2 center = (Vector2)a + diff * 0.5f;

        var hits = Physics2D.OverlapBoxAll(center, new Vector2(length, width), angle);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
                continue;

            var health = DamageUtil.ResolveHealth(hit);
            if (health != null)
                health.TakeDamage(damage);
        }
    }

    // ===== 패턴 2: 광역기 장판 =====

    private IEnumerator AoeCastRoutine(AoePattern p)
    {
        p.isCasting = true;
        isAnyPatternCasting = true;

        Vector3 center = transform.position;
        yield return ChargeAndExplode(center, p.bigRadius, p.smallRadiusStart, p.chargeDuration,
            p.bigCircleDrawInDuration, p.bigCircleColor, p.smallCircleStartColor, p.smallCircleEndColor,
            p.hitFlashColor, p.hitFlashDuration, p.attackDamage);

        p.cooldownRemaining = p.attackCooldown;
        p.isCasting = false;
        EndCast();
    }

    // ===== 패턴 3: 낙하형 장판 공격 =====

    // 장판 하나가 아니라 두 개를 떨어뜨린다 - 첫 번째는 기존 로직 그대로(타겟 주변
    // targetRandomRadius 내 무작위 지점), 두 번째는 첫 번째로부터 secondDropSpreadMin~Max 사이의
    // 거리(너무 붙지도 너무 멀지도 않게)에 secondDropDelay만큼 늦게 떨어진다.
    private IEnumerator MeteorCastRoutine(MeteorPattern p)
    {
        p.isCasting = true;
        isAnyPatternCasting = true;

        // 착탄 지점은 시전 시작 시점에 한 번만 정해서 고정한다 - 인디케이터가 보여준 자리 그대로
        // 공격이 떨어져야 플레이어가 보고 피할 수 있는 텔레그래프로서 의미가 있다.
        Vector2 randomOffset = Random.insideUnitCircle * p.targetRandomRadius;
        Vector3 firstCenter = targetTransform.position + (Vector3)randomOffset;
        Vector3 secondCenter = firstCenter + (Vector3)RandomOffsetInAnnulus(p.secondDropSpreadMin, p.secondDropSpreadMax);

        StartCoroutine(ChargeAndExplode(firstCenter, p.bigRadius, p.smallRadiusStart, p.chargeDuration,
            p.bigCircleDrawInDuration, p.bigCircleColor, p.smallCircleStartColor, p.smallCircleEndColor,
            p.hitFlashColor, p.hitFlashDuration, p.attackDamage));

        yield return new WaitForSeconds(p.secondDropDelay);

        yield return ChargeAndExplode(secondCenter, p.bigRadius, p.smallRadiusStart, p.chargeDuration,
            p.bigCircleDrawInDuration, p.bigCircleColor, p.smallCircleStartColor, p.smallCircleEndColor,
            p.hitFlashColor, p.hitFlashDuration, p.attackDamage);

        p.cooldownRemaining = p.attackCooldown;
        p.isCasting = false;
        EndCast();
    }

    // AoePattern/MeteorPattern이 공유하는 "작은 원이 큰 원까지 커지며 진해지다가, 맞닿는 순간
    // 큰 원 범위 전체에 데미지" 연출.
    private IEnumerator ChargeAndExplode(Vector3 center, float bigRadius, float smallRadiusStart,
        float chargeDuration, float bigCircleDrawInDuration, Color bigCircleColor, Color smallCircleStartColor,
        Color smallCircleEndColor, Color hitFlashColor, float hitFlashDuration, int damage)
    {
        // 큰 원(실제 피격 범위)은 크기 0으로 시작해서 bigCircleDrawInDuration 동안만 빠르게
        // 그려지고, 그 이후엔 chargeDuration이 끝날 때까지 그 크기로 고정된다.
        GameObject bigCircle = CreateCircleIndicator("AttackBigCircle", center, 0f, bigCircleColor, sortingOrder: 1);
        GameObject smallCircle = CreateCircleIndicator("AttackSmallCircle", center, smallRadiusStart * 2f, smallCircleStartColor, sortingOrder: 2);
        var bigRenderer = bigCircle.GetComponent<SpriteRenderer>();
        var smallRenderer = smallCircle.GetComponent<SpriteRenderer>();

        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            float t = chargeDuration > 0f ? Mathf.Clamp01(elapsed / chargeDuration) : 1f;

            float drawT = bigCircleDrawInDuration > 0f ? Mathf.Clamp01(elapsed / bigCircleDrawInDuration) : 1f;
            bigCircle.transform.localScale = Vector3.one * (bigRadius * 2f * drawT);

            float currentRadius = Mathf.Lerp(smallRadiusStart, bigRadius, t);
            smallCircle.transform.localScale = Vector3.one * (currentRadius * 2f);
            smallRenderer.color = Color.Lerp(smallCircleStartColor, smallCircleEndColor, t);

            yield return null;
        }

        // 데미지가 들어가는 순간 잠깐 하얗게 번쩍인 뒤 사라져서 타격감을 준다.
        bigRenderer.color = hitFlashColor;
        smallRenderer.color = hitFlashColor;
        DealCircleDamage(center, bigRadius, damage);

        yield return new WaitForSeconds(hitFlashDuration);

        Destroy(bigCircle);
        Destroy(smallCircle);
    }

    private GameObject CreateCircleIndicator(string name, Vector3 position, float diameter, Color color, int sortingOrder)
    {
        var indicator = new GameObject(name);
        indicator.transform.position = position;
        indicator.transform.localScale = Vector3.one * diameter;

        var renderer = indicator.AddComponent<SpriteRenderer>();
        renderer.sprite = BossIndicatorUtil.GetFilledCircleSprite();
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        return indicator;
    }

    private void DealCircleDamage(Vector3 center, float radius, int damage)
    {
        var hits = Physics2D.OverlapCircleAll(center, radius);
        foreach (var hit in hits)
        {
            // 다른 적(자기 자신 포함, 둘 다 tag=Enemy)에게는 데미지를 주지 않는다.
            if (hit.CompareTag("Enemy"))
                continue;

            var health = DamageUtil.ResolveHealth(hit);
            if (health != null)
                health.TakeDamage(damage);
        }
    }

    // ===== 패턴 4: 정사각형(다이아몬드) 투사체 공격 =====

    // 개별 대형은 예열 인디케이터 없이 즉발로 나간다 - 대신 대형 전체가 매우 느리게 이동해서
    // 플레이어에게 피할 시간을 준다. 대형을 한 번에 2개 만들되, 완전히 동시에 나가면 겹쳐
    // 보이므로 secondFormationDelay만큼 시간차를 둔다. 두 번째는 첫 번째로부터
    // secondFormationSpread 이내의 가까운 위치. 나머지(이동/회전/비주얼 등)는 두 대형 모두
    // 완전히 동일하다.
    private IEnumerator SquareCastRoutine(SquarePattern p)
    {
        p.isCasting = true;
        isAnyPatternCasting = true;

        Vector2 direction = DirectionToTarget();
        Vector3 firstCenter = transform.position;
        Vector3 secondCenter = firstCenter + (Vector3)(Random.insideUnitCircle * p.secondFormationSpread);

        SpawnSquareFormation(firstCenter, direction, p);

        yield return new WaitForSeconds(p.secondFormationDelay);

        SpawnSquareFormation(secondCenter, direction, p);

        p.cooldownRemaining = p.attackCooldown;
        p.isCasting = false;
        EndCast();
    }

    // 다이아몬드 대형 하나(부모 BossOrbFormation + 자식 오브들)를 center를 중심으로 생성한다.
    private void SpawnSquareFormation(Vector3 center, Vector2 direction, SquarePattern p)
    {
        int sides = Mathf.Max(1, p.orbsPerSide);

        // 대형 전체(개별 오브가 아니라 "정사각형 모양 자체")를 하나의 부모로 묶어서, 이동/회전을
        // 부모 하나에서만 처리한다 - 자식 오브들은 로컬 위치를 고정한 채 부모를 따라간다.
        var formationObject = new GameObject("BossSquareFormation");
        formationObject.transform.position = center;

        var formationRb = formationObject.AddComponent<Rigidbody2D>();
        formationRb.bodyType = RigidbodyType2D.Kinematic;
        formationRb.gravityScale = 0f;

        var formation = formationObject.AddComponent<BossOrbFormation>();
        formation.Initialize(direction, p.moveSpeed, p.rotationSpeed, p.lifetime);

        // 위/오른쪽/아래/왼쪽 꼭짓점을 잇는 순서로 다이아몬드(45도 회전한 정사각형) 외곽선을 만든다.
        Vector2[] vertices =
        {
            new Vector2(0f, p.diamondRadius),
            new Vector2(p.diamondRadius, 0f),
            new Vector2(0f, -p.diamondRadius),
            new Vector2(-p.diamondRadius, 0f),
        };

        for (int edge = 0; edge < 4; edge++)
        {
            Vector2 start = vertices[edge];
            Vector2 end = vertices[(edge + 1) % 4];

            for (int i = 0; i < sides; i++)
            {
                float t = (float)i / sides;
                Vector2 localOffset = Vector2.Lerp(start, end, t);
                SpawnSquareOrb(formationObject.transform, localOffset, p);
            }
        }
    }

    // 대형(BossSquareFormation)에 속하는 오브 하나를 생성한다 - 테두리 원(ring)과 속 원(core)
    // 2겹의 비주얼 + 피격 판정(BossOrbHit)만 담당하고, 이동/회전은 하지 않는다(부모가 담당).
    private void SpawnSquareOrb(Transform parent, Vector2 localOffset, SquarePattern p)
    {
        var orbObject = new GameObject("BossOrb");
        orbObject.transform.SetParent(parent, false);
        orbObject.transform.localPosition = localOffset;

        var ring = new GameObject("Ring");
        ring.transform.SetParent(orbObject.transform, false);
        ring.transform.localScale = Vector3.one * p.ringSize;
        var ringRenderer = ring.AddComponent<SpriteRenderer>();
        ringRenderer.sprite = BossIndicatorUtil.GetFilledCircleSprite();
        ringRenderer.color = p.ringColor;
        ringRenderer.sortingOrder = 3;

        var core = new GameObject("Core");
        core.transform.SetParent(orbObject.transform, false);
        core.transform.localScale = Vector3.one * p.orbSize;
        var coreRenderer = core.AddComponent<SpriteRenderer>();
        coreRenderer.sprite = BossIndicatorUtil.GetFilledCircleSprite();
        coreRenderer.color = p.orbColor;
        coreRenderer.sortingOrder = 4;

        var collider = orbObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = p.orbSize * 0.5f;

        var hit = orbObject.AddComponent<BossOrbHit>();
        hit.Initialize(p.attackDamage);
    }

    // 각 패턴의 범위/사거리를 Inspector에서 조절할 때 Scene 뷰에서 바로 확인할 수 있도록.
    private void OnDrawGizmosSelected()
    {
        // 패턴 1: 시작점 2개(A, B)를 뽑는 아주 작은 원 + 각 라인이 뻗어나가는 사거리 참고용 원
        Gizmos.color = new Color(1f, 0f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, linePattern.originSpreadRadius);
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, linePattern.attackRange);

        // 패턴 2: 광역기 장판(보스 위치)
        Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, aoePattern.bigRadius);
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, aoePattern.smallRadiusStart);

        // 패턴 3: 낙하형 장판의 착탄 후보 범위(Play 모드 & 타겟이 있을 때만 의미 있음)
        if (Application.isPlaying && targetTransform != null)
        {
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.5f);
            Gizmos.DrawWireSphere(targetTransform.position, meteorPattern.targetRandomRadius);
        }

        // 패턴 4: 다이아몬드 대형 외곽선
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Vector3 top = transform.position + new Vector3(0f, squarePattern.diamondRadius, 0f);
        Vector3 right = transform.position + new Vector3(squarePattern.diamondRadius, 0f, 0f);
        Vector3 bottom = transform.position + new Vector3(0f, -squarePattern.diamondRadius, 0f);
        Vector3 left = transform.position + new Vector3(-squarePattern.diamondRadius, 0f, 0f);
        Gizmos.DrawLine(top, right);
        Gizmos.DrawLine(right, bottom);
        Gizmos.DrawLine(bottom, left);
        Gizmos.DrawLine(left, top);
    }
}
