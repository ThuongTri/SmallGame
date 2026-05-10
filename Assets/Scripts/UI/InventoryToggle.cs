using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    [Header("System switch")]
    public bool disableInventorySystem = true;
    public GameObject inventoryPanel;
    public CodexUI codexUI;
    public string playerObjectName = "Player";
    public PlayerController playerControllerComponent;
    public MapUIController mapUIController;

    private PlayerController cachedPlayerController;

    void Start()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (codexUI != null) codexUI.HideTooltip();

        if (disableInventorySystem)
        {
            enabled = false; // Tắt hẳn hệ kho đồ theo yêu cầu.
            return;
        }

        if (playerControllerComponent != null)
        {
            cachedPlayerController = playerControllerComponent;
        }
        else
        {
            var playerObj = GameObject.Find(playerObjectName);
            if (playerObj != null)
            {
                var comp = playerObj.GetComponent<PlayerController>();
                if (comp != null) cachedPlayerController = comp;
            }
        }

        if (mapUIController == null)
            mapUIController = FindObjectOfType<MapUIController>(true);

        LockCursor(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleInventory();
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;
        if (EndingManager.Instance != null && EndingManager.Instance.IsShowingChoice) return;

        bool isOpen = !inventoryPanel.activeSelf;

        // Avoid UI overlap causing broken cursor/control states.
        if (isOpen && mapUIController != null && mapUIController.panel != null && mapUIController.panel.activeSelf)
            mapUIController.ToggleMap();

        inventoryPanel.SetActive(isOpen);

        if (isOpen)
        {
            LockCursor(false);
            if (cachedPlayerController != null) cachedPlayerController.enabled = false;

            if (codexUI != null && LoreManager.Instance != null)
                codexUI.UpdateUI(LoreManager.Instance.collectedLore);
        }
        else
        {
            LockCursor(true);
            if (cachedPlayerController != null) cachedPlayerController.enabled = true;
            if (codexUI != null) codexUI.HideTooltip();
        }
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
