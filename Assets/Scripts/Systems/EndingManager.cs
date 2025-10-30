using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý UI lựa chọn khi mở cửa (Exit choice) và chuyển sang Bad Ending.
/// </summary>
public class EndingManager : MonoBehaviour
{
    public static EndingManager Instance;

    [Header("UI - reuse existing Canvas")]
    public Canvas mainCanvas;                 // kéo Canvas chính của bạn vào
    public GameObject choicePanelPrefab;      // optional prefab cho panel, hoặc bạn tạo panel dưới Canvas rồi assign vào choicePanel
    public GameObject choicePanel;            // panel chứa text + 2 nút (Show/Hide)
    public TextMeshProUGUI titleText;         // "Bạn đã rời khỏi nơi này..."
    public Button leaveButton;                // nút "Rời đi"
    public Button cancelButton;               // nút "Hủy"
    public float fadeDuration = 1.0f;         // fade out khi load scene
    public string badEndingScene = "BadEnding";// tên scene

    [Header("Player control")]
    public MonoBehaviour playerControllerScript; // gán script controller (PlayerController) để disable movement (optional)

    private CanvasGroup panelCanvasGroup;
    private bool isShowing = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Ensure panel exists and is disabled at start
        if (choicePanel != null)
        {
            panelCanvasGroup = choicePanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null) panelCanvasGroup = choicePanel.AddComponent<CanvasGroup>();
            choicePanel.SetActive(false);
        }
    }

    void Start()
    {
        // ✅ THÊM VÀO ĐÂY — đảm bảo panel tự ẩn khi bắt đầu
        if (choicePanel != null && choicePanel.activeSelf)
        {
            choicePanel.SetActive(false);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
        }

        if (leaveButton != null) leaveButton.onClick.AddListener(OnLeavePressed);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelPressed);
    }

    /// <summary>
    /// Gọi từ BunkerDoorInteraction khi cửa mở thành công và bạn muốn hiển thị lựa chọn cho người chơi.
    /// </summary>
    public void ShowExitChoice(string message = "Leave ?", string sceneToLoad = null)
    {
        if (isShowing) return;
        isShowing = true;

        if (!string.IsNullOrEmpty(sceneToLoad)) badEndingScene = sceneToLoad;
        if (titleText != null) titleText.text = message;

        // show panel
        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
            panelCanvasGroup.alpha = 1f;
            // unlock cursor so player can click
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // disable player movement if possible
        if (playerControllerScript != null)
            playerControllerScript.enabled = false;
    }

    void OnCancelPressed()
    {
        // hide panel, return control
        if (choicePanel != null)
        {
            StartCoroutine(HidePanel());
        }
        else
        {
            isShowing = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (playerControllerScript != null)
                playerControllerScript.enabled = true;
        }
    }

    void OnLeavePressed()
    {
        // start fade and load bad ending
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator HidePanel()
    {
        // optional fade out panel
        float t = 0f;
        float dura = 0.25f;
        while (t < dura)
        {
            t += Time.deltaTime;
            panelCanvasGroup.alpha = 1f - (t / dura);
            yield return null;
        }
        choicePanel.SetActive(false);
        panelCanvasGroup.alpha = 1f;
        isShowing = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (playerControllerScript != null)
            playerControllerScript.enabled = true;
    }

    IEnumerator FadeAndLoad()
    {
        // Optional: tạo Image full-screen đen để fade
        GameObject fadeGO = new GameObject("ScreenFader");
        fadeGO.transform.SetParent(mainCanvas.transform, false);
        Image img = fadeGO.AddComponent<Image>();
        img.rectTransform.anchorMin = Vector2.zero;
        img.rectTransform.anchorMax = Vector2.one;
        img.rectTransform.offsetMin = Vector2.zero;
        img.rectTransform.offsetMax = Vector2.zero;
        img.color = new Color(0, 0, 0, 0);

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            img.color = new Color(0, 0, 0, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }

        // finally load scene
        SceneManager.LoadScene(badEndingScene);
    }
}
