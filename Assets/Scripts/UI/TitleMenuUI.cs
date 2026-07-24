using UnityEngine;
using UnityEngine.UI;

/// <summary>Wires up the Title scene's menu: Play Start / 새로운 게임 / 설정. The buttons and
/// settings panel are authored directly in the Title scene hierarchy (under this same
/// GameObject) so they can be freely redesigned in the Editor - this script only finds
/// them by name and hooks up behavior, it does not build any UI.</summary>
public class TitleMenuUI : MonoBehaviour
{
    private GameObject settingsPanel;

    private void Start()
    {
        transform.Find("Btn_PlayStart").GetComponent<Button>().onClick.AddListener(OnPlayStart);
        transform.Find("Btn_NewGame").GetComponent<Button>().onClick.AddListener(OnNewGame);
        transform.Find("Btn_Settings").GetComponent<Button>().onClick.AddListener(OnSettings);

        settingsPanel = transform.Find("SettingsPanel").gameObject;
        var card = settingsPanel.transform.Find("Card");

        var bgmSlider = card.Find("BGM 음량 Row/Slider").GetComponent<Slider>();
        bgmSlider.value = AudioManager.Instance != null ? AudioManager.Instance.BgmVolume : 1f;
        bgmSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetBgmVolume(v));

        var sfxSlider = card.Find("효과음 음량 Row/Slider").GetComponent<Slider>();
        sfxSlider.value = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f;
        sfxSlider.onValueChanged.AddListener(v => AudioManager.Instance?.SetSfxVolume(v));

        card.Find("CloseButton").GetComponent<Button>().onClick.AddListener(() => settingsPanel.SetActive(false));
    }

    private void OnPlayStart() => GameManager.Instance.PlayStart();
    private void OnNewGame() => GameManager.Instance.NewGame();
    private void OnSettings() => settingsPanel.SetActive(true);
}
