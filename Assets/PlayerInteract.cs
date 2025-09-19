using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInteract : MonoBehaviour
{
    [Header("Cài đặt tương tác")]
    public float interactRange = 3f;
    public Camera playerCamera;
    public LayerMask interactableLayer = ~0; // default = Everything

    [Header("UI")]
    public Image crosshair;                  // gắn image crosshair ở Canvas
    public TextMeshProUGUI interactText;     // gắn TextMeshPro (InteractText)

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (interactText != null) interactText.gameObject.SetActive(false);
    }

    void Update()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            // Debug: hiện tên collider trúng
            Debug.Log($"Ray hit: {hit.collider.name}");

            // tìm IInteractable (trong collider hoặc parent)
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null)
                interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                // UI feedback
                if (crosshair != null) crosshair.color = Color.yellow;
                if (interactText != null)
                {
                    interactText.gameObject.SetActive(true);
                    interactText.text = "E";
                }

                // Nhấn E để tương tác
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log("Interacting with: " + hit.collider.name);
                    interactable.OnInteract();
                }

                return; // thoát hàm để không reset UI
            }
        }

        // Không trúng gì => reset UI
        if (crosshair != null) crosshair.color = Color.white;
        if (interactText != null) interactText.gameObject.SetActive(false);
    }
}
