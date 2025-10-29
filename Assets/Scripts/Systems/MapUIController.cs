using UnityEngine;
using UnityEngine.UI;

public class MapUIController : MonoBehaviour
{
    public GameObject panel; // Panel chứa image
    public Image mapImage;
    public KeyCode toggleKey = KeyCode.M; // phím mở map

    void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMap();
        }
    }

    public void ToggleMap()
    {
        if (panel == null) return;
        // lấy sprite từ ObjectiveManager
        var spr = ObjectiveManager.Instance != null ? ObjectiveManager.Instance.GetAssembledMapSprite() : null;
        if (spr == null)
        {
            // nếu chưa có map assembled thì show message tạm
            // bạn có thể show UI toast: "Bạn chưa ghép đủ bản đồ."
            Debug.Log("Map chưa ghép được.");
            return;
        }

        mapImage.sprite = spr;
        panel.SetActive(!panel.activeSelf);
    }
}
