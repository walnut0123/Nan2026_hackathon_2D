using TMPro;
using UnityEngine;

// PlayerHealthUI와 동일한 방식으로 "현재체력/최대체력"을 표시하지만 대상이 보스다.
[RequireComponent(typeof(TextMeshProUGUI))]
public class BossHealthTextUI : MonoBehaviour
{
    private TextMeshProUGUI healthText;
    private Health bossHealth;

    private void Awake()
    {
        healthText = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        var boss = FindObjectOfType<BossAttackController>(true);
        if (boss != null)
            bossHealth = boss.GetComponent<Health>();

        if (bossHealth != null)
        {
            bossHealth.OnDamaged += HandleDamaged;
            bossHealth.OnDeath += HandleDeath;
        }
        else
        {
            Debug.LogWarning("[BossHealthTextUI] 보스의 Health를 찾지 못했습니다.");
        }

        UpdateText();
    }

    private void OnDestroy()
    {
        if (bossHealth != null)
        {
            bossHealth.OnDamaged -= HandleDamaged;
            bossHealth.OnDeath -= HandleDeath;
        }
    }

    private void HandleDamaged(float amount) => UpdateText();
    private void HandleDeath() => UpdateText();

    private void UpdateText()
    {
        if (bossHealth == null || healthText == null)
            return;

        healthText.text = $"{Mathf.CeilToInt(Mathf.Max(bossHealth.CurrentHealth, 0f))}/{Mathf.CeilToInt(bossHealth.MaxHealth)}";
    }
}
