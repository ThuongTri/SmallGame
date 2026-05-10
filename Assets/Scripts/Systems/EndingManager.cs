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
    [Header("Escape ending (no scene load)")]
    public bool useEscapeTextEnding = true;
    public string escapeFinalText = "BAN DA TRON THOAT";
    public Color escapeFinalTextColor = new Color(0.9f, 0.05f, 0.05f, 1f);
    public float escapeFinalTextSeconds = 2.2f;
    public TextMeshProUGUI escapeFinalTextUI;
    public GameObject startGamePanel;
    public Button startGameButton;
    [Header("Start menu scene")]
    public string startMenuSceneName = "MainMenu";

    [Header("Player control")]
    public MonoBehaviour playerControllerScript; // gán script controller (PlayerController) để disable movement (optional)
    [Header("Leave SFX")]
    public AudioSource uiAudioSource;
    public AudioClip leaveLaughClip;

    private CanvasGroup panelCanvasGroup;
    private bool isShowing = false;
    bool didLeave = false;
    public bool IsShowingChoice => isShowing;
    public bool DidLeave => didLeave;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (mainCanvas == null)
            mainCanvas = FindObjectOfType<Canvas>();

        if (choicePanel == null && choicePanelPrefab != null && mainCanvas != null)
            choicePanel = Instantiate(choicePanelPrefab, mainCanvas.transform);

        // Ensure panel exists and is disabled at start
        if (choicePanel != null)
        {
            panelCanvasGroup = choicePanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null) panelCanvasGroup = choicePanel.AddComponent<CanvasGroup>();
            choicePanel.SetActive(false);
        }
        RestoreGlobalAudioState();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
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

    public void ResetUiForNewRun()
    {
        didLeave = false;
        isShowing = false;

        if (choicePanel != null) choicePanel.SetActive(false);
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
        if (escapeFinalTextUI != null) escapeFinalTextUI.gameObject.SetActive(false);
        if (startGamePanel != null) startGamePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        RestoreGlobalAudioState();
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
            ForceFullscreen(choicePanel);
            choicePanel.SetActive(true);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
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
        didLeave = false;
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
        didLeave = true;
        if (leaveLaughClip != null)
        {
            if (uiAudioSource != null) uiAudioSource.PlayOneShot(leaveLaughClip, 0.95f);
            else AudioSource.PlayClipAtPoint(leaveLaughClip, Camera.main != null ? Camera.main.transform.position : transform.position, 0.95f);
        }
        if (useEscapeTextEnding)
            StartCoroutine(ShowEscapeEndingFlow());
        else
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
            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = 1f - (t / dura);
            yield return null;
        }
        choicePanel.SetActive(false);
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
        isShowing = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (playerControllerScript != null)
            playerControllerScript.enabled = true;
    }

    static void ForceFullscreen(GameObject panelGO)
    {
        if (panelGO == null) return;
        RectTransform rt = panelGO.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localPosition = Vector3.zero;
    }

    IEnumerator FadeAndLoad()
    {
        if (mainCanvas == null)
            mainCanvas = FindObjectOfType<Canvas>();

        // Optional: tạo Image full-screen đen để fade
        GameObject fadeGO = new GameObject("ScreenFader");
        if (mainCanvas != null) fadeGO.transform.SetParent(mainCanvas.transform, false);
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

        // finally load scene (safe fallback if scene not in Build Settings)
        if (Application.CanStreamedLevelBeLoaded(badEndingScene))
        {
            SceneManager.LoadScene(badEndingScene);
            yield break;
        }

        Debug.LogWarning($"[EndingManager] Scene '{badEndingScene}' chưa có trong Build Settings.");
        if (UIMessageManager.Instance != null)
            UIMessageManager.Instance.ShowMessage("Chưa cấu hình scene ending trong Build Settings.");
        if (choicePanel != null) choicePanel.SetActive(false);
        isShowing = false;
        if (playerControllerScript != null) playerControllerScript.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    IEnumerator ShowEscapeEndingFlow()
    {
        if (choicePanel != null) choicePanel.SetActive(false);
        isShowing = false;

        if (escapeFinalTextUI == null)
            escapeFinalTextUI = CreateRuntimeEscapeText();

        if (escapeFinalTextUI != null)
        {
            escapeFinalTextUI.text = string.IsNullOrWhiteSpace(escapeFinalText) ? "BAN DA TRON THOAT" : escapeFinalText;
            escapeFinalTextUI.color = escapeFinalTextColor;
            escapeFinalTextUI.gameObject.SetActive(true);
        }

        float wait = Mathf.Max(0.5f, escapeFinalTextSeconds);
        yield return new WaitForSeconds(wait);

        if (escapeFinalTextUI != null)
            escapeFinalTextUI.gameObject.SetActive(false);

        // Show start game panel in current scene (user can start again from there).
        if (startGamePanel != null)
            startGamePanel.SetActive(true);
        else
            EnsureRuntimeStartPanel();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // Avoid freezing global audio/game clocks here; keep UI flow stable.
        Time.timeScale = 1f;
        RestoreGlobalAudioState();
    }

    TextMeshProUGUI CreateRuntimeEscapeText()
    {
        if (mainCanvas == null) return null;
        GameObject go = new GameObject("EscapeFinalText", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(mainCanvas.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(1200f, 220f);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 86f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.text = "";
        go.SetActive(false);
        return tmp;
    }

    void EnsureRuntimeStartPanel()
    {
        if (mainCanvas == null) return;
        if (startGamePanel != null)
        {
            startGamePanel.SetActive(true);
            return;
        }

        GameObject panel = new GameObject("RuntimeStartGamePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(mainCanvas.transform, false);
        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;
        Image pimg = panel.GetComponent<Image>();
        pimg.color = new Color(0f, 0f, 0f, 0.82f);

        GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(panel.transform, false);
        RectTransform trtTitle = titleGO.GetComponent<RectTransform>();
        trtTitle.anchorMin = new Vector2(0.5f, 0.5f);
        trtTitle.anchorMax = new Vector2(0.5f, 0.5f);
        trtTitle.pivot = new Vector2(0.5f, 0.5f);
        trtTitle.sizeDelta = new Vector2(1200f, 160f);
        trtTitle.anchoredPosition = new Vector2(0f, 70f);
        TextMeshProUGUI titleTmp = titleGO.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "BAN DA TRON THOAT";
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.fontSize = 78f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.88f, 0.1f, 0.1f, 1f);
        titleTmp.outlineWidth = 0.22f;

        GameObject buttonGO = new GameObject("StartGameButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(panel.transform, false);
        RectTransform brt = buttonGO.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(420f, 102f);
        brt.anchoredPosition = new Vector2(0f, -55f);
        Image bimg = buttonGO.GetComponent<Image>();
        bimg.color = new Color(0.16f, 0.16f, 0.16f, 0.98f);

        startGameButton = buttonGO.GetComponent<Button>();
        startGameButton.onClick.AddListener(RestartCurrentScene);

        GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(buttonGO.transform, false);
        RectTransform trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        TextMeshProUGUI txt = txtGO.GetComponent<TextMeshProUGUI>();
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 42f;
        txt.fontStyle = FontStyles.Bold;
        txt.text = "START GAME";
        txt.color = Color.white;
        txt.outlineWidth = 0.15f;

        startGamePanel = panel;
        startGamePanel.SetActive(true);
    }

    void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        RestoreGlobalAudioState();
        if (!string.IsNullOrWhiteSpace(startMenuSceneName) && Application.CanStreamedLevelBeLoaded(startMenuSceneName))
            SceneManager.LoadScene(startMenuSceneName);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    static void RestoreGlobalAudioState()
    {
        AudioListener.pause = false;
        AudioListener.volume = 1f;
    }
}
