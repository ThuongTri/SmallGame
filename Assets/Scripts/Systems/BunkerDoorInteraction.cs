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
    public bool requireAssembledMap = true;
    public string needMapText = "Bạn cần bản đồ hoàn chỉnh trước khi rời đi.";

    [Header("Bad Ending Setup")]
    public string badEndingScene = "BadEnding"; // tên scene Bad Ending
    public float delayBeforeEnd = 4f; // thời gian chờ
    public string escapeMessage = "Bạn đã rời khỏi nơi này...";
    public bool useExitChoicePanel = true;

    private bool isOpened = false;
    private bool doorUnlockedVisual = false;

    void Start()
    {
        if (doorAnimator == null)
            doorAnimator = GetComponentInChildren<Animator>();
    }

    public void OnInteract()
    {
        if (EndingManager.Instance != null && EndingManager.Instance.IsShowingChoice) return;
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

        if (requireAssembledMap && !ObjectiveManager.Instance.HasItem("assembled_map"))
        {
            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage(needMapText);
            return;
        }

        // Nếu có chìa, mở cửa (trigger 1 lần)
        if (!doorUnlockedVisual && doorAnimator != null)
            doorAnimator.SetTrigger(unlockTrigger);
        doorUnlockedVisual = true;

        Debug.Log(openText);
        if (UIMessageManager.Instance != null)
            UIMessageManager.Instance.ShowMessage(string.IsNullOrWhiteSpace(openText) ? "Cánh cửa đã mở!" : openText);

        // ✅ Gọi UI lựa chọn rời đi (thay vì gọi Bad Ending ngay lập tức)
        if (useExitChoicePanel && EndingManager.Instance != null)
        {
            // Hiển thị hộp thoại cho người chơi chọn
            EndingManager.Instance.ShowExitChoice(
                string.IsNullOrWhiteSpace(escapeMessage) ? "Leave?" : escapeMessage,
                badEndingScene
            );
            // Cho phép tương tác lại nếu player chọn "ở lại".
            isOpened = false;
        }
        else
        {
            // Nếu chưa có EndingManager trong scene, xử lý trực tiếp ở đây
            isOpened = true;
            StartCoroutine(TriggerBadEndingDirect());
        }
    }

    IEnumerator TriggerBadEndingDirect()
    {
        if (UIMessageManager.Instance != null)
            UIMessageManager.Instance.ShowMessage(escapeMessage);

        yield return new WaitForSeconds(delayBeforeEnd);
        if (Application.CanStreamedLevelBeLoaded(badEndingScene))
            SceneManager.LoadScene(badEndingScene);
        else
        {
            Debug.LogWarning($"[BunkerDoorInteraction] Scene '{badEndingScene}' chưa có trong Build Settings.");
            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage("Thiếu scene ending trong Build Settings.");
            isOpened = false;
        }
    }

    public string GetInteractionPrompt()
    {
        if (isOpened)
            return "";
        if (doorUnlockedVisual)
            return "Nhấn E để chọn rời đi";
        if (ObjectiveManager.Instance != null && !ObjectiveManager.Instance.HasItem(requiredKeyID))
            return "Nhấn E (cần chìa khóa)";
        if (requireAssembledMap && ObjectiveManager.Instance != null && !ObjectiveManager.Instance.HasItem("assembled_map"))
            return "Nhấn E (cần bản đồ hoàn chỉnh)";
        return "Nhấn E để mở cửa";
    }
}
