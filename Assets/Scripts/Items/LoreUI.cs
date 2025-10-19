using UnityEngine;
using TMPro;
using System.Text;

public class LoreUI : MonoBehaviour
{
    [Header("UI Thông tin")]
    public GameObject lorePanel;          // panel hiện lore
    public TextMeshProUGUI loreListText;  // danh sách các lore đã nhặt

    private bool isOpen = false;

    void Start()
    {
        if (lorePanel != null)
            lorePanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleLorePanel();
        }
    }

    void ToggleLorePanel()
    {
        isOpen = !isOpen;
        if (lorePanel != null)
        {
            lorePanel.SetActive(isOpen);
            if (isOpen)
                RefreshLoreList();
        }
    }

    void RefreshLoreList()
    {
        if (loreListText == null) return;

		var lores = LoreManager.Instance != null ? LoreManager.Instance.GetAllLore() : null;
        if (lores == null || lores.Count == 0)
        {
            loreListText.text = "<i>Chưa thu thập được gì...</i>";
            return;
        }

        StringBuilder sb = new StringBuilder();
        foreach (var entry in lores)
        {
            sb.AppendLine($"<b>{entry.Key}</b>");
            sb.AppendLine($"{entry.Value}");
            sb.AppendLine();
        }

        loreListText.text = sb.ToString();
    }
}
