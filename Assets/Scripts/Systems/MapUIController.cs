using UnityEngine;
using UnityEngine.UI;

public class MapUIController : MonoBehaviour
{
    public static MapUIController Instance;

    public GameObject panel; // Panel chứa image
    public Image mapImage;
    public RawImage mapRawImage;
    public KeyCode toggleKey = KeyCode.M; // phím mở map
    [Header("Player marker")]
    public RectTransform mapRect;
    public RectTransform playerMarker;
    public Transform player;
    public Vector2 worldMin = new Vector2(-120f, -120f);
    public Vector2 worldMax = new Vector2(120f, 120f);
    public bool autoFitWorldBoundsFromScene = true;
    [Tooltip("Tự nới bounds khi player đi ra ngoài map hiện tại (hữu ích khi có map 2).")]
    public bool autoExpandBoundsFromPlayer = true;
    public Transform worldBoundsRoot;
    public bool rotateMarkerWithPlayerYaw = true;

    [Header("Control lock while map open")]
    public MonoBehaviour playerControllerToDisable;
    public bool unlockCursorWhenOpen = true;
    bool wasControllerEnabled;

    void Start()
    {
        Instance = this;
        if (panel == null)
        {
            var mapPanelGo = GameObject.Find("MapPanel");
            if (mapPanelGo != null) panel = mapPanelGo;
        }
        if (panel != null) panel.SetActive(false);
        if (mapRect == null && panel != null) mapRect = panel.GetComponent<RectTransform>();
        if (mapRawImage == null && panel != null) mapRawImage = panel.GetComponentInChildren<RawImage>(true);
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
        if (playerControllerToDisable == null && player != null)
            playerControllerToDisable = player.GetComponent<PlayerController>();

        if (playerMarker == null && mapRect != null)
            playerMarker = CreateRuntimeMarker(mapRect);

        if (autoFitWorldBoundsFromScene)
            TryAutoFitBounds();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMap();
        }

        if (panel != null && panel.activeSelf)
            UpdatePlayerMarker();
    }

    public void ToggleMap()
    {
        if (panel == null) return;
        if (EndingManager.Instance != null && EndingManager.Instance.IsShowingChoice) return;
        // lấy sprite từ ObjectiveManager
        var spr = ObjectiveManager.Instance != null ? ObjectiveManager.Instance.GetAssembledMapSprite() : null;
        bool canUseRawTexture = mapRawImage != null && mapRawImage.texture != null;
        if (spr == null && !canUseRawTexture)
        {
            // nếu chưa có map assembled thì show message tạm
            // bạn có thể show UI toast: "Bạn chưa ghép đủ bản đồ."
            Debug.Log("Map chưa ghép được.");
            return;
        }

        if (mapImage != null)
            mapImage.sprite = spr;
        if (mapRawImage != null && spr != null)
            mapRawImage.texture = spr.texture;
        bool open = !panel.activeSelf;
        panel.SetActive(open);
        if (open) UpdatePlayerMarker();
        ApplyControlLock(open);
    }

    void UpdatePlayerMarker()
    {
        if (playerMarker == null || mapRect == null || player == null) return;

        if (autoExpandBoundsFromPlayer)
        {
            float pad = 8f;
            worldMin.x = Mathf.Min(worldMin.x, player.position.x - pad);
            worldMin.y = Mathf.Min(worldMin.y, player.position.z - pad);
            worldMax.x = Mathf.Max(worldMax.x, player.position.x + pad);
            worldMax.y = Mathf.Max(worldMax.y, player.position.z + pad);
        }

        float nx = Mathf.InverseLerp(worldMin.x, worldMax.x, player.position.x);
        float ny = Mathf.InverseLerp(worldMin.y, worldMax.y, player.position.z);
        nx = Mathf.Clamp01(nx);
        ny = Mathf.Clamp01(ny);

        Rect r = mapRect.rect;
        float px = Mathf.Lerp(r.xMin, r.xMax, nx);
        float py = Mathf.Lerp(r.yMin, r.yMax, ny);
        playerMarker.anchoredPosition = new Vector2(px, py);

        if (rotateMarkerWithPlayerYaw)
            playerMarker.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
    }

    void ApplyControlLock(bool mapOpen)
    {
        if (playerControllerToDisable != null)
        {
            if (mapOpen)
            {
                wasControllerEnabled = playerControllerToDisable.enabled;
                playerControllerToDisable.enabled = false;
            }
            else
            {
                playerControllerToDisable.enabled = wasControllerEnabled;
            }
        }

        if (unlockCursorWhenOpen)
        {
            Cursor.lockState = mapOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = mapOpen;
        }
    }

    void TryAutoFitBounds()
    {
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        if (worldBoundsRoot != null)
        {
            var renderers = worldBoundsRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (!hasBounds) { b = r.bounds; hasBounds = true; }
                else b.Encapsulate(r.bounds);
            }
        }

        if (!hasBounds)
        {
            var terrains = Terrain.activeTerrains;
            foreach (var t in terrains)
            {
                if (t == null || t.terrainData == null) continue;
                Vector3 min = t.transform.position;
                Vector3 max = min + t.terrainData.size;
                Bounds tb = new Bounds((min + max) * 0.5f, max - min);
                if (!hasBounds) { b = tb; hasBounds = true; }
                else b.Encapsulate(tb);
            }
        }

        if (!hasBounds) return;
        worldMin = new Vector2(b.min.x, b.min.z);
        worldMax = new Vector2(b.max.x, b.max.z);
    }

    RectTransform CreateRuntimeMarker(RectTransform parent)
    {
        var go = new GameObject("PlayerMarker", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(14f, 14f);
        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 0.15f, 0.15f, 0.95f);
        img.raycastTarget = false;
        return rt;
    }
}
