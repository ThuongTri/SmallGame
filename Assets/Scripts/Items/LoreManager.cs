using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LoreManager : MonoBehaviour
{
    public static LoreManager Instance;

    [Header("UI hiển thị lore")]
    [Tooltip("Panel chứa UI hiển thị lore")]
    public GameObject loreUI;

    [Tooltip("Text hiển thị danh sách lore")]
    public TextMeshProUGUI loreListText;

    private bool isOpen = false;

    // Dữ liệu lore thu thập được
    private Dictionary<string, string> collectedLore = new Dictionary<string, string>();

    void Awake()
    {
        if (Instance == null) 
            Instance = this;
        else 
            Destroy(gameObject);

        if (loreUI != null)
            loreUI.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleLoreUI();
        }
    }

    /// <summary>
    /// Thêm lore mới vào danh sách nếu chưa có
    /// </summary>
    public void AddLore(string id, string text)
    {
        if (!collectedLore.ContainsKey(id))
        {
            collectedLore.Add(id, text);
            Debug.Log($"[LoreManager] Đã thêm lore: {id}");
        }
    }

    /// <summary>
    /// Trả về bản sao dữ liệu lore (chỉ đọc)
    /// </summary>
    public Dictionary<string, string> GetAllLore()
    {
        return new Dictionary<string, string>(collectedLore);
    }

    /// <summary>
    /// Hiện hoặc ẩn UI lore
    /// </summary>
    private void ToggleLoreUI()
    {
        if (loreUI == null || loreListText == null) return;

        isOpen = !isOpen;
        loreUI.SetActive(isOpen);

        if (isOpen)
        {
            RefreshUI();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f; // dừng game khi mở menu
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// Làm mới danh sách lore hiển thị trong UI
    /// </summary>
    private void RefreshUI()
    {
        loreListText.text = "";
        foreach (var item in collectedLore)
        {
            loreListText.text += $"• <b>{item.Key}</b>: {item.Value}\n\n";
        }

        if (collectedLore.Count == 0)
        {
            loreListText.text = "<i>Chưa có vật phẩm hoặc ghi chú nào được tìm thấy...</i>";
        }
    }
}
