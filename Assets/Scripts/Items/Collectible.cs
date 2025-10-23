using UnityEngine;

public class Collectible : MonoBehaviour, IInteractable
{
    [Header("Thông tin vật phẩm")]
    public string itemID;              // ID duy nhất cho vật phẩm
    public string itemName;            // Tên hiển thị
    [TextArea(3, 6)]
    public string loreText;            // Nội dung lore hiển thị
    public Sprite icon;                // Icon vật phẩm
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

        Debug.Log($"[Nhặt đồ] {itemID}: {loreText}");

        if (LoreManager.Instance != null)
            LoreManager.Instance.AddLore(itemID, itemName, loreText, icon);

        gameObject.SetActive(false);
    }

    public string GetInteractionPrompt()
    {
        return "Nhấn E để nhặt";
    }
}
