using System.Collections;
using UnityEngine;

// Paper Mario 스타일 "종이 뒤집힘" 효과 (2D 버전).
// 이동 방향의 부호가 반대로 바뀌는 순간을 감지해서,
// 이 오브젝트(스프라이트)의 localScale.x를 +값 -> 0 -> -값 으로 보간한다.
// 회전이 아니라 스케일을 0으로 눌렀다 펴는 방식이라 "종이가 얇게 접히며 뒤집히는" 느낌이 난다.
// 반드시 시각 요소(스프라이트)에 붙이고, 이동 로직을 담당하는 AgentMover와는 분리해서 사용한다.
public class PaperFlipVisual : MonoBehaviour
{
    public enum Axis { X, Y }

    [Header("참조")]
    [Tooltip("이동 방향을 읽어올 AgentMover. 비워두면 부모에서 자동으로 찾음")]
    [SerializeField] private AgentMover agentMover;

    [Header("플립 설정")]
    [Tooltip("어떤 축의 부호가 바뀔 때 플립을 트리거할지 (좌우 이동 기준 기본값 X축)")]
    [SerializeField] private Axis watchAxis = Axis.X;

    [Tooltip("플립 애니메이션 소요 시간(초). 너무 빠르면 그냥 순간 반전처럼 보이고, 너무 느리면 굼떠 보임. 0.1~0.18 권장")]
    [SerializeField] private float flipDuration = 0.12f;

    [Tooltip("이동 입력이 이 값보다 작으면 방향 전환으로 취급하지 않음 (제자리에서 미세하게 떨리는 것 방지)")]
    [SerializeField] private float inputDeadzone = 0.05f;

    [Tooltip("스케일 보간 곡선. 기본은 선형(등속). 가운데(0)에서 살짝 멈칫하는 느낌을 원하면 커브를 손봐도 됨")]
    [SerializeField] private AnimationCurve flipCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private float baseScaleX;   // 원래 폭 (절대값)
    private int facingSign = 1; // 현재 바라보는 방향 부호 (+1 / -1)
    private Coroutine flipRoutine;

    void Awake()
    {
        if (agentMover == null)
            agentMover = GetComponentInParent<AgentMover>();

        if (agentMover == null)
            Debug.LogWarning("[PaperFlipVisual] AgentMover를 찾지 못했습니다. Inspector에서 직접 연결해주세요.");

        baseScaleX = Mathf.Abs(transform.localScale.x);
        facingSign = transform.localScale.x >= 0f ? 1 : -1;
    }

    void Update()
    {
        if (agentMover == null) return;

        Vector2 dir = agentMover.MovementInput;
        float axisValue = (watchAxis == Axis.Y) ? dir.y : dir.x;

        if (Mathf.Abs(axisValue) < inputDeadzone) return;

        int newSign = axisValue > 0f ? 1 : -1;

        if (newSign != facingSign)
        {
            facingSign = newSign;

            if (flipRoutine != null)
                StopCoroutine(flipRoutine);

            flipRoutine = StartCoroutine(FlipTo(newSign));
        }
    }

    private IEnumerator FlipTo(int targetSign)
    {
        float startX = transform.localScale.x;
        float endX = baseScaleX * targetSign;
        float t = 0f;

        while (t < flipDuration)
        {
            t += Time.deltaTime;
            float p = flipCurve.Evaluate(Mathf.Clamp01(t / flipDuration));

            Vector3 s = transform.localScale;
            s.x = Mathf.Lerp(startX, endX, p);
            transform.localScale = s;

            yield return null;
        }

        Vector3 finalScale = transform.localScale;
        finalScale.x = endX;
        transform.localScale = finalScale;

        flipRoutine = null;
    }
}
