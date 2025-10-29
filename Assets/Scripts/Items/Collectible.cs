using UnityEngine;

public class Collectible : MonoBehaviour, IInteractable
{
    [Header("Thông tin vật phẩm")]
    public string itemID;               // ID lore
    public string itemTitle;            // Tên lore
    [TextArea(3, 6)]
    public string loreText;             // Mô tả lore
    public Sprite itemIcon;
    public AudioClip pickupSound;

    [Header("Tùy chọn")]
    public bool oneTimeCollect = true;
    private bool collected = false;

    public void OnInteract()
    {
        if (oneTimeCollect && collected) return;
        collected = true;

        if (pickupSound)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Debug.Log($"[Nhặt đồ] {itemID}: {itemTitle}");

        // ✅ Gọi LoreManager
        if (LoreManager.Instance != null)
            LoreManager.Instance.AddLore(itemID, itemTitle, loreText, itemIcon);

        // ✅ Gọi ObjectiveManager (nếu có)
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.OnItemCollected(itemID);

        Destroy(gameObject); // Giữ nguyên logic xoá vật phẩm
    }

    public string GetInteractionPrompt()
    {
        return "Nhấn E để nhặt";
    }
}
