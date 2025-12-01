using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CodexUI : MonoBehaviour
{
    [Header("Cài đặt Kho Đồ")]
    public Transform gridParent; // Nơi chứa các ô đồ
    public GameObject itemPrefab; // Khuôn mẫu ô đồ (Chỉ có 1 object vừa là Image vừa là Button)

    [Header("Bảng Chi Tiết (Hiện khi bấm vào)")]
    public GameObject detailPanel; 
    public TextMeshProUGUI titleText; 
    public TextMeshProUGUI descText; 
    public Image detailImage; 

    public void UpdateUI(List<LoreData> loreList)
    {
        // 1. Xóa sạch các ô đồ cũ
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        // 2. Tạo lại các ô đồ mới
        foreach (LoreData lore in loreList)
        {
            // Tạo ra một ô đồ mới từ Prefab
            GameObject newSlot = Instantiate(itemPrefab, gridParent);
            
            // --- PHẦN SET ICON (CODE MỚI) ---
            // Lấy ngay component Image nằm trên chính object newSlot
            Image slotImage = newSlot.GetComponent<Image>();

            if (slotImage != null)
            {
                if (lore.icon != null)
                {
                    // Gán sprite của món đồ vào
                    slotImage.sprite = lore.icon;
                    // Đảm bảo màu trắng để thấy rõ ảnh gốc
                    slotImage.color = Color.white; 
                    slotImage.enabled = true;
                }
                else
                {
                    // Nếu không có icon (ví dụ bị null) thì ẩn đi hoặc hiện màu trong suốt
                    // slotImage.enabled = false; // Hoặc
                    slotImage.color = Color.clear;
                }
            }
            // -------------------------------

            // --- PHẦN GẮN NÚT BẤM ---
            // Lấy component Button nằm trên chính object newSlot
            Button btn = newSlot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => ShowDetail(lore));
            }
            // -----------------------
        }
    }

    public void ShowDetail(LoreData data)
    {
        if (detailPanel != null) detailPanel.SetActive(true);
        if (titleText != null) titleText.text = data.title;
        if (descText != null) descText.text = data.description;
        if (detailImage != null) detailImage.sprite = data.icon;
    }

    public void HideTooltip()
    {
        if (detailPanel != null) 
            detailPanel.SetActive(false);
    }
}