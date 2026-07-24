using UnityEngine;
using UnityEngine.UI;

/// <summary>Wires up the in-game 가방(Inventory) toggle button in the Main scene. Both the
/// button and InventoryPanel are authored directly in the scene hierarchy (as siblings on the
/// game Canvas) so they can be freely redesigned in the Editor - this script only finds them
/// by name and hooks up behavior, it does not build any UI. Mirrors MainSettingsButton's
/// pattern for its Btn_Settings/SettingsPanel pair.</summary>
public class InventoryToggleButton : MonoBehaviour
{
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openedSprite;

    private Image bagImage;
    private GameObject inventoryPanel;

    private void Awake()
    {
        bagImage = GetComponent<Image>();
    }

    private void Start()
    {
        var inventoryUI = FindObjectOfType<InventoryUI>(true);
        if (inventoryUI == null)
        {
            Debug.LogWarning("[InventoryToggleButton] No InventoryUI found in the Main scene; cannot wire up the bag button.");
            return;
        }

        inventoryPanel = inventoryUI.gameObject;
        GetComponent<Button>().onClick.AddListener(Toggle);

        // Collapsed by default; only expands once the player taps the bag.
        SetOpen(false);
    }

    private void Toggle() => SetOpen(!inventoryPanel.activeSelf);

    private void SetOpen(bool open)
    {
        inventoryPanel.SetActive(open);
        bagImage.sprite = open ? openedSprite : closedSprite;
    }
}
