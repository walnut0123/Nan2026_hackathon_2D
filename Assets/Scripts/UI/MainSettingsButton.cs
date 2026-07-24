using UnityEngine;
using UnityEngine.UI;

/// <summary>Wires up the in-game 설정(Settings) button and panel in the Main scene. Both are
/// authored directly in the scene hierarchy (as siblings of InventoryPanel on the game
/// Canvas) so they can be freely redesigned in the Editor - this script only finds them by
/// name and hooks up behavior, it does not build any UI.</summary>
public class MainSettingsButton : MonoBehaviour
{
    private GameObject settingsPanel;

    private void Start()
    {
        var inventoryUI = FindObjectOfType<InventoryUI>(true);
        if (inventoryUI == null)
        {
            Debug.LogWarning("[MainSettingsButton] No InventoryUI found in the Main scene; cannot wire up settings UI.");
            return;
        }

        var canvasTransform = inventoryUI.transform.parent;

        canvasTransform.Find("Btn_Settings").GetComponent<Button>().onClick.AddListener(OnSettingsClicked);

        settingsPanel = canvasTransform.Find("SettingsPanel").gameObject;
        var card = settingsPanel.transform.Find("Card");

        var bgmSlider = card.Find("BGM 음량 Row/Slider").GetComponent<Slider>();
        bgmSlider.value = AudioManager.Instance != null ? AudioManager.Instance.BgmVolume : 1f;
        bgmSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetBgmVolume(v));

        var sfxSlider = card.Find("효과음 음량 Row/Slider").GetComponent<Slider>();
        sfxSlider.value = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;
        sfxSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSfxVolume(v));

        card.Find("SaveAndExitButton").GetComponent<Button>().onClick.AddListener(OnSaveAndExit);
        card.Find("CloseButton").GetComponent<Button>().onClick.AddListener(() => settingsPanel.SetActive(false));
    }

    private void OnSettingsClicked() => settingsPanel.SetActive(true);

    private void OnSaveAndExit()
    {
        settingsPanel.SetActive(false);
        GameManager.Instance.SaveAndExitToTitle();
    }
}
