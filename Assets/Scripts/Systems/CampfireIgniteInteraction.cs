using UnityEngine;

public class CampfireIgniteInteraction : MonoBehaviour, IInteractable
{
    [Header("References")]
    public PrologueFlowManager flowManager;

    [Header("Fire Objects (bật/tắt)")]
    public GameObject[] fireVisuals;   // Particle/VFX object(s)
    public Light fireLight;            // Đèn lửa (nếu có)
    public AudioSource fireLoopAudio;  // Âm thanh lửa loop (nếu có)

    [Header("Rule")]
    public int requiredWood = 3;
    public bool consumeWood = false;   // true nếu muốn trừ củi sau khi đốt

    [Header("Doll burn ritual (optional)")]
    public MonsterAI monster;
    public float dollBurnSuppressSeconds = 120f;
    public float dollBurnRepelDistance = 24f;
    public bool disableNightWaveAfterBurn = true;
    public NightHorrorWave nightHorrorWave;
    public AudioClip dollBurnScream;
    public AudioSource ritualAudio;
    public string promptBurnDoll = "Nhấn E để thiêu nộm búp bê";

    [Header("Prompt")]
    public string promptNotReady = "Cần thêm củi để nhóm lửa";
    public string promptReady = "Nhấn E để nhóm lửa";
    public string promptDone = "Lửa trại đang cháy";

    private bool isLit = false;

    void Start()
    {
        if (flowManager == null) flowManager = PrologueFlowManager.Instance;
        SetFireState(false); // Luôn tắt lúc bắt đầu
    }

    public void OnInteract()
    {
        if (isLit)
        {
            TryBurnDoll();
            return;
        }

        if (flowManager == null)
        {
            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage("Thiếu PrologueFlowManager trong scene.");
            return;
        }

        if (flowManager.woodCollected < requiredWood)
        {
            int need = requiredWood - flowManager.woodCollected;
            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage($"Cần thêm {need} cui.");
            return;
        }

        // Đủ củi -> nhóm lửa
        if (consumeWood) flowManager.woodCollected -= requiredWood;

        isLit = true;
        SetFireState(true);

        // Quan trọng: báo cho PrologueFlowManager biết lửa đã cháy (để CanSleep() pass)
        flowManager.MarkCampfireLit();

        if (UIMessageManager.Instance != null)
            UIMessageManager.Instance.ShowMessage("Bạn đã nhóm lửa trại.");
    }

    void TryBurnDoll()
    {
        if (PlayerRelicController.Instance == null || !PlayerRelicController.Instance.hasDoll)
        {
            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage("Lửa trại đang cháy.");
            return;
        }

        if (!PlayerRelicController.Instance.ConsumeDollForBurn())
            return;

        if (ritualAudio != null && dollBurnScream != null)
            ritualAudio.PlayOneShot(dollBurnScream, 1f);
        else if (dollBurnScream != null)
            AudioSource.PlayClipAtPoint(dollBurnScream, transform.position);

        if (monster == null) monster = FindObjectOfType<MonsterAI>(true);
        if (monster != null)
        {
            monster.RepelFrom(transform.position, dollBurnRepelDistance, dollBurnSuppressSeconds);
            if (monster.gameObject.activeInHierarchy)
                StartCoroutine(TemporarilyHideMonster(monster, Mathf.Max(8f, dollBurnSuppressSeconds * 0.6f)));
        }

        if (disableNightWaveAfterBurn)
        {
            if (nightHorrorWave == null) nightHorrorWave = FindObjectOfType<NightHorrorWave>(true);
            if (nightHorrorWave != null) nightHorrorWave.DisableAfterRitual();
        }

        if (UIMessageManager.Instance != null)
            UIMessageManager.Instance.ShowMessage("Nộm búp bê đã bị thiêu. Đàn hù đêm tan biến, chỉ còn quái chính săn đuổi.");
    }

    System.Collections.IEnumerator TemporarilyHideMonster(MonsterAI m, float hideSeconds)
    {
        if (m == null) yield break;
        Renderer[] rends = m.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
            if (rends[i] != null) rends[i].enabled = false;
        yield return new WaitForSeconds(Mathf.Max(0.1f, hideSeconds));
        for (int i = 0; i < rends.Length; i++)
            if (rends[i] != null) rends[i].enabled = true;
    }

    public string GetInteractionPrompt()
    {
        if (isLit)
        {
            if (PlayerRelicController.Instance != null && PlayerRelicController.Instance.hasDoll)
                return promptBurnDoll;
            return promptDone;
        }

        if (flowManager == null) return promptNotReady;

        return flowManager.woodCollected >= requiredWood ? promptReady : promptNotReady;
    }

    void SetFireState(bool on)
    {
        if (fireVisuals != null)
        {
            for (int i = 0; i < fireVisuals.Length; i++)
            {
                if (fireVisuals[i] != null) fireVisuals[i].SetActive(on);
            }
        }

        if (fireLight != null) fireLight.enabled = on;

        if (fireLoopAudio != null)
        {
            if (on && !fireLoopAudio.isPlaying) fireLoopAudio.Play();
            if (!on && fireLoopAudio.isPlaying) fireLoopAudio.Stop();
        }
    }
}