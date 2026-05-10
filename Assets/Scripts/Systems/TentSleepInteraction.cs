using System.Collections;
using UnityEngine;

public class TentSleepInteraction : MonoBehaviour, IInteractable
{
    public PrologueFlowManager flow;
    public EnvironmentController env;
    public SimpleScreenFader fader;

    public PlayerController player;
    public NightHorrorWave nightHorror;

    [Header("Timing")]
    public float fadeOut = 1.0f;
    public float blackHold = 2.0f;
    public float fadeIn = 1.0f;

    public string promptReady = "Nhan E de vao leu ngu";
    public string promptNotReady = "Ban chua san sang de ngu";

    [Header("Sleep curtain (laugh + figure in front of camera)")]
    [Tooltip("2D / non-spatial: always audible when starting sleep, any viewing angle.")]
    public AudioClip[] sleepLaughClips;
    public GameObject sleepCurtainFigurePrefab;
    [Min(0.2f)] public float sleepFigureForwardDistance = 0.9f;
    public float sleepFigureVerticalOffset = -0.1f;
    public bool sleepFigureStickToGround = false;
    public LayerMask sleepFigureGroundMask = ~0;
    [Tooltip("How long to hold the laugh + face spawn before starting screen fade.")]
    public float sleepCurtainHoldSeconds = 1.05f;

    AudioSource sleepUiAudio;

    void Awake()
    {
        if (flow == null) flow = PrologueFlowManager.Instance;
        if (player == null) player = FindObjectOfType<PlayerController>();
        EnsureSleepUiAudio();
    }

    void EnsureSleepUiAudio()
    {
        if (sleepUiAudio != null) return;
        sleepUiAudio = gameObject.GetComponent<AudioSource>();
        if (sleepUiAudio == null) sleepUiAudio = gameObject.AddComponent<AudioSource>();
        sleepUiAudio.playOnAwake = false;
        sleepUiAudio.spatialBlend = 0f;
        sleepUiAudio.dopplerLevel = 0f;
    }

    public void OnInteract()
    {
        if (flow == null || env == null || fader == null) return;

        if (!flow.CanSleep())
        {
            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage("Ban chua hoan thanh trai.");
            return;
        }

        StartCoroutine(SleepRoutine());
    }

    IEnumerator SleepRoutine()
    {
        flow.SetPhase(PrologueFlowManager.Phase.TransitionSleep);

        if (player != null) player.LockPlayerInput();

        PlaySleepCurtainBeat();
        if (sleepCurtainHoldSeconds > 0f)
            yield return new WaitForSecondsRealtime(sleepCurtainHoldSeconds);

        yield return fader.FadeOut(fadeOut);
        yield return new WaitForSecondsRealtime(blackHold);

        env.ApplyNight();
        flow.SetPhase(PrologueFlowManager.Phase.NightmareNight);

        if (nightHorror != null)
            nightHorror.enabled = true;

        yield return fader.FadeIn(fadeIn);

        if (player != null) player.UnlockPlayerInput();
    }

    void PlaySleepCurtainBeat()
    {
        EnsureSleepUiAudio();

        AudioClip[] laughs = sleepLaughClips;
        if (laughs == null || laughs.Length == 0)
        {
            var sp = FindObjectOfType<JumpscareSpawner>(true);
            if (sp != null && sp.scareSounds != null && sp.scareSounds.Length > 0)
                laughs = sp.scareSounds;
        }

        if (laughs != null && laughs.Length > 0)
        {
            var clip = laughs[Random.Range(0, laughs.Length)];
            if (clip != null)
                sleepUiAudio.PlayOneShot(clip, 1f);
        }

        GameObject pref = sleepCurtainFigurePrefab;
        if (pref == null)
        {
            var sp = FindObjectOfType<JumpscareSpawner>(true);
            if (sp != null) pref = sp.monsterPrefab;
        }

        if (pref == null || player == null) return;

        var cam = player.GetComponentInChildren<Camera>(true);
        if (cam == null) return;

        Vector3 aim = cam.transform.position
                      + cam.transform.forward * sleepFigureForwardDistance
                      + Vector3.up * sleepFigureVerticalOffset;
        Vector3 pos = sleepFigureStickToGround
            ? SpawnSurfaceAlign.Resolve(
                aim,
                player.transform,
                sleepFigureGroundMask,
                50f,
                220f,
                true,
                4f,
                0.12f)
            : aim;

        Vector3 flatToPlayer = player.transform.position - pos;
        flatToPlayer.y = 0f;
        Quaternion rot = flatToPlayer.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(flatToPlayer.normalized)
            : Quaternion.Euler(0f, cam.transform.eulerAngles.y + 180f, 0f);

        var go = Instantiate(pref, pos, rot);

        foreach (var m in go.GetComponentsInChildren<JumpscareMove>(true))
            m.enabled = false;

        foreach (var a in go.GetComponentsInChildren<Animator>(true))
        {
            a.applyRootMotion = false;
            a.speed = 0f;
        }

        Destroy(go, Mathf.Max(fadeOut + blackHold + 2f, sleepCurtainHoldSeconds + 3f));
    }

    public string GetInteractionPrompt()
    {
        if (flow != null && flow.CanSleep()) return promptReady;
        return promptNotReady;
    }
}
