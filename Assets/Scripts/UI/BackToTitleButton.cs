using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Simple OnGUI back button for the LLMSpeedTest scene - matches the "씬 전환 UI
/// 버튼 영역(X:40, Y:40, 300x100)" LLMSpeedTest.cs already reserves space for by starting its
/// own content lower on screen.</summary>
public class BackToTitleButton : MonoBehaviour
{
    private void OnGUI()
    {
        if (GUI.Button(new Rect(40f, 40f, 300f, 100f), "타이틀로"))
            SceneManager.LoadScene(GameManager.TitleSceneName);
    }
}
