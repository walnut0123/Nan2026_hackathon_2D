using System.Collections;
using UnityEngine;

// 몬스터 프리팹 루트에 부착하는 스폰 연출 오케스트레이터. RoomMonsterSpawner.SpawnAll()이
// Instantiate 직후 곧바로 enemy.SetActive(true)를 호출하는 기존 흐름을 그대로 이용한다 -
// 이 컴포넌트는 OnEnable에서 스스로 시퀀스를 시작하므로, 스폰 코드 쪽은 전혀 수정할 필요가 없다.
//
// 1단계(예고) -> 2단계(플래시) -> 3단계(등장) 순서로 재생하는 동안 이동/공격/피격 판정을 모두
// 비활성 상태로 유지한다. 피격 판정은 이 프로젝트에서 전부 Collider2D를 통한 물리 감지로만
// 이루어지므로(카드 자동공격의 OverlapCircleNonAlloc, 투사체의 OnTriggerEnter2D 등)
// Collider2D 하나만 꺼두면 Health를 별도로 건드리지 않아도 완전한 무적이 된다.
[DisallowMultipleComponent]
public class MonsterSpawnSequencer : MonoBehaviour
{
    [Header("연출 프리팹")]
    [Tooltip("1단계 락온 예고 프리팹 (SpawnLockOnTelegraph 포함) - 플레이어 수동 타겟팅 VFX(TargetLockVFX)와 " +
        "동일한 락온 브라켓 이미지를 재사용한다")]
    [SerializeField] private GameObject lockOnTelegraphPrefab;
    [Tooltip("2단계 화이트 플래시 프리팹 (SpawnFlashBurst 포함) - 3단계 임팩트 링에도 재사용된다")]
    [SerializeField] private GameObject flashBurstPrefab;

    [Header("1단계 - 예고 (락온 브라켓)")]
    [Tooltip("브라켓이 좁혀지는(잠기는) 데 걸리는 시간(초)")]
    [SerializeField] private float telegraphDuration = 0.45f;
    [Tooltip("시작 시점 브라켓 배율 - 크게 벌어진 상태에서 시작한다")]
    [SerializeField] private float telegraphStartScale = 1.1f;
    [Tooltip("완전히 좁혀졌을 때(잠긴 상태) 브라켓 배율")]
    [SerializeField] private float telegraphEndScale = 1f;
    [Tooltip("완전히 잠긴 뒤, 2단계로 넘어가기 전 잠깐 멈춰있는 시간(초) - '탁 잠기고 한 박자 쉬었다가 터진다'는 리듬을 만든다")]
    [SerializeField] private float telegraphHoldDuration = 0.1f;
    [SerializeField] private Color telegraphColor = new Color(1f, 0.35f, 0.1f, 1f);
    [Tooltip("좁혀지는 동안 함께 회전하는 각도(도) - 기계적으로 '조준해 들어간다'는 느낌을 더한다")]
    [SerializeField] private float telegraphRotationDegrees = 60f;

    [Header("2단계 - 플래시 (Flash)")]
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float flashStartScale = 0.3f;
    [SerializeField] private float flashEndScale = 1.2f;

    [Header("3단계 - 등장 (Emerge)")]
    [SerializeField] private float emergeDuration = 0.15f;
    [SerializeField] private float emergeScaleUndershoot = 0.6f;
    [SerializeField] private float emergeScaleOvershoot = 1.25f;
    [Tooltip("등장 시작 직후 순백색을 유지하는 비율(0~1) - 이 구간이 지나야 원래 색으로 서서히 돌아가기 " +
        "시작한다. 색이 곧바로 빠지지 않고 잠깐 '번쩍'인 채로 유지되어 등장 임팩트가 더 강조된다")]
    [SerializeField, Range(0f, 1f)] private float emergeWhiteHoldFraction = 0.15f;

