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
    [Tooltip("Cần đủ các mảnh map này mới ghép xong. Nếu thiếu, vẫn fallback theo prefix để map 1 mảnh vẫn chạy.")]
    public string[] requiredMapPieceIDs = new string[] { "map_1", "map_2", "map_3" };
    [Tooltip("Fallback: nếu không dùng danh sách cụ thể, cần ít nhất bao nhiêu item bắt đầu bằng prefix này.")]
    public int fallbackMapPieceCountRequired = 1;
    public string fallbackMapPiecePrefix = "map_";

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
        string id = itemID.Trim().ToLowerInvariant();
        if (collectedItems.Contains(id)) return;

        collectedItems.Add(id);
        Debug.Log($"[ObjectiveManager] Collected: {id}");

        if (!mapAssembled && ShouldAssembleMapNow(id))
        {
            mapAssembled = true;
            OnAssembleMap();
        }
    }

    bool ShouldAssembleMapNow(string collectedId)
    {
        if (collectedId == assembledMapID.Trim().ToLowerInvariant())
            return true;

        if (requiredMapPieceIDs != null && requiredMapPieceIDs.Length > 0)
        {
            bool hasAnyRequired = false;
            for (int i = 0; i < requiredMapPieceIDs.Length; i++)
            {
                string req = requiredMapPieceIDs[i];
                if (string.IsNullOrWhiteSpace(req)) continue;
                hasAnyRequired = true;
                if (!collectedItems.Contains(req.Trim().ToLowerInvariant()))
                {
                    hasAnyRequired = false;
                    break;
                }
            }
            if (hasAnyRequired) return true;
        }

        // Fallback: đủ số lượng mảnh map theo prefix.
        string prefix = string.IsNullOrWhiteSpace(fallbackMapPiecePrefix) ? "map_" : fallbackMapPiecePrefix.Trim().ToLowerInvariant();
        int needed = Mathf.Max(1, fallbackMapPieceCountRequired);
        int count = 0;
        foreach (var k in collectedItems)
        {
            if (k != null && k.StartsWith(prefix))
                count++;
        }
        if (count >= needed) return true;

        // Fallback mềm cho map có 1 mảnh ID tùy chỉnh (vd: map_piece_final, forest_map, ...).
        return collectedId.Contains("map");
    }

    private void OnAssembleMap()
    {
        Debug.Log("[ObjectiveManager] Map Unlocked!");

        // 1. Thêm vào Lore/Inventory
        if (LoreManager.Instance != null)
        {
            Sprite mapSprite = null;
            try
            {
                mapSprite = assembledMapSprite;
            }
            catch (UnassignedReferenceException)
            {
                mapSprite = null;
            }
            if (mapSprite == null)
            {
                // Tránh UnassignedReferenceException khi designer chưa gán sprite.
                var mapUI = FindObjectOfType<MapUIController>(true);
                if (mapUI != null && mapUI.mapRawImage != null)
                {
                    Texture tex = mapUI.mapRawImage.texture;
                    Texture2D tex2D = tex as Texture2D;
                    if (tex2D != null)
                        mapSprite = Sprite.Create(tex2D, new Rect(0, 0, tex2D.width, tex2D.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
            LoreManager.Instance.AddLore(assembledMapID, assembledMapTitle, "Bản đồ chi tiết khu rừng. Nhấn 'M' để xem.", mapSprite);
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
        if (string.IsNullOrWhiteSpace(id)) return false;
        string low = id.Trim().ToLowerInvariant();
        return collectedItems.Contains(low) || (low == assembledMapID.Trim().ToLowerInvariant() && mapAssembled);
    }

    // ✅ HÀM NÀY VỪA BỊ THIẾU, GIỜ ĐÃ THÊM LẠI:
    // MapUIController cần hàm này để lấy ảnh bản đồ hiển thị lên
    public Sprite GetAssembledMapSprite()
    {
        if (!mapAssembled) return null;
        try
        {
            return assembledMapSprite;
        }
        catch (UnassignedReferenceException)
        {
            return null;
        }
    }
}