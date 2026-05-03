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
        if (isLit) return;

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

    public string GetInteractionPrompt()
    {
        if (isLit) return promptDone;

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