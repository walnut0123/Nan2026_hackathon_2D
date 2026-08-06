public interface IInteractable
{
    /// <summary>지금 이 순간 실제로 상호작용 가능한 상태인지. false면 InteractionDetector가
    /// 프롬프트/선택 대상에서 제외한다(트리거 범위 안에 계속 있어도) - 예: 아직 방 클리어 전인
    /// 보물상자, 필드 드랍이 아닌 날아가는 카드 투사체.</summary>
    bool CanInteract { get; }

    void Interact(PlayerInventory inventory);
}
