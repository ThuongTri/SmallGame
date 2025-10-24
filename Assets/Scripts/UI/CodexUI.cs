using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CodexUI : MonoBehaviour
{
    public Transform gridParent;          // GridLayoutGroup
    public GameObject itemSlotPrefab;     // Prefab ItemSlot
    public GameObject tooltipPanel;       // Panel chi tiết
    public TMP_Text tooltipText;          // Text chi tiết

    private List<GameObject> slots = new List<GameObject>();

    void Start()
    {
        HideTooltip();
        gameObject.SetActive(false);
    }

    public void UpdateUI(List<LoreData> entries)
    {
        foreach (var s in slots)
            Destroy(s);
        slots.Clear();

        foreach (var entry in entries)
        {
            var slot = Instantiate(itemSlotPrefab, gridParent);
            var icon = slot.transform.Find("Icon").GetComponent<Image>();
            var nameText = slot.transform.Find("Name").GetComponent<TMP_Text>();

            icon.sprite = entry.icon;
            nameText.text = entry.title;

            // Khi click vào slot -> hiển thị tooltip
            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => ShowTooltip(entry));
            }

            slots.Add(slot);
        }
    }

    public void ShowTooltip(LoreData lore)
    {
        tooltipPanel.SetActive(true);
        tooltipText.text = $"{lore.title}\n\n{lore.description}";
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
}
