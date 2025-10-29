using UnityEngine;

public class BunkerDoorInteraction : MonoBehaviour, IInteractable
{
    [Header("Yêu cầu")]
    public string requiredKeyID = "rusty_key";

    [Header("Tham chiếu")]
    public Animator doorAnimator;
    public string unlockTrigger = "Unlock";

    [Header("Tin nhắn")]
    [TextArea]
    public string needKeyText = "Cần chìa khóa để mở cánh cửa.";
    [TextArea]
    public string openText = "Cánh cửa mở ra...";

    private bool isOpened = false;

    void Start()
    {
        if (doorAnimator == null)
            doorAnimator = GetComponentInChildren<Animator>();
    }

    public void OnInteract()
    {
        if (isOpened) return;

        if (ObjectiveManager.Instance == null)
        {
            Debug.LogWarning("ObjectiveManager chưa tồn tại trong scene!");
            return;
        }

        if (!ObjectiveManager.Instance.HasItem(requiredKeyID))
        {
            Debug.Log(needKeyText);
            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage(needKeyText);
            return;
        }

        if (doorAnimator != null)
            doorAnimator.SetTrigger(unlockTrigger);

        Debug.Log(openText);
        if (UIMessageManager.Instance != null)
            UIMessageManager.Instance.ShowMessage("Cánh cửa đã mở!");

        isOpened = true;
    }

    public string GetInteractionPrompt()
    {
        if (isOpened)
            return ""; // Khi đã mở rồi thì không hiện gì nữa
        return "Nhấn E để mở cửa";
    }
}