    [Header("3단계 - 등장 임팩트 링")]
    [Tooltip("등장 순간 스프라이트가 켜짐과 동시에 지면에서 확 퍼져나가는 충격파 링 - flashBurstPrefab을 " +
        "재사용하되 스프라이트만 링 모양으로 바꿔서 표현한다(새 프리팹 불필요)")]
    [SerializeField] private bool enableEmergeImpactRing = true;
    [SerializeField] private float emergeImpactRingDuration = 0.25f;
    [SerializeField] private float emergeImpactRingStartScale = 0.4f;
    [SerializeField] private float emergeImpactRingEndScale = 2.2f;
    [SerializeField] private Color emergeImpactRingColor = new Color(1f, 0.95f, 0.8f, 0.9f);

    [Header("등장 후")]
    [Tooltip("등장 연출이 끝난 뒤 이동/공격/피격 판정을 추가로 더 묶어두는 시간(초) - 스폰킬 방지용 무적 시간")]
    [SerializeField] private float postSpawnInvincibleDuration = 0.2f;

    // 매 프레임 GetComponent를 피하기 위해 Awake에서 전부 캐싱한다. mover/attacker는 프리팹마다
    // 붙어있는 구현체가 달라서(EnemyAIPathMover/EnemyChaser/MobLinearChaser,
    // EnemyAttacker/RangedAttacker/MobPeriodicBulletRing) 클래스를 하드코딩하지 않고 Behaviour로
    // 통일해 있는 쪽을 찾아 담는다.
    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider;
    private Behaviour mover;
    private Behaviour attacker;

    private Vector3 originalScale;
    private Color originalColor;
    private bool hasPlayed;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();

        mover = GetComponent<EnemyAIPathMover>() as Behaviour;
        if (mover == null) mover = GetComponent<EnemyChaser>();
        if (mover == null) mover = GetComponent<MobLinearChaser>();

        attacker = GetComponent<EnemyAttacker>() as Behaviour;
        if (attacker == null) attacker = GetComponent<RangedAttacker>();
        if (attacker == null) attacker = GetComponent<MobPeriodicBulletRing>();

        originalScale = transform.localScale;
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    // RoomMonsterSpawner가 Instantiate 직후 SetActive(true)를 호출하는 순간 자동 시작된다.
    // hasPlayed로 1회성을 보장 - 오브젝트 풀링 등으로 나중에 다시 SetActive(true)가 걸려도
    // 연출이 중복 재생되지 않는다.
    private void OnEnable()
    {
        if (hasPlayed)
            return;

        hasPlayed = true;
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        SetGameplayActive(false);

        yield return PlayTelegraph();
        yield return PlayFlash();
        yield return PlayEmerge();

        if (postSpawnInvincibleDuration > 0f)
            yield return new WaitForSeconds(postSpawnInvincibleDuration);

        SetGameplayActive(true);
    }

    private void SetGameplayActive(bool active)
    {
        if (bodyCollider != null) bodyCollider.enabled = active;
        if (mover != null) mover.enabled = active;
        if (attacker != null) attacker.enabled = active;
    }

    private IEnumerator PlayTelegraph()
    {
        if (lockOnTelegraphPrefab == null)
            yield break;

        var telegraphObj = Instantiate(lockOnTelegraphPrefab, transform.position, Quaternion.identity);
        var telegraph = telegraphObj.GetComponent<SpawnLockOnTelegraph>();
        if (telegraph == null)
        {
            Destroy(telegraphObj);
            yield break;
        }

        bool done = false;
        telegraph.Play(telegraphStartScale, telegraphEndScale, telegraphDuration, telegraphHoldDuration,
            telegraphColor, telegraphRotationDegrees, onComplete: () => done = true);
        yield return new WaitUntil(() => done);
    }

