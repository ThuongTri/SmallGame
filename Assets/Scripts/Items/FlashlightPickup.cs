using UnityEngine;
using TMPro;

public class FlashlightPickup : MonoBehaviour, IInteractable
{
    public GameObject playerFlashlight; 
    public TextMeshProUGUI flashlightText;

    public void OnInteract()
    {
        if (playerFlashlight != null)
        {
            // Kích hoạt PlayerFlashlight
            var playerFlashlightScript = playerFlashlight.GetComponent<PlayerFlashlight>();
            if (playerFlashlightScript != null)
            {
                playerFlashlightScript.UnlockFlashlight();
            }
            
            // Kích hoạt FlashlightController
            var flashlightController = playerFlashlight.GetComponent<FlashlightController>();
            if (flashlightController != null)
            {
                flashlightController.ActivateFlashlight();
            }
        }

		if (flashlightText != null)
		{
			flashlightText.text = "Press F";
			flashlightText.gameObject.SetActive(true);
		}

		StartCoroutine(PickupSequence());
    }

	private System.Collections.IEnumerator PickupSequence()
	{
		// Ngăn tương tác lại ngay lập tức
		var colliders = GetComponentsInChildren<Collider>();
		for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
		var renderers = GetComponentsInChildren<Renderer>();
		for (int i = 0; i < renderers.Length; i++) renderers[i].enabled = false;

		// Chờ 3 giây rồi ẩn chữ và hủy object
		yield return new WaitForSeconds(3f);
		if (flashlightText != null) flashlightText.gameObject.SetActive(false);
		Destroy(gameObject);
	}
}
