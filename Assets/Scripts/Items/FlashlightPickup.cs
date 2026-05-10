using UnityEngine;
using TMPro;

public class FlashlightPickup : MonoBehaviour, IInteractable
{
    public GameObject playerFlashlight; 
    public TextMeshProUGUI flashlightText;
    [Header("Rule")]
    public bool allowPickupOnlyAtNight = true;
    public string dayBlockMessage = "Ban ngay chua the nhat den pin.";

    // 🔹 Interface yêu cầu hàm này
    public string GetInteractionPrompt()
    {
        if (!CanPickupNow())
            return "Ban ngay khong the nhat den pin";
        return "Nhấn E để nhặt đèn pin";
    }

    // 🔹 Interface yêu cầu hàm này — ta gọi lại OnInteract() cũ cho tiện
    public void Interact()
    {
        OnInteract();
    }

    // 🔹 Hàm gốc của bạn (giữ nguyên)
    public void OnInteract()
    {
        if (!CanPickupNow())
        {
            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage(dayBlockMessage);
            return;
        }

        if (playerFlashlight != null)
        {
            var playerFlashlightScript = playerFlashlight.GetComponent<PlayerFlashlight>();
            if (playerFlashlightScript != null)
                playerFlashlightScript.UnlockFlashlight();

            var flashlightController = playerFlashlight.GetComponent<FlashlightController>();
            if (flashlightController != null)
                flashlightController.ActivateFlashlight();
        }

        if (flashlightText != null)
        {
            flashlightText.text = "Press F";
            flashlightText.gameObject.SetActive(true);
        }

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.OnItemCollected("flashlight");

        StartCoroutine(PickupSequence());
    }

    bool CanPickupNow()
    {
        if (!allowPickupOnlyAtNight) return true;
        if (PrologueFlowManager.Instance == null) return true;
        return PrologueFlowManager.Instance.currentPhase == PrologueFlowManager.Phase.NightmareNight;
    }

    private System.Collections.IEnumerator PickupSequence()
    {
        var colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;

        var renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = false;

        yield return new WaitForSeconds(3f);
        if (flashlightText != null) flashlightText.gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
