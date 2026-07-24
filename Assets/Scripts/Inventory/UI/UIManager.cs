using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central registry for every UI panel in the scene. Individual UI scripts register
/// themselves here instead of exposing serialized cross-references to each other, so
/// there is a single place to see (and toggle) every GUI element at once.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private KeyCode debugOverlayKey = KeyCode.F1;

    private readonly Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();
    private bool showDebugOverlay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public static void Register(string panelName, GameObject panel)
    {
        if (Instance == null || panel == null)
            return;

        Instance.panels[panelName] = panel;
    }

    public static void Unregister(string panelName)
    {
        if (Instance == null)
            return;

        Instance.panels.Remove(panelName);
    }

    private void Update()
    {
        if (Input.GetKeyDown(debugOverlayKey))
            showDebugOverlay = !showDebugOverlay;
    }

    private void OnGUI()
    {
        if (!showDebugOverlay)
            return;

        float height = 24 + panels.Count * 26;
        GUILayout.BeginArea(new Rect(10, 10, 280, height), GUI.skin.box);
        GUILayout.Label($"UI Manager ({debugOverlayKey} to toggle)");

        foreach (var pair in panels)
        {
            GUILayout.BeginHorizontal();
            bool isActive = pair.Value != null && pair.Value.activeSelf;
            GUILayout.Label($"{pair.Key} [{(isActive ? "ON" : "OFF")}]", GUILayout.Width(180));

            if (pair.Value != null && GUILayout.Button(isActive ? "Hide" : "Show", GUILayout.Width(70)))
                pair.Value.SetActive(!isActive);

            GUILayout.EndHorizontal();
        }

        GUILayout.EndArea();
    }
}
