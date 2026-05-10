using UnityEngine;

/// <summary>
/// Gate/fence interaction requiring a specific tool item.
/// If the player has the required item, pressing E will "open" by disabling blockers.
/// </summary>
public class ToolFenceInteraction : MonoBehaviour, IInteractable
{
    [Header("Requirement")]
    public string requiredToolItemID = "saw_tool";
    [Tooltip("Accept IDs containing this keyword too (e.g. 'saw', 'chainsaw'). Leave empty to disable.")]
    public string requiredToolKeyword = "saw";
    public bool consumeToolOnUse = false;

    [Header("Open Action")]
    [Tooltip("Blocker objects/colliders to disable when opened.")]
    public GameObject[] disableOnOpen;
    [Tooltip("Optional objects to enable when opened (broken fence, opened gap, FX).")]
    public GameObject[] enableOnOpen;
    public bool destroySelfAfterOpen = false;
    public float openDelay = 0f;

    [Header("Prompt / Messages")]
    public string promptNeedTool = "Cần công cụ phù hợp để mở hàng rào";
    public string promptReady = "Nhấn E để mở hàng rào";
    public string promptOpened = "Lối đi đã mở";
    public string openedMessage = "Bạn đã mở lối qua hàng rào.";
    public string consumedToolMessage = "Bạn đã dùng công cụ để cắt hàng rào.";

    [Header("Audio")]
    public AudioSource interactAudioSource;
    public AudioClip fenceCutClip;
    public AudioClip fenceOpenClip;
    [Range(0f, 1f)] public float cutVolume = 1f;
    [Range(0f, 1f)] public float openVolume = 1f;

    bool opened;

    public void OnInteract()
    {
        if (opened) return;

        if (!PlayerHasRequiredTool())
        {
            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage(promptNeedTool);
            return;
        }

        if (fenceCutClip != null) PlayClip(fenceCutClip, cutVolume);
        if (openDelay > 0f)
        {
            Invoke(nameof(CompleteOpen), openDelay);
            opened = true;
            return;
        }

        CompleteOpen();
    }

    void CompleteOpen()
    {
        if (opened && (disableOnOpen == null || disableOnOpen.Length == 0) && (enableOnOpen == null || enableOnOpen.Length == 0))
            return;

        opened = true;
        ApplyOpenState();
        if (fenceOpenClip != null) PlayClip(fenceOpenClip, openVolume);

        if (consumeToolOnUse && !string.IsNullOrWhiteSpace(consumedToolMessage) && UIMessageManager.Instance != null)
            UIMessageManager.Instance.ShowMessage(consumedToolMessage);

        if (UIMessageManager.Instance != null)
            UIMessageManager.Instance.ShowMessage(openedMessage);

        if (destroySelfAfterOpen)
            Destroy(this);
    }

    public string GetInteractionPrompt()
    {
        if (opened) return promptOpened;
        return PlayerHasRequiredTool() ? promptReady : promptNeedTool;
    }

    bool PlayerHasRequiredTool()
    {
        if (string.IsNullOrWhiteSpace(requiredToolItemID))
            return true;

        string id = requiredToolItemID.Trim().ToLowerInvariant();
        string kw = string.IsNullOrWhiteSpace(requiredToolKeyword) ? "" : requiredToolKeyword.Trim().ToLowerInvariant();

        if (ObjectiveManager.Instance != null && (ObjectiveManager.Instance.HasItem(id) || (!string.IsNullOrEmpty(kw) && ObjectiveManager.Instance.HasItem(kw))))
            return true;

        if (PlayerRelicController.Instance != null)
        {
            if (PlayerRelicController.Instance.HasCollectedItem(id)) return true;
            if (!string.IsNullOrEmpty(kw) && PlayerRelicController.Instance.HasCollectedItem(kw)) return true;
        }

        return false;
    }

    void ApplyOpenState()
    {
        if (disableOnOpen != null)
        {
            for (int i = 0; i < disableOnOpen.Length; i++)
                if (disableOnOpen[i] != null)
                    disableOnOpen[i].SetActive(false);
        }

        if (enableOnOpen != null)
        {
            for (int i = 0; i < enableOnOpen.Length; i++)
                if (enableOnOpen[i] != null)
                    enableOnOpen[i].SetActive(true);
        }
    }

    void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null) return;
        if (interactAudioSource != null)
        {
            interactAudioSource.PlayOneShot(clip, volume);
            return;
        }

        AudioSource.PlayClipAtPoint(clip, transform.position, volume);
    }
}

