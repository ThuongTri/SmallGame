using UnityEngine;

public class MapController : MonoBehaviour
{
    [Header("Giao diện")]
    public GameObject mapPanel; // Kéo cái MapPanel vào đây
    [Tooltip("Giữ OFF nếu đã dùng MapUIController để tránh bấm M bị toggle 2 lần.")]
    public bool listenInput = false;

    [Header("Điều kiện")]
    // ID của bản đồ sau khi ghép xong (phải khớp với ID trong ObjectiveManager)
    public string assembledMapID = "assembled_map"; 

    void Update()
    {
        if (!listenInput) return;
        // Kiểm tra nếu người chơi ấn phím M
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMap();
        }
    }

    void ToggleMap()
    {
        if (MapUIController.Instance != null)
        {
            MapUIController.Instance.ToggleMap();
            return;
        }

        // 1. Kiểm tra xem ObjectiveManager đã có bản đồ ghép chưa
        // (Hàm HasItem này bạn đã có trong ObjectiveManager rồi)
        if (ObjectiveManager.Instance != null && ObjectiveManager.Instance.HasItem(assembledMapID))
        {
            // 2. Bật/Tắt Panel
            bool isActive = mapPanel.activeSelf;
            mapPanel.SetActive(!isActive);

            // (Tùy chọn) Có thể thêm code khóa chuột/dừng game khi xem map nếu muốn
        }
        else
        {
            Debug.Log("Bạn chưa ghép đủ bản đồ!");
            // Hoặc hiện thông báo UI: "Cần tìm đủ 3 mảnh bản đồ"
        }
    }
}