using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Cần cái này để chỉnh sửa chữ

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI difficultyText; // Kéo cái chữ bên trong nút Độ khó vào đây

    // 0 = Dễ, 1 = Thường, 2 = Khó
    private int currentDifficulty = 1; 

    void Start()
    {
        // Hiện chuột để bấm menu (quan trọng vì trong game bạn đã khóa chuột)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Lấy độ khó đã lưu lần trước (mặc định là 1 - Thường)
        currentDifficulty = PlayerPrefs.GetInt("Difficulty", 1);
        UpdateDifficultyText();
    }

    // === HÀM CHO NÚT START ===
    public void OnClickStart()
    {
        // Lưu độ khó lại trước khi chuyển cảnh
        PlayerPrefs.SetInt("Difficulty", currentDifficulty);
        PlayerPrefs.Save();

        // Load scene game (đảm bảo tên scene đúng y hệt)
        // Hoặc dùng số index: SceneManager.LoadScene(1);
        SceneManager.LoadScene("Main"); 
    }

    // === HÀM CHO NÚT ĐỘ KHÓ ===
    public void OnClickDifficulty()
    {
        // Tăng độ khó lên 1 mức
        currentDifficulty++;
        
        // Nếu vượt quá 2 thì quay về 0
        if (currentDifficulty > 2) currentDifficulty = 0;

        UpdateDifficultyText();
    }

    // === HÀM CHO NÚT QUIT ===
    public void OnClickQuit()
    {
        Debug.Log("Đã thoát game!");
        Application.Quit();
    }

    // Cập nhật chữ hiển thị
    void UpdateDifficultyText()
    {
        switch (currentDifficulty)
        {
            case 0: difficultyText.text = "ĐỘ KHÓ: DỄ"; break;
            case 1: difficultyText.text = "ĐỘ KHÓ: THƯỜNG"; break;
            case 2: difficultyText.text = "ĐỘ KHÓ: KHÓ"; break;
        }
    }
}