    private IEnumerator PlayFlash()
    {
        if (flashBurstPrefab == null)
            yield break;

        var flashObj = Instantiate(flashBurstPrefab, transform.position, Quaternion.identity);
        var flash = flashObj.GetComponent<SpawnFlashBurst>();
        if (flash == null)
        {
            Destroy(flashObj);
            yield break;
        }

        bool done = false;
        flash.Play(flashDuration, flashStartScale, flashEndScale, Color.white, onComplete: () => done = true);
        yield return new WaitUntil(() => done);
    }

    private IEnumerator PlayEmerge()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
        }

        // 스프라이트가 켜지는 바로 그 순간, 지면에서 충격파 링이 함께 터진다 - 등장 자체의
        // 스케일/색 애니메이션(아래 while 루프)과는 별개로 동시에 흘러가도록 yield 없이 시작한다.
        if (enableEmergeImpactRing)
            PlayEmergeImpactRing();

        transform.localScale = originalScale * emergeScaleUndershoot;

        float elapsed = 0f;
        while (elapsed < emergeDuration)
        {
            elapsed += Time.deltaTime;
            float t = emergeDuration > 0f ? Mathf.Clamp01(elapsed / emergeDuration) : 1f;

            transform.localScale = originalScale * EvaluateOvershoot(t);

            // 색은 순백색을 emergeWhiteHoldFraction 구간만큼 그대로 유지하다가, 그 이후부터
            // 원래 색으로 서서히 돌아간다 - 곧바로 빠지는 것보다 "번쩍인 채로 잠깐 유지"되는 쪽이
            // 등장의 임팩트를 더 또렷하게 만든다.
            if (spriteRenderer != null)
            {
                float colorT = emergeWhiteHoldFraction < 1f
                    ? Mathf.Clamp01((t - emergeWhiteHoldFraction) / (1f - emergeWhiteHoldFraction))
                    : 0f;
                spriteRenderer.color = Color.Lerp(Color.white, originalColor, colorT);
            }

            yield return null;
        }

        transform.localScale = originalScale;
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    // flashBurstPrefab(SpawnFlashBurst)을 재사용하되 스프라이트만 필드 원형(BossIndicatorUtil의
    // 링 스프라이트)으로 바꿔서, 등장 순간 지면에서 확 퍼져나가는 충격파를 표현한다. 새 프리팹을
    // 만들지 않고 기존 자산을 재활용한 것 - 시퀀스 완료를 막지 않도록 대기 없이 그냥 발사만 한다.
    private void PlayEmergeImpactRing()
    {
        if (flashBurstPrefab == null)
            return;

        var impactObj = Instantiate(flashBurstPrefab, transform.position, Quaternion.identity);
        var impactRenderer = impactObj.GetComponent<SpriteRenderer>();
        if (impactRenderer != null)
            impactRenderer.sprite = BossIndicatorUtil.GetRingCircleSprite();

        var impact = impactObj.GetComponent<SpawnFlashBurst>();
        if (impact == null)
        {
            Destroy(impactObj);
            return;
        }

        impact.Play(emergeImpactRingDuration, emergeImpactRingStartScale, emergeImpactRingEndScale, emergeImpactRingColor);
    }

    // 0.6 -> 1.15(70% 지점) -> 1.0 오버슈트 곡선. 전반부는 빠르게 튀어나오고, 후반부는 살짝
    // 가라앉으며 안정된다(Soul Knight류 등장 연출의 "팡" 하는 느낌).
    private float EvaluateOvershoot(float t)
    {
        const float overshootPoint = 0.7f;

        if (t < overshootPoint)
        {
            float local = overshootPoint > 0f ? t / overshootPoint : 1f;
            return Mathf.Lerp(emergeScaleUndershoot, emergeScaleOvershoot, EaseOutQuad(local));
        }

        float remaining = 1f - overshootPoint;
        float localTail = remaining > 0f ? (t - overshootPoint) / remaining : 1f;
        return Mathf.Lerp(emergeScaleOvershoot, 1f, EaseOutQuad(localTail));
    }

    private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}
