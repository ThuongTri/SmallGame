using UnityEngine;
using TMPro;

public class FlashlightPickup : MonoBehaviour, IInteractable
{
    [Tooltip("Drag the Player's flashlight GameObject (child of Camera) here")]
    public GameObject playerFlashlight;

    [Tooltip("Drag the FlashlightText (TMP UI) từ Canvas vào đây")]
    public TextMeshProUGUI flashlightText;

    public void OnInteract()
    {
        if (playerFlashlight != null)
        {
            // Bật flashlight trên Player (gắn với Camera)
            playerFlashlight.SetActive(true);

            // Nếu có FlashlightController thì cho phép bật/tắt bằng F
            var controller = playerFlashlight.GetComponent<FlashlightController>();
            if (controller != null)
            {
                controller.ActivateFlashlight();
            }
        }

        // Hiện gợi ý "Ấn F để bật đèn pin"
        if (flashlightText != null)
        {
            flashlightText.text = "Press F";
            flashlightText.gameObject.SetActive(true);

            // Tắt sau 3 giây
            flashlightText.GetComponent<MonoBehaviour>().StartCoroutine(HideTextAfterDelay());
        }

        // Xóa object đèn pin dưới đất
        Destroy(gameObject);
    }

    private System.Collections.IEnumerator HideTextAfterDelay()
    {
        yield return new WaitForSeconds(3f);
        if (flashlightText != null)
        {
            flashlightText.gameObject.SetActive(false);
        }
    }
}
