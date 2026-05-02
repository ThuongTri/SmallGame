using UnityEngine;

public class CampfireIgniteInteraction : MonoBehaviour, IInteractable
{
    [Header("References")]
    public PrologueFlowManager flowManager;

    [Header("Fire Objects (bat/tat)")]
    public GameObject[] fireVisuals;   // Particle/VFX object(s)
    public Light fireLight;            // Den lua (neu co)
    public AudioSource fireLoopAudio;  // Am lua chay loop (neu co)

    [Header("Rule")]
    public int requiredWood = 3;
    public bool consumeWood = false;   // true neu muon tru cui sau khi dot

    [Header("Prompt")]
    public string promptNotReady = "Can them cui de nhom lua";
    public string promptReady = "Nhan E de nhom lua";
    public string promptDone = "Lua trai dang chay";

    private bool isLit = false;

    void Start()
    {
        if (flowManager == null) flowManager = PrologueFlowManager.Instance;
        SetFireState(false); // Luon tat luc bat dau
    }

    public void OnInteract()
    {
        if (isLit) return;

        if (flowManager == null)
        {
            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage("Thieu PrologueFlowManager");
            return;
        }

        if (flowManager.woodCollected < requiredWood)
        {
            int need = requiredWood - flowManager.woodCollected;
            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage($"Can them {need} cui");
            return;
        }

        // Du cui -> nhom lua
        if (consumeWood) flowManager.woodCollected -= requiredWood;
        isLit = true;
        SetFireState(true);

        if (UIMessageManager.Instance != null)
            UIMessageManager.Instance.ShowMessage("Ban da nhom lua trai");
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