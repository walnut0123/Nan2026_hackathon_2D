using UnityEngine;

// AgentMover의 이동 입력을 Animator의 Speed 파라미터로 전달한다.
// EnemyAnimator와 동일한 역할이지만, Player는 위치 델타 대신 입력값(MovementInput)을 그대로 쓴다 -
// AgentMover가 가속/감속으로 rb.velocity를 다음 FixedUpdate에서야 반영하므로 입력 기준이 더 즉각적이다.
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    [Tooltip("이동 입력을 읽어올 AgentMover. 비워두면 부모에서 자동으로 찾음")]
    [SerializeField] private AgentMover agentMover;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (agentMover == null)
            agentMover = GetComponentInParent<AgentMover>();

        if (agentMover == null)
            Debug.LogWarning("[PlayerAnimator] AgentMover를 찾지 못했습니다. Inspector에서 직접 연결해주세요.");
    }

    private void Update()
    {
        if (agentMover == null) return;

        animator.SetFloat("Speed", agentMover.MovementInput.magnitude);
    }
}
