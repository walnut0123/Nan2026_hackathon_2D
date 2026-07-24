using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>Dev-only shortcut, built at runtime so the LLM speed-test tool doesn't need any
/// hand-authored UI in the Title scene - the tool now lives in its own scene.</summary>
public class GoToLLMSpeedTestButton : MonoBehaviour
{
    private const string LLMSpeedTestSceneName = "LLMSpeedTest";

    private void Start()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[GoToLLMSpeedTestButton] No Canvas found in the Title scene; cannot create button.");
            return;
        }

        var buttonGO = new GameObject("Btn_LLMSpeedTest", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(canvas.transform, false);

        var rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(300f, 80f);
        rect.anchoredPosition = new Vector2(-40f, 40f);

        buttonGO.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(buttonGO.transform, false);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textGO.GetComponent<Text>();
        text.text = "LLM 속도 테스트";
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white;
        text.fontSize = 24;

        buttonGO.GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene(LLMSpeedTestSceneName));
    }
}
