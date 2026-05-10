using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PrologueObjectiveHUD : MonoBehaviour
{
    static PrologueObjectiveHUD instance;
    TextMeshProUGUI objectiveText;
    PrologueFlowManager flow;
    ObjectiveManager objectiveManager;
    Canvas hudCanvas;
    const int HudSortingOrder = 20000;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Main") return;
        if (FindObjectOfType<PrologueObjectiveHUD>(true) != null) return;
        var go = new GameObject("PrologueObjectiveHUD");
        go.AddComponent<PrologueObjectiveHUD>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        flow = PrologueFlowManager.Instance;
        objectiveManager = ObjectiveManager.Instance;
        EnsureHudCanvas();
        EnsureObjectiveText();
    }

    void Update()
    {
        if (hudCanvas == null) EnsureHudCanvas();
        if (objectiveText == null) EnsureObjectiveText();
        if (objectiveText == null) return;
        if (flow == null) flow = PrologueFlowManager.Instance;
        if (objectiveManager == null) objectiveManager = ObjectiveManager.Instance;
        if (flow == null) return;

        string msg = BuildObjectiveText();
        bool show = !string.IsNullOrWhiteSpace(msg);
        objectiveText.gameObject.SetActive(show);
        if (show) objectiveText.text = msg;
    }

    string BuildObjectiveText()
    {
        switch (flow.currentPhase)
        {
            case PrologueFlowManager.Phase.PrologueDay:
                if (flow.woodCollected < flow.requiredWood)
                    return $"THU THAP CUI ({flow.woodCollected}/{flow.requiredWood})";
                if (!flow.campfireLit)
                    return "NHOM LUA TRAI";
                return "VAO LEU DE NGU";

            case PrologueFlowManager.Phase.TransitionSleep:
                return "DANG NGU...";

            case PrologueFlowManager.Phase.NightmareNight:
                return HasFlashlight() ? string.Empty : "TIM DEN PIN";
        }

        return string.Empty;
    }

    bool HasFlashlight()
    {
        if (objectiveManager == null) return false;
        return objectiveManager.HasItem("den_pin")
               || objectiveManager.HasItem("flashlight")
               || objectiveManager.HasItem("denpin")
               || objectiveManager.HasItem("flash_light");
    }

    void EnsureHudCanvas()
    {
        if (hudCanvas != null) return;

        // Always use our own canvas so other panels can't break/cover it.
        var cgo = new GameObject("PrologueObjectiveHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        cgo.transform.SetParent(transform, false);
        DontDestroyOnLoad(cgo);

        hudCanvas = cgo.GetComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = HudSortingOrder;

        var scaler = cgo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    void EnsureObjectiveText()
    {
        if (objectiveText != null) return;
        if (hudCanvas == null) EnsureHudCanvas();
        if (hudCanvas == null) return;

        var go = new GameObject("ObjectiveText", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(hudCanvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(22f, -18f);
        rt.sizeDelta = new Vector2(980f, 86f);

        objectiveText = go.GetComponent<TextMeshProUGUI>();
        objectiveText.fontSize = 30f;
        objectiveText.fontStyle = FontStyles.Bold;
        objectiveText.color = new Color(1f, 0.92f, 0.7f, 0.98f);
        objectiveText.outlineWidth = 0.22f;
        objectiveText.alignment = TextAlignmentOptions.TopLeft;
        objectiveText.text = "";

        // TMP tạo runtime thường bị thiếu Font asset => text không hiện.
        // Copy font từ một TextMeshProUGUI có sẵn trong scene để đảm bảo render.
        if (objectiveText.font == null)
        {
            var anyTmp = FindObjectsOfType<TextMeshProUGUI>(true);
            for (int i = 0; i < anyTmp.Length; i++)
            {
                var t = anyTmp[i];
                if (t == null || t == objectiveText) continue;
                if (t.font != null)
                {
                    objectiveText.font = t.font;
                    objectiveText.fontSharedMaterial = t.fontSharedMaterial;
                    break;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
