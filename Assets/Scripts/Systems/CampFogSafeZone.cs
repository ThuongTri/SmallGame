using UnityEngine;

/// <summary>
/// Trong trại (zone): gần chỗ đứng trong veo — chỉ xa mới có sương (Linear fog), kiểu “sương chưa lan tới chỗ này”.
/// Ra ngoài: preset đậm hơn (thường Exponential). Chỉ PrologueDay mặc định.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CampFogSafeZone : MonoBehaviour
{
    [Header("Phase")]
    public PrologueFlowManager flow;
    [Tooltip("Khi bật, chỉ đổi fog trong PrologueDay.")]
    public bool onlyInPrologueDay = true;

    [Header("Fog trong trại — nhìn xa mới thấy sương")]
    [Tooltip("BẬT = tắt hẳn RenderSettings.fog trong zone → gần xa đều không sương, CHỈ khi bước ra khỏi collider mới thấy sương (đúng bug bạn gặp). Để TẮT (unchecked) để dùng fog Linear nhìn xa có mù.")]
    public bool fogOffInsideCamp = false;

    [Tooltip("Bật: fog Linear (gần trong veo, xa mới đục). Tắt: dùng Exponential + Fog Density Inside.")]
    public bool insideFogUsesDistanceFalloff = true;

    public Color fogColorInside = new Color(0.72f, 0.74f, 0.82f, 1f);

    [Tooltip("Linear: từ camera, những gì GẦN hơn giá trị này (m) gần như không sương.")]
    public float insideLinearFogStart = 26f;

    [Tooltip("Linear: xa đến khoảng cách này (m) thì sương gần như đặc.")]
    public float insideLinearFogEnd = 150f;

    [Tooltip("Chế độ phụ khi TẮT insideFogUsesDistanceFalloff — sương đều Exponential trong zone.")]
    public float fogDensityInside = 0.012f;

    [Header("Fog bên ngoài (hoang dã)")]
    public bool fogOnOutside = true;
    public Color fogColorOutside = new Color(0.67f, 0.68f, 0.76f, 1f);
    public FogMode fogModeOutside = FogMode.ExponentialSquared;
    public float fogDensityOutside = 0.03f;
    public float linearFogStartOutside = 10f;
    public float linearFogEndOutside = 120f;

    int playersInside;

    void Awake()
    {
        var c = GetComponent<Collider>();
        if (c != null && !c.isTrigger)
            c.isTrigger = true;
        if (flow == null)
            flow = PrologueFlowManager.Instance;
    }

    void Start()
    {
        Invoke(nameof(TryApplyFogIfPlayerStartsInside), 0.05f);
    }

    bool GateAllowsFogDriver()
    {
        if (!onlyInPrologueDay) return true;
        if (flow == null) return true;
        return flow.currentPhase == PrologueFlowManager.Phase.PrologueDay;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playersInside++;
        if (playersInside == 1 && GateAllowsFogDriver())
            ApplyInsideFog();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playersInside = Mathf.Max(0, playersInside - 1);
        if (playersInside == 0 && GateAllowsFogDriver())
            ApplyOutsideFog();
    }

    void TryApplyFogIfPlayerStartsInside()
    {
        if (!GateAllowsFogDriver()) return;
        if (playersInside > 0) return;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;

        var zoneCol = GetComponent<Collider>();
        var cc = p.GetComponent<CharacterController>();
        if (zoneCol == null) return;

        Bounds pb = cc != null ? cc.bounds : new Bounds(p.transform.position, Vector3.one * 0.5f);
        if (!zoneCol.bounds.Intersects(pb))
            return;

        playersInside = 1;
        ApplyInsideFog();
    }

    void ApplyInsideFog()
    {
        if (fogOffInsideCamp)
        {
            RenderSettings.fog = false;
            return;
        }

        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColorInside;

        if (insideFogUsesDistanceFalloff)
        {
            RenderSettings.fogMode = FogMode.Linear;
            float start = Mathf.Max(0f, insideLinearFogStart);
            float end = Mathf.Max(start + 5f, insideLinearFogEnd);
            RenderSettings.fogStartDistance = start;
            RenderSettings.fogEndDistance = end;
            RenderSettings.fogDensity = 0f;
            return;
        }

        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = Mathf.Max(0f, fogDensityInside);
        RenderSettings.fogStartDistance = linearFogStartOutside;
        RenderSettings.fogEndDistance = linearFogEndOutside;
    }

    void ApplyOutsideFog()
    {
        if (!fogOnOutside)
        {
            RenderSettings.fog = false;
            return;
        }

        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColorOutside;
        RenderSettings.fogMode = fogModeOutside;
        RenderSettings.fogDensity = fogDensityOutside;
        RenderSettings.fogStartDistance = linearFogStartOutside;
        RenderSettings.fogEndDistance = linearFogEndOutside;
    }

#if UNITY_EDITOR
    [ContextMenu("Copy OUTSIDE fog from current Render Settings")]
    void SnapOutsideFromScene()
    {
        fogOnOutside = RenderSettings.fog;
        fogColorOutside = RenderSettings.fogColor;
        fogModeOutside = RenderSettings.fogMode;
        fogDensityOutside = RenderSettings.fogDensity;
        linearFogStartOutside = RenderSettings.fogStartDistance;
        linearFogEndOutside = RenderSettings.fogEndDistance;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
