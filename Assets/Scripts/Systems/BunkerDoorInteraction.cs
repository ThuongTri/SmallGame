using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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

    [Header("Bad Ending Setup")]
    public string badEndingScene = "BadEnding"; // tên scene Bad Ending
    public float delayBeforeEnd = 4f; // thời gian chờ
    public string escapeMessage = "Bạn đã rời khỏi nơi này...";

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

        // Kiểm tra xem người chơi có chìa chưa
        if (!ObjectiveManager.Instance.HasItem(requiredKeyID))
        {
            Debug.Log(needKeyText);
            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage(needKeyText);
            return;
        }

        // Nếu có chìa, mở cửa
        if (doorAnimator != null)
            doorAnimator.SetTrigger(unlockTrigger);

        Debug.Log(openText);
        if (UIMessageManager.Instance != null)
            UIMessageManager.Instance.ShowMessage("Cánh cửa đã mở!");

        isOpened = true;

        // ✅ Gọi UI lựa chọn rời đi (thay vì gọi Bad Ending ngay lập tức)
        if (EndingManager.Instance != null)
        {
            // Hiển thị hộp thoại cho người chơi chọn
            EndingManager.Instance.ShowExitChoice(
                "Leave?",
                badEndingScene
            );
        }
        else
        {
            // Nếu chưa có EndingManager trong scene, xử lý trực tiếp ở đây
            StartCoroutine(TriggerBadEndingDirect());
        }
    }

    IEnumerator TriggerBadEndingDirect()
    {
        if (UIMessageManager.Instance != null)
            UIMessageManager.Instance.ShowMessage(escapeMessage);

        yield return new WaitForSeconds(delayBeforeEnd);

        SceneManager.LoadScene(badEndingScene);
    }

    public string GetInteractionPrompt()
    {
        if (isOpened)
            return ""; // Khi đã mở rồi thì không hiện gì nữa
        return "Nhấn E để mở cửa";
    }
}
