using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameOverManager : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup gameOverGroup; // Kéo cái GameOverPanel vào đây
    public Button retryButton;        // Kéo nút Retry vào đây

    void Start()
    {
        // 1. Ẩn màn hình chết khi bắt đầu game
        if(gameOverGroup != null)
        {
            gameOverGroup.alpha = 0; // Trong suốt
            gameOverGroup.interactable = false; // Không bấm được
            gameOverGroup.blocksRaycasts = false; // Không chặn chuột
        }
        
        // 2. Gắn chức năng cho nút Retry
        if(retryButton != null)
            retryButton.onClick.AddListener(RestartGame);
    }

    // Hàm này sẽ được MonsterAI gọi khi bắt được người chơi
    public void ShowGameOver()
    {
        StartCoroutine(FadeInGameOver());
    }

    /// <summary>
    /// Sau cinematic capture đã fade đen; hiển thị Game Over ngay không chờ thêm.
    /// </summary>
    public void ShowGameOverAfterCapture()
    {
        StopAllCoroutines();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (gameOverGroup != null)
        {
            gameOverGroup.alpha = 1f;
            gameOverGroup.interactable = true;
            gameOverGroup.blocksRaycasts = true;
        }
    }

    IEnumerator FadeInGameOver()
    {
        // Chờ 1 chút để quái hù xong đã
        yield return new WaitForSeconds(1.5f); 

        // Hiện chuột lên để bấm nút
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Làm màn hình tối dần (Fade In)
        float t = 0;
        while(t < 1)
        {
            t += Time.deltaTime;
            if(gameOverGroup != null) gameOverGroup.alpha = t;
            yield return null;
        }
        
        // Cho phép bấm nút
        if(gameOverGroup != null)
        {
            gameOverGroup.interactable = true;
            gameOverGroup.blocksRaycasts = true;
        }
    }

    public void RestartGame()
    {
        // Load lại màn chơi hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}