using UnityEngine;

public class InventoryToggle : MonoBehaviour
{
    public GameObject inventoryPanel;
    public CodexUI codexUI;
    public string playerObjectName = "Player";
    public MonoBehaviour playerControllerComponent;

    private MonoBehaviour cachedPlayerController;

    void Start()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (codexUI != null) codexUI.HideTooltip();

        if (playerControllerComponent != null)
        {
            cachedPlayerController = playerControllerComponent;
        }
        else
        {
            var playerObj = GameObject.Find(playerObjectName);
            if (playerObj != null)
            {
                var comp = playerObj.GetComponent<MonoBehaviour>();
                if (comp != null) cachedPlayerController = comp;
            }
        }

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

        bool isOpen = !inventoryPanel.activeSelf;
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
