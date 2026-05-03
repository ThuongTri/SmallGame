using UnityEngine;
using UnityEngine.Rendering;

public class EnvironmentController : MonoBehaviour
{
    [System.Serializable]
    public class EnvPreset
    {
        [Header("Fog")]
        public bool fog;
        public Color fogColor = Color.gray;
        public FogMode fogMode = FogMode.ExponentialSquared;
        public float fogDensity = 0.02f;
        public float linearFogStart = 0f;
        public float linearFogEnd = 300f;

        [Header("Ambient")]
        public AmbientMode ambientMode = AmbientMode.Skybox;
        public Color ambientSkyColor = new Color(0.212f, 0.227f, 0.259f);
        public Color ambientEquatorColor = new Color(0.114f, 0.125f, 0.133f);
        public Color ambientGroundColor = new Color(0.047f, 0.043f, 0.035f);
        public float ambientIntensity = 1f;

        [Header("Skybox")]
        public Material skyboxMaterial; // null = giu nguyen skybox hien tai

        [Header("Sun (Directional Light)")]
        public Light sun;
        public bool sunEnabled = true;
        public float sunIntensity = 1f;
        public Color sunColor = Color.white;
public Vector3 sunEuler = new Vector3(50f, 30f, 0f);
        [Header("URP PostProcess Volume toggles (optional)")]
        public GameObject[] enableThese;
        public GameObject[] disableThese;
    }

    [Header("Presets")]
    public EnvPreset day;
    public EnvPreset night;

    [Header("Boot")]
    public bool applyDayOnStart = true;

    void Start()
    {
        if (applyDayOnStart) Apply(day);
    }

    public void ApplyDay() => Apply(day);
    public void ApplyNight() => Apply(night);

    public void Apply(EnvPreset p)
    {
        // Fog
        RenderSettings.fog = p.fog;
        RenderSettings.fogColor = p.fogColor;
        RenderSettings.fogMode = p.fogMode;
        RenderSettings.fogDensity = p.fogDensity;
        RenderSettings.fogStartDistance = p.linearFogStart;
        RenderSettings.fogEndDistance = p.linearFogEnd;

        // Ambient
        RenderSettings.ambientMode = p.ambientMode;
        RenderSettings.ambientSkyColor = p.ambientSkyColor;
        RenderSettings.ambientEquatorColor = p.ambientEquatorColor;
        RenderSettings.ambientGroundColor = p.ambientGroundColor;
        RenderSettings.ambientIntensity = p.ambientIntensity;

        // Skybox
        if (p.skyboxMaterial != null)
            RenderSettings.skybox = p.skyboxMaterial;

        // Sun
        if (p.sun != null)
        {
            p.sun.enabled = p.sunEnabled;
            p.sun.intensity = p.sunIntensity;
            p.sun.color = p.sunColor;
            p.sun.transform.rotation = Quaternion.Euler(p.sunEuler);
        }

        // Volumes
        ToggleObjects(p.enableThese, true);
        ToggleObjects(p.disableThese, false);
    }

    static void ToggleObjects(GameObject[] objs, bool on)
    {
        if (objs == null) return;
        for (int i = 0; i < objs.Length; i++)
            if (objs[i] != null) objs[i].SetActive(on);
    }

#if UNITY_EDITOR
    [ContextMenu("SNAPSHOT CURRENT -> DAY")]
    void SnapDay()
    {
        Snap(ref day);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("SNAPSHOT CURRENT -> NIGHT")]
    void SnapNight()
    {
        Snap(ref night);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    void Snap(ref EnvPreset p)
    {
        p.fog = RenderSettings.fog;
        p.fogColor = RenderSettings.fogColor;
        p.fogMode = RenderSettings.fogMode;
        p.fogDensity = RenderSettings.fogDensity;
        p.linearFogStart = RenderSettings.fogStartDistance;
        p.linearFogEnd = RenderSettings.fogEndDistance;

        p.ambientMode = RenderSettings.ambientMode;
        p.ambientSkyColor = RenderSettings.ambientSkyColor;
        p.ambientEquatorColor = RenderSettings.ambientEquatorColor;
        p.ambientGroundColor = RenderSettings.ambientGroundColor;
        p.ambientIntensity = RenderSettings.ambientIntensity;

        p.skyboxMaterial = RenderSettings.skybox;

        var sun = FindObjectOfType<Light>();
        if (sun != null && sun.type == LightType.Directional)
        {
            p.sun = sun;
            p.sunEnabled = sun.enabled;
            p.sunIntensity = sun.intensity;
            p.sunColor = sun.color;
            p.sunEuler = sun.transform.rotation.eulerAngles;
        }
    }
#endif
}