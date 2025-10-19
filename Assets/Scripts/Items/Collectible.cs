using UnityEngine;

public class Collectible : MonoBehaviour, IInteractable
{
    [Header("Thông tin vật phẩm")]
    public string ItemID;
    public AudioClip PickupSound;

    [TextArea]
    public string loreText;

    public bool oneTimeCollect = true;
    private bool collected = false;

    public void OnInteract()
    {
        if (oneTimeCollect && collected) return;

        collected = true;

        if (PickupSound)
            AudioSource.PlayClipAtPoint(PickupSound, transform.position);

        Debug.Log($"[Nhặt đồ] {ItemID} - {loreText}");

        // 👉 Lưu vào hệ thống lore
        if (LoreManager.Instance != null)
            LoreManager.Instance.AddLore(ItemID, loreText);

        // Ẩn vật phẩm sau khi nhặt
        gameObject.SetActive(false);
    }

    public string GetInteractionPrompt()
    {
        return "Nhấn E để nhặt";
    }
}
