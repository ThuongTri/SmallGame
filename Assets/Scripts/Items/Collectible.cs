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
        // Nếu đã nhặt và chỉ được nhặt 1 lần thì không làm gì cả
        if (oneTimeCollect && collected) return;
        collected = true;

        // Phát âm thanh nếu có
        if (pickupSound)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        // In ra Console để kiểm tra
        Debug.Log($"[Nhặt đồ] {itemID}: {itemTitle}");

        // Thêm thông tin vào LoreManager
        if (LoreManager.Instance != null)
            LoreManager.Instance.AddLore(itemID, itemTitle, loreText, itemIcon);

        // THAY ĐỔI QUAN TRỌNG NHẤT Ở ĐÂY:
        // gameObject.SetActive(false); // Dòng này chỉ ẩn vật thể đi tạm thời.
        Destroy(gameObject); // Dòng này sẽ XÓA HẲN vật thể khỏi màn chơi.
    }

    public string GetInteractionPrompt()
    {
        return "Nhấn E để nhặt";
    }
}