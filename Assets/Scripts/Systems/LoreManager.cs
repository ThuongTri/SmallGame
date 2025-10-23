using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LoreData
{
    public string id;
    public string title;
    public string description;
    public Sprite icon;
}

public class LoreManager : MonoBehaviour
{
    public static LoreManager Instance;

    [Header("Collected Lore")]
    public List<LoreData> collectedLore = new List<LoreData>();

    [Header("UI Reference")]
    public CodexUI codexUI;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Thêm lore mới khi người chơi nhặt item
    /// </summary>
    public void AddLore(string id, string title, string desc, Sprite icon)
    {
        // Nếu đã có rồi thì bỏ qua
        if (collectedLore.Exists(l => l.id == id)) return;

        LoreData newLore = new LoreData
        {
            id = id,
            title = title,
            description = desc,
            icon = icon
        };

        collectedLore.Add(newLore);

        // Cập nhật UI nếu có
        if (codexUI != null)
            codexUI.UpdateUI(collectedLore);
    }

    /// <summary>
    /// Dành cho khi mở inventory để reload lại
    /// </summary>
    public void RefreshCodex()
    {
        if (codexUI != null)
            codexUI.UpdateUI(collectedLore);
    }
}
