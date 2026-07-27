using UnityEngine;

// 씬에 이 컴포넌트가 붙은 오브젝트(예: 테스트용 허수아비 Dummy_Scarecrow)가 존재하면,
// EnemyChaser/EnemyAttacker는 플레이어 대신 이 오브젝트를 최우선 타겟으로 삼는다.
// 없으면 기존처럼 PlayerInventory를 찾아 플레이어를 타겟으로 삼는다(하위 호환).
public class PriorityTarget : MonoBehaviour
{
}
