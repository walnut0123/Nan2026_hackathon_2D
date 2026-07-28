using UnityEngine;

// 플레이어 스프라이트가 바라보는 좌우 방향을 결정한다: CardAutoAttack이 현재 조준 중인 적이 있으면
// 그 적 방향을, 없으면 이동 입력 방향을 우선한다. PaperFlipVisual(페이퍼 아트 스타일 전용 플립 연출)을
// 대체하는 게 아니라 그와 별개로, 실제 "바라보는 방향" 자체를 결정하는 로직이라 이름을 다르게 뒀다.
public class PlayerFacing : MonoBehaviour
{
    [Tooltip("좌우 방향을 적용할 SpriteRenderer. 비워두면 자식에서 자동으로 찾음")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Tooltip("타겟 방향을 읽어올 CardAutoAttack. 비워두면 같은 오브젝트에서 자동으로 찾음")]
    [SerializeField] private CardAutoAttack cardAutoAttack;

    [Tooltip("이동 방향을 읽어올 AgentMover. 비워두면 같은 오브젝트에서 자동으로 찾음")]
    [SerializeField] private AgentMover agentMover;

    [Tooltip("이 값보다 작은 x축 방향 성분은 무시하고 기존 바라보는 방향을 유지한다(수직 이동/타겟일 때 떨림 방지)")]
    [SerializeField] private float deadzone = 0.05f;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();

        if (cardAutoAttack == null)
            cardAutoAttack = GetComponent<CardAutoAttack>();

        if (agentMover == null)
            agentMover = GetComponent<AgentMover>();
    }

    // CardAutoAttack.Update()가 이번 프레임의 CurrentTarget을 정한 뒤에 읽어야 하므로 LateUpdate 사용
    // (WeaponAim과 동일한 이유).
    private void LateUpdate()
    {
        if (targetRenderer == null)
            return;

        Vector2 direction;

        if (cardAutoAttack != null && cardAutoAttack.CurrentTarget != null)
            direction = (Vector2)(cardAutoAttack.CurrentTarget.position - transform.position);
        else if (agentMover != null)
            direction = agentMover.MovementInput;
        else
            return;

        if (Mathf.Abs(direction.x) < deadzone)
            return;

        targetRenderer.flipX = direction.x < 0f;
    }
}
