using UnityEngine;

public class PlayerFlashlight : MonoBehaviour
{
    public Light playerFlashlight; 
    private bool hasFlashlight = false;

    void Start()
    {
        if (playerFlashlight != null)
        {
            playerFlashlight.enabled = false; // Player chưa có đèn => tắt
        }
    }

    void Update()
    {
        if (!hasFlashlight || playerFlashlight == null) return;

        // Kiểm tra xem FlashlightController có đang hoạt động không
        var flashlightController = GetComponent<FlashlightController>();
        if (flashlightController != null)
        {
            // Nếu FlashlightController có, thì để nó xử lý input
            return;
        }

        // ❌ Chỉ cho phép bật đèn SAU KHI nhặt
        if (Input.GetKeyDown(KeyCode.F))
        {
            playerFlashlight.enabled = !playerFlashlight.enabled;
        }
    }

    public void UnlockFlashlight()
    {
        hasFlashlight = true;
    }
}
