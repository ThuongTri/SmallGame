using System.Collections.Generic;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    private HashSet<string> collectedItems = new HashSet<string>();

    [Header("Assembled Map (Inventory)")]
    [Tooltip("Sprite hiển thị khi map được ghép")]
    public Sprite assembledMapSprite;
    [Tooltip("ID dùng để lưu assembled map vào Lore/Inventory")]
    public string assembledMapID = "assembled_map";
    [Tooltip("Title để show trong Codex/Inventory")]
    public string assembledMapTitle = "Bản đồ hoàn chỉnh";

    [Header("References for UI feedback")]
    public GameObject toastPrefab; // small UI popup, optional
    public Transform toastParent;

    [Header("Bunker door references")]
    public GameObject bunkerDoor; // assign door object with Animator
    public string openDoorAnimTrigger = "Unlock";

    private bool mapAssembled = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OnItemCollected(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return;
        if (collectedItems.Contains(itemID)) return;

        collectedItems.Add(itemID);
        Debug.Log($"[ObjectiveManager] Collected: {itemID}");

        CheckMapAssembly();
    }

    private void CheckMapAssembly()
    {
        // count map pieces with prefix "map_"
        int mapCount = 0;
        foreach (var id in collectedItems)
            if (id.StartsWith("map_")) mapCount++;

        if (!mapAssembled && mapCount >= 3)
        {
            mapAssembled = true;
            OnAssembleMap();
        }
    }

    private void OnAssembleMap()
    {
        Debug.Log("[ObjectiveManager] Map assembled!");
        // 1) Thêm lore/entry nếu muốn (dùng LoreManager nếu bạn muốn)
        if (LoreManager.Instance != null)
        {
            LoreManager.Instance.AddLore(assembledMapID, assembledMapTitle, "Một bản đồ ghép lại, đánh dấu vị trí bunker.", null);
        }

        // 2) Nếu bạn có InventoryManager, gọi nó để thêm item "assembled_map"
        // Example (uncomment & chỉnh nếu bạn có InventoryManager):
        // InventoryManager.Instance.AddItem(assembledMapID, assembledMapSprite, assembledMapTitle);

        // 3) Nếu không có Inventory, ta có thể show toast + set một flag để MapUI truy cập
        if (toastPrefab != null && toastParent != null)
        {
            var go = Instantiate(toastPrefab, toastParent);
            var txt = go.GetComponentInChildren<UnityEngine.UI.Text>();
            if (txt != null) txt.text = "Bạn đã ghép được bản đồ!";
            Destroy(go, 3f);
        }
    }

    // Kiểm tra có item
    public bool HasItem(string id)
    {
        return collectedItems.Contains(id) || (id == assembledMapID && mapAssembled);
    }

    // Dùng để lấy sprite assembled map (MapUI sẽ gọi)
    public Sprite GetAssembledMapSprite()
    {
        return mapAssembled ? assembledMapSprite : null;
    }

    // Optionally expose count
    public int GetMapPieceCount()
    {
        int c = 0;
        foreach (var id in collectedItems) if (id.StartsWith("map_")) c++;
        return c;
    }
}
