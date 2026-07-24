using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptUI : MonoBehaviour
{
    // Discovered automatically instead of serialized, so nothing can be mis-wired
    // by dragging the wrong object in the Inspector.
    private InteractionDetector detector;
    private TextMeshProUGUI promptText;
    private Button interactButton;

    private void Awake()
    {
        promptText = GetComponentInChildren<TextMeshProUGUI>(true);
        interactButton = GetComponent<Button>();
    }

    private void Start()
    {
        detector = FindObjectOfType<InteractionDetector>();
        detector.OnPromptChanged += HandlePromptChanged;
        gameObject.SetActive(false);

        if (interactButton != null)
            interactButton.onClick.AddListener(() => detector.TryInteract());

        UIManager.Register("Interaction Prompt", gameObject);
    }

    private void OnDestroy()
    {
        if (detector != null)
            detector.OnPromptChanged -= HandlePromptChanged;

        if (interactButton != null)
            interactButton.onClick.RemoveAllListeners();

        UIManager.Unregister("Interaction Prompt");
    }

    private void HandlePromptChanged(string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        promptText.text = $"{label} 줍기";
    }
}
