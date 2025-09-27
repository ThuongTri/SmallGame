using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInteract : MonoBehaviour
{
    [Header("Cài đặt tương tác")]
    public float interactRange = 3f;
    public Camera playerCamera;
    public LayerMask interactableLayer = ~0;

    [Header("UI")]
    public Image crosshair;                  
    public TextMeshProUGUI interactText;     

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (interactText != null) 
            interactText.gameObject.SetActive(false); // ẩn lúc đầu
    }

    void Update()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable == null)
                interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                if (crosshair != null) crosshair.color = Color.yellow;
                if (interactText != null)
                {
                    interactText.gameObject.SetActive(true);
                    interactText.text = "E";
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    interactable.OnInteract();
                }
                return; // quan trọng: ngăn reset UI nếu đang nhìn vào
            }
        }

        // reset UI khi không nhắm vào interactable
        if (crosshair != null) crosshair.color = Color.white;
        if (interactText != null) interactText.gameObject.SetActive(false);
    }
}
