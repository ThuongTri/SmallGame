using System.Collections.Generic;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    private HashSet<string> collectedItems = new HashSet<string>();

    [Header("Assembled Map (Inventory)")]
    [Tooltip("Sprite hiển thị khi map đã mở")]
    public Sprite assembledMapSprite;
    [Tooltip("ID dùng để lưu map vào Lore/Inventory")]
    public string assembledMapID = "assembled_map";
    [Tooltip("Title để show trong Codex/Inventory")]
    public string assembledMapTitle = "Bản đồ khu rừng";

    [Header("References for UI feedback")]
    public GameObject toastPrefab; 
    public Transform toastParent;

    // Biến kiểm tra xem đã có map chưa
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

        // Logic nhặt 1 cái là mở map luôn
        if (!mapAssembled && itemID.StartsWith("map_"))
        {
            mapAssembled = true;
            OnAssembleMap();
        }
    }

    private void OnAssembleMap()
    {
        Debug.Log("[ObjectiveManager] Map Unlocked!");

        // 1. Thêm vào Lore/Inventory
        if (LoreManager.Instance != null)
        {
            LoreManager.Instance.AddLore(assembledMapID, assembledMapTitle, "Bản đồ chi tiết khu rừng. Nhấn 'M' để xem.", assembledMapSprite);
        }

        // 2. Hiện thông báo nhỏ
        if (toastPrefab != null && toastParent != null)
        {
            var go = Instantiate(toastPrefab, toastParent);
            var txt = go.GetComponentInChildren<UnityEngine.UI.Text>();
            // Nếu dùng TMP: var txt = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (txt != null) txt.text = "Đã tìm thấy Bản Đồ! (Nhấn M)";
            Destroy(go, 3f);
        }
    }

    // Kiểm tra có item (để MapController gọi)
    public bool HasItem(string id)
    {
        return collectedItems.Contains(id) || (id == assembledMapID && mapAssembled);
    }

    // ✅ HÀM NÀY VỪA BỊ THIẾU, GIỜ ĐÃ THÊM LẠI:
    // MapUIController cần hàm này để lấy ảnh bản đồ hiển thị lên
    public Sprite GetAssembledMapSprite()
    {
        return mapAssembled ? assembledMapSprite : null;
    }
}