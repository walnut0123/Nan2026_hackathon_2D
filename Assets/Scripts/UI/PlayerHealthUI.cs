using TMPro;
using UnityEngine;

// Health의 OnDamaged/OnDeath 이벤트를 구독해서 좌측 상단에 "현재체력/최대체력"(예: 5/5) 형태로 표시한다.
[RequireComponent(typeof(TextMeshProUGUI))]
public class PlayerHealthUI : MonoBehaviour
{
    private TextMeshProUGUI healthText;
    private Health playerHealth;

    private void Awake()
    {
        healthText = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        var playerInventory = FindObjectOfType<PlayerInventory>();
        if (playerInventory != null)
            playerHealth = playerInventory.GetComponent<Health>();

        if (playerHealth != null)
        {
            playerHealth.OnDamaged += HandleDamaged;
            playerHealth.OnDeath += HandleDeath;
        }
        else
        {
            Debug.LogWarning("[PlayerHealthUI] 플레이어의 Health를 찾지 못했습니다.");
        }

        UpdateText();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged -= HandleDamaged;
            playerHealth.OnDeath -= HandleDeath;
        }
    }

    private void HandleDamaged(float amount) => UpdateText();
    private void HandleDeath() => UpdateText();

    private void UpdateText()
    {
        if (playerHealth == null || healthText == null)
            return;

        // 체력 텍스트 자체는 정수로 표기 유지 (소수점은 데미지 텍스트에서만 보여준다).
        healthText.text = $"{Mathf.CeilToInt(Mathf.Max(playerHealth.CurrentHealth, 0f))}/{Mathf.CeilToInt(playerHealth.MaxHealth)}";
    }
}
