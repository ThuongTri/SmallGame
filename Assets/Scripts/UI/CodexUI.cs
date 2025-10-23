using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// CodexUI: hiển thị danh sách vật phẩm/lore đã thu thập và tooltip
public class CodexUI : MonoBehaviour
{
    public Transform gridParent;          // Grid chứa các slot
    public GameObject itemSlotPrefab;     // Prefab 1 slot có Image icon
    public GameObject tooltipPanel;       // Panel hiển thị chi tiết
    public TMP_Text tooltipText;

    private List<GameObject> currentSlots = new List<GameObject>();

    public void UpdateUI(List<LoreData> entries)
    {
        // Xóa slot cũ
        foreach (var slot in currentSlots)
            Destroy(slot);
        currentSlots.Clear();

        // Tạo slot mới
        foreach (var entry in entries)
        {
            if (entry == null) continue;
            var slot = Instantiate(itemSlotPrefab, gridParent);
            var img = slot.transform.GetChild(0).GetComponent<Image>();
            if (img != null) img.sprite = entry.icon;

            // Thêm hover xử lý tooltip
            var hover = slot.AddComponent<SlotHover>();
            hover.Initialize(entry, tooltipPanel, tooltipText);

            currentSlots.Add(slot);
        }
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    // Lớp xử lý hover
    private class SlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private LoreData data;
        private GameObject tooltip;
        private TMP_Text tooltipText;

        public void Initialize(LoreData entry, GameObject tooltipPanel, TMP_Text detailText)
        {
            data = entry;
            tooltip = tooltipPanel;
            tooltipText = detailText;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltip == null || tooltipText == null || data == null) return;
            tooltip.SetActive(true);
            tooltipText.text = $"<b>{data.name}</b>\n\n{data.description}";
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (tooltip == null) return;
            tooltip.SetActive(false);
        }
    }
}
